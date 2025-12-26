using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InventorySystem : NetworkBehaviour
{
    [Header("Settings")]
    public int slotsCount = 4;
    [SerializeField] private GameObject handItem;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private OxygenSystem oxygenSystem;
    [SerializeField] private InventoryUI inventoryUI;

    private MeshFilter itemFilter;
    private MeshRenderer itemRenderer;
    [SerializeField] private List<Item> items = new List<Item>();
    private Item currentItem;
    private int selectedSlot = -1;

    // NetworkVariables для синхронизации визуала
    private NetworkVariable<int> networkSelectedSlot = new NetworkVariable<int>(-1);
    private NetworkVariable<ulong> networkCurrentItemId = new NetworkVariable<ulong>(0);
    private NetworkVariable<bool> isItemVisible = new NetworkVariable<bool>(false);

    public List<Item> GetItems() => items;

    // Добавьте эти переменные
    private bool isThrowCharging = false;
    private float throwChargeTime = 0f;
    private const float MAX_CHARGE_TIME = 2f;
    public override void OnNetworkSpawn()
    {
        itemFilter = handItem.GetComponent<MeshFilter>();
        itemRenderer = handItem.GetComponent<MeshRenderer>();

        for (int i = 0; i < slotsCount; i++)
        {
            items.Add(null);
        }

        // Подписываемся на изменения NetworkVariables
        if (!IsOwner)
        {
            networkSelectedSlot.OnValueChanged += OnSelectedSlotChanged;
            networkCurrentItemId.OnValueChanged += OnCurrentItemChanged;
            isItemVisible.OnValueChanged += OnItemVisibilityChanged;
        
        
            // У других игроков обновляем визуал при появлении
            UpdateVisualForOtherPlayers();
        }
    }
    #region Item Management (Local)

    [ClientRpc]
    public void AddItemClientRpc(ulong itemId)
    {
        if (!IsOwner) return; // Только владелец добавляет в свой инвентарь

        for (int i = 0; i < slotsCount; i++)
        {
            if (items[i] == null)
            {
                NetworkObject itemObject = NetworkManager.SpawnManager.SpawnedObjects[itemId];
                Item item = itemObject.GetComponent<Item>();
                items[i] = item;

                item.HideItemServerRpc();
                // Штрафы кислорода вес (локально)
                OxygenPenaltyScriptableObject penalty = item.Penalty;
                oxygenSystem.AddPenaltyServerRpc(
                    penalty.id,
                    penalty.displayName,
                    penalty.penaltyAmount,
                    penalty.isTemporary,
                    penalty.penaltyColor,
                    penalty.cureItem
                );

                Debug.Log($"Added item {itemId} to slot {i}");

                // Локальный выбор слота
                SetSelectedSlot(i);
                return;
            }
        }
        Debug.LogWarning("No free slots available");
        return;
    }

    #endregion

    #region Input Handling (Local - Owner Only)

    private void Update()
    {
        if (!IsOwner) return;

        // Выбор слота колесом мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int direction = scroll > 0 ? -1 : 1;
            int newSlot = (selectedSlot + direction + slotsCount) % slotsCount;
            SetSelectedSlot(newSlot);
        }

        // Выбор слота по цифрам
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSelectedSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSelectedSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSelectedSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSelectedSlot(3);

        // Выброс предмета
        DropItemWithForce();

        // Отмена зарядки
        if (isThrowCharging && currentItem == null)
        {
            isThrowCharging = false;
            throwChargeTime = 0f;
        }

        // Использование предмета
        if (Input.GetKeyDown(KeyCode.E) && currentItem != null)
        {
            currentItem.InteractItem();
        }
    }

    private void DropItemWithForce()
    {
        // Начало зарядки броска
        if (Input.GetKeyDown(KeyCode.Q) && currentItem != null)
        {
            isThrowCharging = true;
            throwChargeTime = 0f;
        }

        // Обновление зарядки
        if (isThrowCharging && Input.GetKey(KeyCode.Q))
        {
            throwChargeTime += Time.deltaTime;
            inventoryUI.ProgressBarDrop(throwChargeTime / MAX_CHARGE_TIME);
            Debug.Log($"Зарядка броска: {throwChargeTime:F1} сек");
        }

        // Бросок при отпускании
        if (Input.GetKeyUp(KeyCode.Q) && isThrowCharging)
        {
            float chargePercent = Mathf.Clamp01(throwChargeTime / MAX_CHARGE_TIME);
            float calculatedForce = throwForce + (throwForce * chargePercent * 2f); // Увеличиваем до 3x

            // ТОЛЬКО ВПЕРЕД, без вертикального смещения
            Vector3 dropPosition = transform.position + transform.forward * 1.1f + transform.up * 1.2f;

            Vector3 throwDirection = playerCamera.transform.forward;

            DropItemServerRpc(selectedSlot, dropPosition, throwDirection, calculatedForce);

            inventoryUI.ProgressBarDrop(0f);
            isThrowCharging = false;
            throwChargeTime = 0f;
        }

    }

    #endregion

    #region Slot Selection (Local + Network Sync)

    private void SetSelectedSlot(int newSlot)
    {
        // Проверяем границы массива
        if (newSlot < 0 || newSlot >= slotsCount) return;

        // Если пытаемся выбрать пустой слот - отменяем
        if (items[newSlot] == null) return;

        // Если выбрали тот же слот - снимаем выбор
        if (selectedSlot == newSlot)
        {
            selectedSlot = -1;
            currentItem.ChangeItem();
            currentItem = null;
        }
        else
        {
            // Уведомляем предыдущий предмет о смене
            if (currentItem != null)
            {
                currentItem.ChangeItem();
                if (currentItem.IsSlider && inventoryUI != null)
                {
                    inventoryUI.SelectSliderSlot(selectedSlot, false);
                }
            }

            // Выбираем новый слот
            selectedSlot = newSlot;
            currentItem = items[selectedSlot];
        }

        // Локальное обновление UI и визуала
        UpdateLocalHandAndUI();

        // Синхронизируем с другими клиентами через сервер
        if (IsOwner)
        {
            ulong itemId = currentItem != null ? currentItem.NetworkObjectId : 0;
            UpdateSelectedSlotServerRpc(selectedSlot, itemId);
        }
    }

    [ServerRpc]
    private void UpdateSelectedSlotServerRpc(int slotIndex, ulong itemId)
    {
        networkSelectedSlot.Value = slotIndex;
        networkCurrentItemId.Value = itemId;
        isItemVisible.Value = (slotIndex >= 0 && itemId != 0);
    }

    // Вызывается у других игроков при изменении NetworkVariable
    private void OnSelectedSlotChanged(int oldSlot, int newSlot)
    {
        if (IsOwner) return;
        UpdateVisualForOtherPlayers();
    }

    private void OnCurrentItemChanged(ulong oldId, ulong newId)
    {
        if (IsOwner) return;
        UpdateVisualForOtherPlayers();
    }

    private void OnItemVisibilityChanged(bool oldValue, bool newValue)
    {
        if (IsOwner) return;
        UpdateVisualForOtherPlayers();
    }

    private void UpdateVisualForOtherPlayers()
    {
        if (IsOwner) return;

        if (networkSelectedSlot.Value >= 0 && networkCurrentItemId.Value != 0)
        {
            // Показываем предмет в руке
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                networkCurrentItemId.Value, out NetworkObject itemObject))
            {
                Item item = itemObject.GetComponent<Item>();
                if (item != null)
                {
                    itemFilter.mesh = item.MeshFilter.sharedMesh;
                    itemRenderer.materials = item.Materials.ToArray();
                }
            }
        }
        else
        {
            // Скрываем предмет
            itemFilter.mesh = null;
            itemRenderer.materials = new Material[0];
        }
    }

    private void UpdateLocalHandAndUI()
    {
        if (!IsOwner) return;

        // Обновляем предмет в руке
        if (currentItem != null)
        {
            itemFilter.mesh = currentItem.MeshFilter.sharedMesh;
            itemRenderer.materials = currentItem.Materials.ToArray();

            // Уведомляем предмет о выборе
            currentItem.SelectItem(gameObject);

            // Настройка слайдера
            if (currentItem.IsSlider)
            {
                inventoryUI.SelectSliderSlot(selectedSlot, true);
                currentItem.GetSlider(inventoryUI.GetSlider(selectedSlot));
            }

            // Обновление UI
            inventoryUI.FullSlot(selectedSlot, currentItem.Icon);
        }
        else
        {
            itemFilter.mesh = null;
            itemRenderer.materials = new Material[0];
        }

        // Обновляем выделение слота в UI
        inventoryUI.SelectSlot(selectedSlot);
    }

    #endregion

    #region Drop Item (Server + Local)

    [ServerRpc]
    public void DropItemServerRpc(int slotIndex, Vector3 dropPosition, Vector3 throwDirection, float force)
    {
        // Проверяем на сервере
        if (slotIndex >= 0 && slotIndex < slotsCount)
        {
            // Находим предмет по NetworkObjectId
            ulong itemId = networkCurrentItemId.Value;
            if (itemId != 0 && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                itemId, out NetworkObject itemObject))
            {
                Item item = itemObject.GetComponent<Item>();
                if (item != null)
                {
                    // Возвращаем предмет в мир
                    item.ReturnToWorldServerRpc(dropPosition, throwDirection, throwForce, force);

                    // Удаляем штраф кислорода
                    OxygenPenaltyScriptableObject penalty = item.Penalty;
                    oxygenSystem.RemovePenaltyServerRpc(penalty.cureItem, penalty.penaltyAmount);
                }
            }

            // Сбрасываем NetworkVariables
            networkSelectedSlot.Value = -1;
            networkCurrentItemId.Value = 0;
            isItemVisible.Value = false;

            // Уведомляем клиента о успешном выбрасывании
            DropItemClientRpc(slotIndex, OwnerClientId);
        }
    }

    [ClientRpc]
    private void DropItemClientRpc(int slotIndex, ulong playerId)
    {
        if (IsOwner)
        {
            // Локальная очистка инвентаря
            if (slotIndex >= 0 && slotIndex < items.Count)
            {
                items[slotIndex] = null;
            }

            // Очистка текущего предмета
            if (currentItem != null)
            {
                currentItem.DropItem();
                currentItem = null;
            }

            // Локальное обновление
            selectedSlot = -1;
            UpdateLocalHandAndUI();

            // Очистка UI
            inventoryUI.FullSlot(slotIndex, null);
            inventoryUI.SelectSliderSlot(slotIndex, false);
        }
        else if (NetworkManager.LocalClientId == playerId)
        {
            // У другого игрока скрываем предмет
            itemFilter.mesh = null;
            itemRenderer.materials = new Material[0];
        }
    }

    #endregion

    #region Public Methods

    public Item GetCurrentItem()
    {
        return currentItem;
    }

    public int GetSelectedSlot()
    {
        return selectedSlot;
    }

    public bool HasItemInSlot(int slot)
    {
        return slot >= 0 && slot < slotsCount && items[slot] != null;
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (items == null || items.Count == 0) return;

        if (other.CompareTag("Dry"))
        {
            foreach (var item in items)
            {
                if (item != null) item.WaterCheckServerRpc(false);
            }
        }
        if (other.CompareTag("Water"))
        {
            foreach (var item in items)
            {
                if (item != null) item.WaterCheckServerRpc(true);
            }
        }
    }
}