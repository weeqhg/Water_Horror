using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using Unity.Netcode;

public class AudioController : NetworkBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private FootstepCollection[] footstepCollections;
    private AudioSource audioSource;



    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float raycastDistance = 0.2f;

    [Header("Footstep Settings")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField] private float swimStepInterval = 0.7f;
    [SerializeField] private float jumpLandVolumeMultiplier = 1.5f;

    [Header("Sound Collections")]

    private float stepTimer;
    private bool isGrounded;
    private bool isMoving;
    private bool isSwimming;
    private bool isRunning;
    private bool isCrouch;
    private bool wasGrounded;
    [SerializeField] private string currentSurfaceTag = "Default";


    private CharacterState previousState = CharacterState.Idle;
    [SerializeField] private CharacterState currentState = CharacterState.Idle;
    private Dictionary<string, FootstepCollection> collectionDictionary;


    [System.Serializable]
    public class FootstepCollection
    {
        public string surfaceTag = "Default";
        public AudioClip[] walkClips;
        public AudioClip[] swimClips;

        [Header("Audio Settings")]
        [Range(0.1f, 2f)] public float pitchMin = 0.9f;
        [Range(0.1f, 2f)] public float pitchMax = 1.1f;
        [Range(0f, 1f)] public float volumeMultiplier = 1f;

        public AudioClip GetRandomClip(CharacterState state)
        {
            AudioClip[] clips = state switch
            {
                CharacterState.Idle => null,
                CharacterState.Swimming => swimClips,
                _ => walkClips
            };

            return clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        }
    }

    public enum CharacterState
    {
        Idle,
        Walking,
        Running,
        Crouching,
        Swimming,
        Falling,
        Jump,
        JumpLand
    }


    private void Start()
    {
        audioSource = GetComponent<AudioSource>();


        // Создаём словарь для быстрого доступа
        collectionDictionary = new Dictionary<string, FootstepCollection>();
        foreach (var collection in footstepCollections)
        {
            if (!collectionDictionary.ContainsKey(collection.surfaceTag))
            {
                collectionDictionary.Add(collection.surfaceTag, collection);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        // Сохраняем предыдущее состояние
        previousState = currentState;

        // Обновляем состояние
        UpdateCharacterState();

        //Воспроизводим шаги, если нужно
        HandleFootsteps();

        // Проверяем приземление
        CheckLanding();

        //Прыжок
        CheckJump();

        // Сохраняем состояние приземления
        wasGrounded = isGrounded;
    }


    private void UpdateCharacterState()
    {
        isMoving = playerController.IsMoving;
        isGrounded = playerController.IsGrounded;
        isSwimming = playerController.IsSwim;
        isRunning = playerController.IsRunning;
        isCrouch = playerController.IsCrouch;
        float speed = playerController.CurrentSpeed;

        if (isSwimming)
        {
            currentState = CharacterState.Swimming;
            return;
        }

        if (!isGrounded)
        {
            currentState = CharacterState.Falling;
            return;
        }

        if (!isMoving && isGrounded)
        {
            currentState = CharacterState.Idle;
            return;
        }
        else if (isRunning)
        {
            currentState = CharacterState.Running;
        }
        else if (isCrouch)
        {
            currentState = CharacterState.Crouching;
        }
        else
        {
            currentState = CharacterState.Walking;
            // Если состояние изменилось - сбрасываем таймер
        }
    }

    private void CheckJump()
    {
        if (wasGrounded && !isGrounded)
        {
            PlayFootstep(CharacterState.Jump);
            PlayFootstepServerRpc(CharacterState.Jump);
        }
    }

    private void CheckLanding()
    {
        // Если только что приземлились
        if (isGrounded && !wasGrounded)
        {
            PlayFootstep(CharacterState.JumpLand);
            PlayFootstepServerRpc(CharacterState.JumpLand);
        }
    }

    private void HandleFootsteps()
    {

        if (currentState == CharacterState.Idle ||
            currentState == CharacterState.JumpLand ||
            currentState == CharacterState.Falling ||
            currentState == CharacterState.Jump)
        {
            stepTimer = 0;
            return;
        }


        // Определяем интервал в зависимости от состояния
        float targetInterval = currentState switch
        {
            CharacterState.Running => runStepInterval,
            CharacterState.Crouching => crouchStepInterval,
            CharacterState.Swimming => swimStepInterval,
            _ => walkStepInterval
        };

        if (previousState != currentState)
        {
            PlayFootstep(currentState);
            PlayFootstepServerRpc(currentState);
        }

        // Обновляем таймер
        stepTimer += Time.deltaTime;

        if (stepTimer >= targetInterval)
        {
            PlayFootstep(currentState);
            PlayFootstepServerRpc(currentState);
            stepTimer = 0;
        }
    }

    [ServerRpc]
    private void PlayFootstepServerRpc(CharacterState state)
    {
        PlayFootstepClientRpc(state);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(CharacterState state)
    {
        if (IsOwner) return; // Владелец уже воспроизвел

        PlayFootstep(state);
    }

    private void PlayFootstep(CharacterState state)
    {
        //Определяем поверхность
        DetectSurface();

        //Получаем коллекцию для текущей поверхности
        if (!collectionDictionary.TryGetValue(currentSurfaceTag, out FootstepCollection collection))
        {
            // Если не нашли, используем первую коллекцию как дефолтную
            if (footstepCollections.Length > 0)
                collection = footstepCollections[0];
            else
                return;
        }

        // Получаем клип
        AudioClip clip = collection.GetRandomClip(state);
        if (clip == null)
        {
            // Если для этого состояния нет клипов, используем walkClips
            if (collection.walkClips.Length > 0)
                clip = collection.walkClips[Random.Range(0, collection.walkClips.Length)];
            else
                return;
        }

        // Настройки воспроизведения
        audioSource.clip = clip;
        audioSource.pitch = Random.Range(collection.pitchMin, collection.pitchMax);

        // Настраиваем громкость
        float baseVolume = Random.Range(0.8f, 1f) * collection.volumeMultiplier;
        if (state == CharacterState.JumpLand)
            baseVolume *= jumpLandVolumeMultiplier;
        else if (state == CharacterState.Crouching)
            baseVolume *= 0.6f;

        audioSource.volume = Mathf.Clamp01(baseVolume);

        // Воспроизводим
        audioSource.Play();
    }

    private void DetectSurface()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
        {
            // Способ 1: Проверяем тег объекта
            if (!string.IsNullOrEmpty(hit.collider.tag))
            {
                currentSurfaceTag = hit.collider.tag;
                return;
            }
            else
            {
                currentSurfaceTag = "Default";
            }
        }

        
    }

}