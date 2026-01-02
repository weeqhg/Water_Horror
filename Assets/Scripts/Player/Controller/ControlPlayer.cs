using Cinemachine;
using System.ComponentModel;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private float swimDelay = 0.2f;
    [SerializeField] private float swimForce = 2f;

    [Header("References")]
    [SerializeField] private GameObject localBody;
    [SerializeField] private GameObject remoteBody;

    [Header("First Person References")]
    [SerializeField] private Transform firstPersonParent;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Volume postProcess;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Canvas ui;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Animator playerAnimatorOwner;
    [SerializeField] private AudioMixerController audioMixerController;
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private RadarSystem radarSystem;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private FlashPlayer flashPlayer;
    private Rigidbody rb;
    private CapsuleCollider playerColliderWalk;
    [SerializeField] private BoxCollider topCollider;

    public LayerMask GroundLayer = 1;
    private readonly int Y_Axis = 1;
    private readonly int Z_Axis = 2;
    private bool previousSwimPermission = true;
    private float jumpButtonHoldTime = 0f;
    private Vector3 lastTeleport;

    // Локальные переменные
    private Vector3 movementInput;
    private bool isRunning;
    private bool jumpRequest;
    private bool isCrouch;
    private bool swimPermission = false;
    private bool isSwim;
    private float xRotation = 0f;
    private float currentSpeed;

    // Слои
    private int hide;
    private int show;


    private bool isGroundedCached;
    private float lastGroundCheckTime;
    private const float GROUND_CHECK_INTERVAL = 0.1f;


    private bool isBlockMove = false;

    public bool IsSwim => isSwim;
    public bool IsRunning => isRunning;
    public bool IsCrouch => isCrouch;
    public bool IsMoving => movementInput.magnitude > 0.1f;
    public float CurrentSpeed => currentSpeed;

    public bool InWater => swimPermission;

    #region Проверка на грунт
    public bool IsGrounded
    {
        get
        {
            if (Time.time - lastGroundCheckTime > GROUND_CHECK_INTERVAL)
            {
                isGroundedCached = PerformGroundCheck();
                lastGroundCheckTime = Time.time;
            }
            return isGroundedCached;
        }
    }

    private bool PerformGroundCheck()
    {
        if (playerColliderWalk.enabled)
            return CheckColliderGrounded(playerColliderWalk);
        return false;
    }
    private bool CheckColliderGrounded(CapsuleCollider collider)
    {
        if (collider == null) return false;

        var bottomCenterPoint = new Vector3(
            collider.bounds.center.x,
            collider.bounds.min.y,
            collider.bounds.center.z
        );

        return Physics.CheckCapsule(
            collider.bounds.center,
            bottomCenterPoint,
            collider.bounds.size.x / 2 * 0.9f,
            GroundLayer
        );
    }
    #endregion

    #region Setting for Multiplayer
    public override void OnNetworkSpawn()
    {
        // Получаем индексы слоев
        hide = LayerMask.NameToLayer("HideLayer");
        show = LayerMask.NameToLayer("Default");

        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }

    private void SetupLocalPlayer()
    {

        rb = GetComponent<Rigidbody>();
        playerColliderWalk = GetComponent<CapsuleCollider>();
        radarSystem.Inizialized(transform.position);
        // НАСТРОЙКА ДЛЯ ЛОКАЛЬНОГО ИГРОКА:

        SetLayerRecursively(localBody, show);
        SetLayerRecursively(remoteBody, hide);

        // 2. Включаем камеру игроку
        if (playerCamera != null)
        {
            ui.enabled = true;
            virtualCamera.enabled = true;
            playerCamera.gameObject.SetActive(true);
            SetupFPSCamera();
        }

        // 3. Настройка физики
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        GlobalEventManager.BlockMove.AddListener(BlockMove);
        GlobalEventManager.UnBlockMove.AddListener(UnBlockMove);
        GlobalEventManager.TeleportPos.AddListener(TeleportPlayer);

        // 4. Настройка курсора
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupRemotePlayer()
    {
        // НАСТРОЙКА ДЛЯ УДАЛЕННОГО ИГРОКА:

        // 1. Отключаем камеру других игроков 
        if (playerCamera != null)
        {
            ui.enabled = false;
            virtualCamera.enabled = false;
            playerCamera.gameObject.SetActive(false);
        }

        // 2. Делаем физику кинематическим
        if (rb != null) rb.isKinematic = true;

        // 3. Устанавливаем слой RemotePlayer для тела другого игрока
        SetLayerRecursively(remoteBody, show);
        SetLayerRecursively(localBody, hide);
    }


    private void SetupFPSCamera()
    {
        if (playerCamera == null) return;

        // Камера не должна видеть объекты на слое LocalPlayer
        LayerMask cameraMask = playerCamera.cullingMask;
        cameraMask &= ~(1 << hide); // Убираем слой LocalPlayer из видимости камеры
        cameraMask |= (1 << show); // Добавляем слой RemotePlayer
        playerCamera.cullingMask = cameraMask;

        // Настройки FPS камеры
        playerCamera.nearClipPlane = 0.01f;
        playerCamera.fieldOfView = 80f;
    }
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    #endregion

    #region Movement Players
    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            HandleInputCancel();

            if (isBlockMove)
            {
                movementInput = Vector3.zero;
                isCrouch = false;
                jumpRequest = false;
                isSwim = false;
                isRunning = false;

                UpdateAnimation();
                return;
            }

            HandleInput();
            UpdateAnimation();
            ChangeCollider();
            HandleMouseLook();
        }
    }

    void FixedUpdate()
    {
        if (!IsSpawned) return;


        if (IsOwner)
        {
            if (isBlockMove) return;
            MoveLogic();

            if (jumpRequest)
                JumpLogic();
            if (isSwim)
                SwimLogic();
            if (isCrouch)
                CrouchLogic();
        }
    }

    public void Knock(bool enable)
    {
        if (enable)
        {
            BlockMove();
        }
        else
        {
            UnBlockMove();
        }
    }

    private bool isDie = false;
    public void Die(bool enable)
    {
        isDie = enable;

        if (enable)
        {
            BlockMove();
        }
        else
        {
            UnBlockMove();
        }
    }


    private void HandleInputCancel()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (!isDie) UnBlockMove();
            GlobalEventManager.KeyCancel?.Invoke();
        }
    }
    private void BlockMove()
    {
        Debug.Log("Движение заблокировано");
        isBlockMove = true;
        interactionManager.ToggleInteract(false);
    }
    private void UnBlockMove()
    {
        if (isDie) return;
        Debug.Log("Движение разблокировано");
        isBlockMove = false;
        interactionManager.ToggleInteract(true);
    }

    private void TeleportPlayer(Vector3 pos)
    {
        if (pos != Vector3.zero)
        {
            lastTeleport = pos;
        }
        UnBlockMove();
        radarSystem.Inizialized(lastTeleport);
        transform.position = lastTeleport;
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        movementInput = new Vector3(horizontal, 0.0f, vertical);

        if (Input.GetKey(KeyCode.LeftShift) && vertical >= 0.1f && isCrouch == false && isSwim == false) isRunning = true;
        else isRunning = false;


        if (IsGrounded && Input.GetButtonDown("Crouched"))
        {
            isCrouch = true;
            playerAnimatorOwner.SetTrigger("Crouch");
        }
        // Объединяем проверки прыжка
        if (IsGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
            isCrouch = false;
        }

        // Упрощаем логику плавания
        if (Input.GetButton("Jump") && swimPermission && !IsGrounded)
        {
            jumpButtonHoldTime += Time.deltaTime;

            if (jumpButtonHoldTime >= swimDelay)
            {
                isSwim = true;
            }
        }
        else
        {
            jumpButtonHoldTime = 0f;
        }


        if (Input.GetButtonUp("Jump"))
        {
            isSwim = false;
            jumpButtonHoldTime = 0f;
        }

        if (Input.GetButtonUp("Crouched"))
        {
            isCrouch = false;
        }
    }

    private void ChangeCollider()
    {
        // Проверяем изменилось ли состояние swimPermission
        bool permissionChanged = (swimPermission != previousSwimPermission);

        if (isCrouch)
        {
            topCollider.center = new Vector3(0f, -0.8f, 0.3f);
            playerColliderWalk.center = new Vector3(0f, 0.45f, 0f);
            playerColliderWalk.height = 0.9f;
        }
        else if (!isCrouch)
        {
            if (!swimPermission) topCollider.center = Vector3.zero;
            playerColliderWalk.center = new Vector3(0f, 0.7f, 0f);
            playerColliderWalk.height = 1.4f;
        }

        if (!swimPermission)
        {
            if (permissionChanged)
            {
                topCollider.center = Vector3.zero;
                playerColliderWalk.direction = Y_Axis;
                isSwim = false;
            }
            previousSwimPermission = swimPermission;
            return;
        }

        // Обновляем предыдущее состояние
        previousSwimPermission = swimPermission;

        if (!IsGrounded && IsMoving && isSwim)
        {
            topCollider.center = new Vector3(0f, -0.8f, 0.3f);
            playerColliderWalk.direction = Z_Axis;
        }
        else if (!IsGrounded && !IsMoving || IsGrounded)
        {
            topCollider.center = Vector3.zero;
            playerColliderWalk.direction = Y_Axis;
        }

    }

    private void HandleMouseLook()
    {
        if (cameraPivot == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вертикальный поворот на камере
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Горизонтальный поворот на всем игроке
        transform.Rotate(Vector3.up * mouseX);
    }


    private void MoveLogic()
    {
        if (rb == null) return;

        currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveDirection = transform.forward * movementInput.z + transform.right * movementInput.x;
        moveDirection = moveDirection.normalized;

        // Очищаем горизонтальную скорость для точного контроля
        Vector3 horizontalVelocity = new(rb.velocity.x, 0, rb.velocity.z);
        if (horizontalVelocity.magnitude > currentSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * currentSpeed;
            rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
        }

        // Применяем силу для движения
        Vector3 targetVelocity = moveDirection * currentSpeed;
        Vector3 velocityDifference = targetVelocity - horizontalVelocity;

        // Используем ForceMode.VelocityChange для мгновенного изменения скорости
        rb.AddForce(velocityDifference, ForceMode.VelocityChange);
    }

    private void UpdateAnimation()
    {
        if (playerAnimatorOwner != null)
        {
            if (IsMoving)
            {
                playerAnimatorOwner.SetFloat("Speed", movementInput.magnitude * currentSpeed);
                playerAnimatorOwner.SetFloat("Vertical", movementInput.z);
                playerAnimatorOwner.SetFloat("Horizontal", movementInput.x);
            }
            else
            {
                playerAnimatorOwner.SetFloat("Speed", 0);
                playerAnimatorOwner.SetFloat("Vertical", 0);
                playerAnimatorOwner.SetFloat("Horizontal", 0);
            }

            playerAnimatorOwner.SetBool("isGrounded", IsGrounded);
            playerAnimatorOwner.SetBool("isSwim", isSwim);
            playerAnimatorOwner.SetBool("isCrouch", isCrouch);
        }
    }

    private void JumpLogic()
    {
        if (jumpRequest && IsGrounded && rb != null)
        {
            playerAnimatorOwner.SetTrigger("isJump");
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            jumpRequest = false;
        }
    }

    private void SwimLogic()
    {
        if (isSwim)
        {
            rb.velocity = new Vector3(rb.velocity.x, swimForce, rb.velocity.z);
        }
    }

    private void CrouchLogic()
    {
        if (isCrouch)
        {

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Dry"))
        {
            if (other.bounds.Intersects(topCollider.bounds))
            {
                oxygenSystem.ToggleOxygenServerRpc(false);
                oxygenSystem.ToggleParticle(false);
                audioMixerController.ToggleSnapshot(false);
                swimPermission = false;
                postProcess.enabled = false;
                inventorySystem.ToggleItemInWater(false);
                flashPlayer.ChangeFog(false);
            }
        }
        if (other.CompareTag("Water"))
        {
            if (other.bounds.Intersects(topCollider.bounds))
            {
                oxygenSystem.ToggleOxygenServerRpc(true);
                oxygenSystem.ToggleParticle(true);
                oxygenSystem.IncreaseParticle(false);
                audioMixerController.ToggleSnapshot(true);
                swimPermission = true;
                postProcess.enabled = true;
                inventorySystem.ToggleItemInWater(true);
                flashPlayer.ChangeFog(true);
            }
        }
        if (other.CompareTag("Oxygen"))
        {
            if (other.bounds.Intersects(topCollider.bounds))
            {
                oxygenSystem.ToggleOxygenServerRpc(false);
                oxygenSystem.IncreaseParticle(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Oxygen"))
        {
            oxygenSystem.ToggleOxygenServerRpc(true);
            oxygenSystem.IncreaseParticle(false);
        }
    }
    #endregion

}