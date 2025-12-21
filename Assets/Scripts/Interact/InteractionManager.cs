using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Управляет взаимодействием игрока с интерактивными объектами
/// Поддерживает мгновенные и долгие взаимодействия
/// </summary>

[System.Serializable]
public class InteractionSettings
{
    public float HoldTime = 0.5f;
    public InteractionType Type = InteractionType.Instant;

    public enum InteractionType
    {
        Instant,    // Мгновенное взаимодействие (двери, кнопки)
        Hold,       // С удержанием (подбор предметов)
    }
}

public class InteractionManager : NetworkBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float _interactionDistance = 1f;
    [SerializeField] private LayerMask _interactableLayer = -1;
    [SerializeField] private Camera _playerCamera;

    [Header("Input Settings")]
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    [SerializeField] private float _interactionCooldown = 0.5f;

    [Header("UI References")]
    [SerializeField] private GameObject _interactionTipsPanel;
    [SerializeField] private LocalizeStringEvent _objectNameLocalizer;
    [SerializeField] private Image _progressBar;

    private InteractableObject _currentInteractable;
    private InteractionSettings _currentInteractionSettings;
    private bool _canInteract = true;

    private bool _isHoldingInteractKey;
    private float _holdTimer;

    private const float RAY_ORIGIN_X_RATIO = 0.5f;
    private const float RAY_ORIGIN_Y_RATIO = 0.5f;
    private const float MIN_PROGRESS = 0f;
    private const float MAX_PROGRESS = 1f;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
    }

    private void Update()
    {
        if (!IsOwner && !_canInteract) return;

        HandleRaycastDetection();
        HandleInteractionInput();
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        if (_interactionTipsPanel != null)
        {
            _interactionTipsPanel.SetActive(false);
        }

        if (_objectNameLocalizer == null && _interactionTipsPanel != null)
        {
            _objectNameLocalizer = _interactionTipsPanel.GetComponentInChildren<LocalizeStringEvent>();
        }

        if (_progressBar != null)
        {
            _progressBar.fillAmount = 0f;
        }
    }

    #endregion

    #region Input Processing
    private void HandleInteractionInput()
    {
        if (_currentInteractable == null) return;

        switch (_currentInteractionSettings.Type)
        {
            case InteractionSettings.InteractionType.Instant:
                HandleInstantInteraction();
                break;

            case InteractionSettings.InteractionType.Hold:
                HandleHoldInteraction();
                break;
        }
    }

    private void HandleInstantInteraction()
    {
        if (Input.GetKeyDown(_interactKey))
        {
            PerformInteraction();
        }
    }

    private void HandleHoldInteraction()
    {
        if (Input.GetKeyDown(_interactKey))
        {
            StartHoldInteraction();
        }

        if (_isHoldingInteractKey)
        {
            UpdateHoldInteraction();
        }

        if (Input.GetKeyUp(_interactKey))
        {
            CancelHoldInteraction();
        }
    }

    #endregion

    #region Raycast Detection

    private void HandleRaycastDetection()
    {
        if (TryFindInteractable(out InteractableObject interactable))
        {
            HandleNewInteractable(interactable);
        }
        else
        {
            ClearCurrentInteractable();
        }
    }

    private bool TryFindInteractable(out InteractableObject interactable)
    {
        interactable = null;

        Vector3 screenCenter = GetScreenCenter();
        Ray ray = _playerCamera.ScreenPointToRay(screenCenter);

        if (!Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactableLayer))
            return false;

        return hit.collider.TryGetComponent(out interactable);
    }

    private Vector3 GetScreenCenter()
    {
        return new Vector3(
            Screen.width * RAY_ORIGIN_X_RATIO,
            Screen.height * RAY_ORIGIN_Y_RATIO,
            0f
        );
    }

    private void HandleNewInteractable(InteractableObject interactable)
    {
        if (_currentInteractable == interactable) return;

        ClearCurrentInteractable();

        _currentInteractable = interactable;
        _currentInteractable.HighlightObject();

        _currentInteractionSettings = _currentInteractable.GetSetting();
        UpdateInteractionUI(true, _currentInteractable.GetName(), _currentInteractable.GetPrice());
    }

    private void ClearCurrentInteractable()
    {
        if (_currentInteractable == null) return;

        _currentInteractable.DeselectObject();
        ResetHoldInteraction();
        UpdateInteractionUI(false, null, 0);

        _currentInteractable = null;
    }

    #endregion

    #region Hold Interaction

    private void StartHoldInteraction()
    {
        _isHoldingInteractKey = true;
        _holdTimer = 0f;
    }

    private void UpdateHoldInteraction()
    {
        _holdTimer += Time.deltaTime;

        float progress = CalculateHoldProgress();
        UpdateHoldProgressUI(progress);

        if (IsHoldComplete())
        {
            CompleteHoldInteraction();
        }
    }

    private void CancelHoldInteraction()
    {
        ResetHoldInteraction();
    }

    private void CompleteHoldInteraction()
    {
        PerformInteraction();
        ResetHoldInteraction();
    }

    private void ResetHoldInteraction()
    {
        _isHoldingInteractKey = false;
        _holdTimer = 0f;
        UpdateHoldProgressUI(0f);
    }

    private float CalculateHoldProgress()
    {
        return Mathf.Clamp(_holdTimer / _currentInteractionSettings.HoldTime, MIN_PROGRESS, MAX_PROGRESS);
    }

    private bool IsHoldComplete()
    {
        return _holdTimer >= _currentInteractionSettings.HoldTime;
    }

    #endregion

    #region Interaction Execution

    private void PerformInteraction()
    {
        if (_currentInteractable == null) return;

        _currentInteractable.Interact(gameObject);

        _currentInteractable.DeselectObject();

        UpdateInteractionUI(false, null, 0);

        _currentInteractable = null;

        StartCoroutine(InteractionCooldownRoutine());
    }

    private IEnumerator InteractionCooldownRoutine()
    {
        _canInteract = false;
        yield return new WaitForSeconds(_interactionCooldown);
        _canInteract = true;
    }

    #endregion

    #region UI Management

    private void UpdateInteractionUI(bool show, LocalizedString objectName, float price)
    {
        if (_interactionTipsPanel == null) return;

        _interactionTipsPanel.SetActive(show);

        if (show && objectName != null && _objectNameLocalizer != null)
        {
            _objectNameLocalizer.StringReference = objectName;
            _objectNameLocalizer.RefreshString();
        }
        if (price > 0)
        {
            _objectNameLocalizer.StringReference.Arguments = new object[] { price };
            _objectNameLocalizer.RefreshString();
        }
    }

    private void UpdateHoldProgressUI(float progress)
    {
        if (_progressBar == null) return;

        _progressBar.fillAmount = progress;
    }

    #endregion
}