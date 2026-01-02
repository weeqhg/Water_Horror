using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Базовый класс для всех предметов в игре
/// </summary>
public abstract class Item : NetworkBehaviour
{
    [Header("Item Information")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] protected string nameId = "";
    [SerializeField] protected int id = 0;
    [SerializeField] private LocalizedString nameItemForUI;
    [SerializeField] private LocalizedString descriptionItemForUI;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _isSlider;
    [SerializeField] private float _price;
    [SerializeField] private GameObject _meshInteract;
    [SerializeField] private OxygenPenaltyScriptableObject _oxygenPenalty;
    [SerializeField] private float _weight = 0;
    [SerializeField] private bool isActiveItem = true;
    // Components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Renderer _objectRenderer;
    private Collider _itemCollider;
    private Rigidbody _rigidbody;

    // Materials cache
    private readonly List<Material> _originalMaterials = new();

    // Abstract methods for item behavior
    public abstract void InteractItem();
    public abstract void SelectItem(GameObject player);
    public abstract void ChangeItem();
    public abstract void DropItem();
    public abstract void GetSlider(Slider slider);

    #region Properties and Getters

    public Sprite Icon => _icon;
    public bool IsSlider => _isSlider;
    public OxygenPenaltyScriptableObject Penalty => _oxygenPenalty;
    public MeshFilter MeshFilter => _meshFilter;
    public List<Material> Materials => _originalMaterials;
    public float Price => _price;

    public LocalizedString NameItem => nameItemForUI;
    public LocalizedString DescriptionItem => descriptionItemForUI;
    public int Id => id;

    public bool IsActive => isActiveItem;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        SetupPenaltyWeight();
        CacheOriginalMaterials();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        _objectRenderer = GetComponent<Renderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _itemCollider = GetComponentInChildren<Collider>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        WaterCheckServerRpc(false);
    }

    private void SetupPenaltyWeight()
    {
        if (_oxygenPenalty != null)
        {
            _oxygenPenalty.penaltyAmount = _weight;
        }
    }

    private void CacheOriginalMaterials()
    {
        if (_objectRenderer != null)
        {
            _originalMaterials.AddRange(_objectRenderer.materials);
        }
    }

    #endregion

    #region Pickup System

    /// <summary>
    /// Вызывается при взаимодействии с предметом
    /// </summary>
    public void OnItemInteract()
    {
        if (!IsClient) return;

        TryPickupItem();
    }

    private void RbDefault()
    {
        _rigidbody.drag = 0.1f;
        _rigidbody.angularDrag = 0.05f;
    }
    private void RbWater()
    {
        _rigidbody.drag = 3f;
        _rigidbody.angularDrag = 1f;
    }

    private void TryPickupItem()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PickupItemServerRpc(localClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupItemServerRpc(ulong playerId)
    {
        if (!TryGetPlayerInventory(playerId, out InventorySystem inventorySystem))
            return;

        AddItemToInventory(inventorySystem);
    }

    private bool TryGetPlayerInventory(ulong playerId, out InventorySystem inventorySystem)
    {
        inventorySystem = null;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            return false;

        inventorySystem = client.PlayerObject.GetComponent<InventorySystem>();
        return inventorySystem != null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void WaterCheckServerRpc(bool isWater)
    {
        if (isWater)
        {
            RbWater();
        }
        else
        {
            RbDefault();
        }
    }

    private void AddItemToInventory(InventorySystem inventorySystem)
    {
        ulong itemId = NetworkObject.NetworkObjectId;
        inventorySystem.AddItemClientRpc(itemId);
    }

    #endregion

    #region Visibility Control

    [ServerRpc(RequireOwnership = false)]
    public void HideItemServerRpc()
    {
        isActiveItem = false;
        HideItemClientRpc();
    }

    [ClientRpc]
    private void HideItemClientRpc()
    {
        SetItemVisibility(false);
    }

    /// <summary>
    /// Возвращает предмет в мир с физическим броском
    /// </summary>
    [ServerRpc]
    public void ReturnToWorldServerRpc(Vector3 position, Vector3 throwDirection, float throwForce, float throwUpwardForce)
    {
        isActiveItem = true;

        ShowItemClientRpc(position, throwDirection, throwForce, throwUpwardForce);

        transform.position = position;

        if (_rigidbody == null) return;

        Vector3 force = throwDirection.normalized * throwForce + Vector3.up * (throwUpwardForce * 0.5f);
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }

    [ClientRpc]
    private void ShowItemClientRpc(Vector3 position, Vector3 throwDirection, float throwForce, float throwUpwardForce)
    {
        SetItemVisibility(true);
        //transform.position = position;
        //ApplyThrowForce(throwDirection, throwForce, throwUpwardForce);
    }

    private void SetItemVisibility(bool isVisible)
    {
        // Rigidbody
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = !isVisible;
        }

        // Renderers
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = isVisible;
        }

        // Collider
        if (_itemCollider != null)
        {
            _itemCollider.enabled = isVisible;
        }

        // Interactive mesh
        if (_meshInteract != null)
        {
            _meshInteract.SetActive(isVisible);
        }
    }

    private void ApplyThrowForce(Vector3 throwDirection, float throwForce, float throwUpwardForce)
    {
        if (_rigidbody == null) return;

        Vector3 force = throwDirection.normalized * throwForce + Vector3.up * (throwUpwardForce * 0.5f);
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Проверяет, можно ли подобрать предмет
    /// </summary>
    protected bool CanBePickedUp()
    {
        return IsClient && IsSpawned;
    }

    /// <summary>
    /// Получает NetworkObjectId предмета
    /// </summary>
    protected ulong GetItemNetworkId()
    {
        return NetworkObject.NetworkObjectId;
    }

    #endregion


    private void OnCollisionEnter(Collision collision)
    {
        //Слой Ground с индексом = 3
        if (collision.gameObject.layer == 3)
        {
            audioSource.Play();
        }
    }
}