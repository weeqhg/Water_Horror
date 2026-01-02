using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class OxygenSystem : NetworkBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float oxygenDepletionRate = 0.5f;
    [SerializeField] private float criticalOxygenLevel = 20f;
    [SerializeField] private float lowOxygenLevel = 40f;

    [Header("UI Reference")]
    [SerializeField] private OxygenUI oxygenUI;
    [SerializeField] private SimpleRagdollController ragdollController;
    [SerializeField] private ParticleSystem oxygenParticle;
    [SerializeField] private InventorySystem inventorySystem;
    private ParticleSystem.MainModule oxygenParticleMain;

    // Разрешаем владельцу писать
    [SerializeField] private NetworkVariable<float> currentOxygen = new(100f);

    [SerializeField] private NetworkVariable<float> currentMaxOxygen = new(100f);

    [SerializeField] private NetworkVariable<float> currentDopOxygen = new(0f);


    //Словарь для хранения штрафов
    private Dictionary<string, OxygenPenalty> activePenaltiesDict = new();


    //Кэш переменные
    private float cachedTotalPenalty = 0f;
    private bool needsMaxOxygenUpdate = false;
    private float oxygenPercentageCache = 100f;
    private bool isCriticalCache = false;
    private bool isLowCache = false;
    private float currentOxygenRate = 0f;
    //Переключатель на расход кислорода
    private bool isOxygenEnabled = false;

    private bool isOxygenParticle = false;
    //Утилиты
    public float OxygenPercentage => oxygenPercentageCache;
    public bool IsCritical => isCriticalCache;
    public bool IsLow => isLowCache;


    public float AvailableCapacityDopOxygen() => maxOxygen - currentDopOxygen.Value;
    //Класс
    [System.Serializable]
    public class OxygenPenalty
    {
        public string id;
        public string displayName;
        public float penaltyAmount;
        public bool isTemporary;
        public Color penaltyColor;
        public string cureItem;
    }


    #region StartMethod
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentOxygen.Value = maxOxygen;
            currentMaxOxygen.Value = maxOxygen;

            SyncAllPenaltiesClientRpc();
        }

        GlobalEventManager.RebornPlayer.AddListener(RebornPlayerServerRpc);

        currentOxygenRate = oxygenDepletionRate;

        currentOxygen.OnValueChanged += OnOxygenChanged;
        currentMaxOxygen.OnValueChanged += OnMaxOxygenChanged;
        currentDopOxygen.OnValueChanged += OnDopOxygenChanged;
        oxygenParticleMain = oxygenParticle.main;

        if (oxygenUI != null)
        {
            oxygenUI.Initialize(this);
            oxygenUI.UpdateOxygenSlider();
            oxygenUI.UpdateOxygenUiPenalty();
        }
    }

    public void ToggleParticle(bool enable)
    {
        isOxygenParticle = enable;

        if (isOxygenParticle)
        {
            oxygenParticle.Play();
        }
        else
        {
            oxygenParticle.Stop();
        }
    }

    public void IncreaseParticle(bool enable)
    {
        if (enable)
            oxygenParticleMain.maxParticles = 500;
        else
            oxygenParticleMain.maxParticles = 10;
    }

    [ClientRpc]
    private void SyncAllPenaltiesClientRpc()
    {
        if (!IsServer)
        {
            activePenaltiesDict.Clear();
        }

        if (!IsServer && IsClient)
        {
            RequestAllPenaltiesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAllPenaltiesServerRpc()
    {
        foreach (var penalty in activePenaltiesDict.Values)
        {
            SyncPenaltyClientRpc(penalty.id, penalty.displayName, penalty.penaltyAmount,
                penalty.isTemporary, penalty.penaltyColor, penalty.cureItem);
        }
    }
    #endregion

    void Update()
    {
        if (IsServer)
        {
            // Только сервер обновляет кислород
            UpdateOxygen();
            UpdateOxygenCache();

            if (needsMaxOxygenUpdate)
            {
                UpdateMaxOxygen();
                needsMaxOxygenUpdate = false;
            }
        }
        else if (IsClient)
        {
            // Клиенты только обновляют кэш и UI
            UpdateOxygenCache();
        }

        // Все обновляют временные штрафы
        UpdateTemporaryPenalties();

    }

    #region MainOxygen

    /// <summary>
    /// Расход кислорода и восстановление происходит на сервере.
    /// Игроки только получает максимальное значение кислорода и текущие
    /// </summary>
    private void UpdateOxygen()
    {
        // Проверяем, что мы на сервере
        if (!IsServer) return; // Важная проверка!

        if (isOxygenEnabled)
        {
            // Сначала расходуем основной кислород
            if (currentOxygen.Value > 0)
            {
                float depletion = currentOxygenRate * Time.deltaTime;
                currentOxygen.Value = Mathf.Max(0, currentOxygen.Value - depletion);
            }
            // Когда основной закончился, расходуем дополнительный
            else if (currentDopOxygen.Value > 0)
            {
                float depletion = currentOxygenRate * Time.deltaTime;
                currentDopOxygen.Value = Mathf.Max(0, currentDopOxygen.Value - depletion);

                Debug.Log($"Используется дополнительный кислород: {currentDopOxygen.Value:F1}");
            }
        }
        else
        {
            // Восстанавливаем только основной кислород (дополнительный не восстанавливается)
            float restoration = oxygenDepletionRate * 10f * Time.deltaTime;
            currentOxygen.Value = Mathf.Min(currentMaxOxygen.Value, currentOxygen.Value + restoration);
        }
    }

    // Метод для обновления кэшированных значений
    private void UpdateOxygenCache()
    {
        float newPercentage = currentOxygen.Value / currentMaxOxygen.Value * 100f;

        // Обновляем только если значение изменилось значительно
        if (Mathf.Abs(oxygenPercentageCache - newPercentage) > 0.1f)
        {
            oxygenPercentageCache = newPercentage;
            isLowCache = oxygenPercentageCache <= lowOxygenLevel;

            isCriticalCache = oxygenPercentageCache <= criticalOxygenLevel;
            if (isCriticalCache) currentOxygenRate = oxygenDepletionRate / 2f;
            else currentOxygenRate = oxygenDepletionRate;
        }
    }

    private void UpdateMaxOxygen()
    {
        // Быстрое вычисление общего штрафа
        float newTotalPenalty = 0f;
        foreach (var penalty in activePenaltiesDict.Values)
        {
            newTotalPenalty += penalty.penaltyAmount;
        }

        // Обновляем только если изменилось значительно
        if (Mathf.Abs(cachedTotalPenalty - newTotalPenalty) > 0.01f)
        {
            cachedTotalPenalty = newTotalPenalty;
            float newMaxOxygen = Mathf.Max(0f, maxOxygen - cachedTotalPenalty);
            currentMaxOxygen.Value = newMaxOxygen;


            if (currentOxygen.Value > currentMaxOxygen.Value)
            {
                currentOxygen.Value = currentMaxOxygen.Value;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ToggleOxygenServerRpc(bool enable)
    {
        isOxygenEnabled = enable;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RebornPlayerServerRpc()
    {
        ClearAllPenalties();
        NoticePlayerForRebornClientRpc();
    }

    [ClientRpc]
    private void NoticePlayerForRebornClientRpc()
    {
        ragdollController.ToggleDie(false);
        oxygenUI.ToggleDeathPanel(false);
    }


    public void ClearAllPenalties()
    {
        if (activePenaltiesDict.Count == 0)
        {
            Debug.Log("No penalties to clear");
            return;
        }

        Debug.Log($"Clearing all penalties ({activePenaltiesDict.Count} total)");

        // Запоминаем ID всех штрафов для удаления на клиентах
        var allPenaltyIds = new List<string>(activePenaltiesDict.Keys);

        // Очищаем словарь на сервере
        activePenaltiesDict.Clear();

        // Сбрасываем кэш штрафов
        cachedTotalPenalty = 0f;

        // Уведомляем всех клиентов об удалении каждого штрафа
        foreach (var penaltyId in allPenaltyIds)
        {
            RemovePenaltyClientRpc(penaltyId);
        }

        // Принудительно обновляем максимальный кислород
        needsMaxOxygenUpdate = true;
        Debug.Log("All penalties cleared successfully");
    }
    #endregion

    #region Penalty

    #region AddPenalty
    //Добавление штрафов
    [ServerRpc(RequireOwnership = false)]
    public void AddPenaltyServerRpc(string penaltyId, string displayName, float penaltyAmount, bool isTemporary, Color color, string cureItem = "")
    {
        if (currentMaxOxygen.Value <= 0f)
        {
            return;
        }

        // Рассчитываем доступную емкость для штрафов
        float availableForPenalties = maxOxygen - cachedTotalPenalty;

        if (availableForPenalties <= 0f)
        {
            Debug.LogWarning($"Cannot add penalty {penaltyId}: no room for more penalties");
            return;
        }

        // 3. Ограничиваем добавляемый штраф доступным местом
        float penaltyToAdd = Mathf.Min(penaltyAmount, availableForPenalties);

        // Быстрый поиск в Dictionary вместо List.Find
        if (activePenaltiesDict.TryGetValue(penaltyId, out var existingPenalty))
        {
            float maxPossibleForThisPenalty = maxOxygen - (cachedTotalPenalty - existingPenalty.penaltyAmount);
            float actuallyCanAdd = Mathf.Min(penaltyToAdd, maxPossibleForThisPenalty);

            if (actuallyCanAdd <= 0f)
            {
                Debug.LogWarning($"Cannot add to penalty {penaltyId}: would exceed limits");
                return;
            }

            existingPenalty.penaltyAmount += actuallyCanAdd;
            existingPenalty.isTemporary = isTemporary;

            if (!string.IsNullOrEmpty(displayName))
                existingPenalty.displayName = displayName;
            if (!string.IsNullOrEmpty(cureItem))
                existingPenalty.cureItem = cureItem;

            needsMaxOxygenUpdate = true;

            SyncPenaltyClientRpc(penaltyId, displayName, existingPenalty.penaltyAmount,
                existingPenalty.isTemporary, color, cureItem);

            return;
        }


        // penaltyToAdd уже ограничен availableForPenalties
        var penalty = new OxygenPenalty
        {
            id = penaltyId,
            displayName = displayName,
            penaltyAmount = penaltyToAdd,
            isTemporary = isTemporary,
            penaltyColor = color,
            cureItem = cureItem
        };

        activePenaltiesDict.Add(penaltyId, penalty);
        needsMaxOxygenUpdate = true;

        SyncPenaltyClientRpc(penaltyId, displayName, penaltyToAdd, isTemporary, color, cureItem);
    }

    //Синхронизируем с клиентом для того чтобы отобразить на Ui
    [ClientRpc]
    private void SyncPenaltyClientRpc(string penaltyId, string displayName, float penaltyAmount, bool isTemporary, Color color, string cureItem)
    {
        if (activePenaltiesDict.TryGetValue(penaltyId, out var existingPenalty))
        {
            existingPenalty.displayName = displayName;
            existingPenalty.penaltyAmount = penaltyAmount;
            existingPenalty.isTemporary = isTemporary;
            existingPenalty.penaltyColor = color;
            existingPenalty.cureItem = cureItem;
        }
        else
        {
            var penalty = new OxygenPenalty
            {
                id = penaltyId,
                displayName = displayName,
                penaltyAmount = penaltyAmount,
                isTemporary = isTemporary,
                penaltyColor = color,
                cureItem = cureItem
            };
            activePenaltiesDict.Add(penaltyId, penalty);
        }
        oxygenUI?.UpdatePenaltyUI(); // Используем null-conditional operator
    }
    #endregion

    #region MainPenalty

    public List<OxygenPenalty> GetActivePenalties()
    {
        return new List<OxygenPenalty>(activePenaltiesDict.Values);
    }

    private void UpdateTemporaryPenalties()
    {
        if (IsServer)
        {
            // ⭐⭐ ОБНОВЛЯЕМ КЭШ СРАЗУ, чтобы GetAvailablePenaltyCapacity() работал корректно
            float newTotalPenalty = 0f;
            foreach (var penalty in activePenaltiesDict.Values)
            {
                newTotalPenalty += penalty.penaltyAmount;
            }

            if (Mathf.Abs(cachedTotalPenalty - newTotalPenalty) > 0.01f)
            {
                cachedTotalPenalty = newTotalPenalty;
            }
        }

        bool localNeedsUpdate = false;
        var keysToRemove = new List<string>();

        foreach (var kvp in activePenaltiesDict)
        {
            var penalty = kvp.Value;
            if (penalty.isTemporary)
            {
                float oldPenaltyAmount = penalty.penaltyAmount;
                penalty.penaltyAmount -= Time.deltaTime;

                if (IsServer)
                {
                    if (Mathf.Abs(oldPenaltyAmount - penalty.penaltyAmount) > 0.01f)
                    {
                        localNeedsUpdate = true;
                    }

                    if (penalty.penaltyAmount <= 0)
                    {
                        keysToRemove.Add(kvp.Key);
                        localNeedsUpdate = true;
                    }
                }
            }
        }

        if (IsServer)
        {
            // Пакетное удаление
            foreach (var penaltyId in keysToRemove)
            {
                activePenaltiesDict.Remove(penaltyId);
                RemovePenaltyClientRpc(penaltyId);
            }

            if (localNeedsUpdate)
            {
                needsMaxOxygenUpdate = true;
            }
        }
    }
    #endregion

    #region RemovePenalty
    //Уничтожение штрафа с помощью предметов
    [ServerRpc(RequireOwnership = false)]
    public void RemovePenaltyServerRpc(string itemId, float healAmount)
    {
        // Список для полного удаления штрафов
        var keysToRemove = new List<string>();
        bool needsUpdate = false;

        foreach (var kvp in activePenaltiesDict)
        {
            var penalty = kvp.Value;

            // Если предмет подходит для лечения этого штрафа
            if (penalty.cureItem == itemId)
            {
                // Уменьшаем penaltyAmount
                float oldAmount = penalty.penaltyAmount;
                penalty.penaltyAmount -= healAmount;

                Debug.Log($"Healed penalty {penalty.id}: {oldAmount} -> {penalty.penaltyAmount} with {itemId}");

                // Если штраф уменьшился до 0 или меньше - помечаем для удаления
                if (penalty.penaltyAmount <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                    Debug.Log($"Penalty {penalty.id} fully healed and will be removed");
                }

                needsUpdate = true;

                // Синхронизируем изменения с клиентами
                SyncPenaltyClientRpc(penalty.id, penalty.displayName, penalty.penaltyAmount,
                    penalty.isTemporary, penalty.penaltyColor, penalty.cureItem);
            }
        }

        // Удаляем полностью вылеченные штрафы
        foreach (var penaltyId in keysToRemove)
        {
            activePenaltiesDict.Remove(penaltyId);
            RemovePenaltyClientRpc(penaltyId);
        }

        if (needsUpdate)
        {
            needsMaxOxygenUpdate = true;
        }
    }

    //Сообщаем клиенту что штрафа больше нет
    [ClientRpc]
    private void RemovePenaltyClientRpc(string penaltyId)
    {
        // Используем Dictionary
        activePenaltiesDict.Remove(penaltyId);

        oxygenUI?.UpdatePenaltyUI();
    }

    #endregion

    #endregion


    //Дальше идут вспомогательные методы, которые ещё будут изменяться

    public List<OxygenPenalty> GetPenaltiesByCureItem(string id)
    {
        List<OxygenPenalty> result = new();
        foreach (var penalty in activePenaltiesDict.Values)
        {
            if (penalty.cureItem == id && penalty.penaltyAmount > 0)
            {
                result.Add(penalty);
            }
        }
        return result;
    }

    private float oxygenRecoveryTime = 30f; //30 секунд чтобы спасти
    private Coroutine recoveryCoroutine;
    private float requiredOxygenLevel = 5f;
    private void OnOxygenKnock()
    {
        ragdollController.ToggleDie(true);

        if (recoveryCoroutine != null) return;

        recoveryCoroutine = StartCoroutine(WaitAndRecover());
    }

    private IEnumerator WaitAndRecover()
    {
        Debug.Log("Начинается восстановление после отключки...");

        float timer = 0f;

        inventorySystem.DropAllItems();
        oxygenUI.ToggleDeathPanel(true);

        while (timer < oxygenRecoveryTime)
        {
            timer += Time.deltaTime;

            // Проверяем, восстановился ли кислород
            if (currentOxygen.Value >= requiredOxygenLevel)
            {
                Debug.Log($"Кислород восстановлен до {currentOxygen.Value:F1}, поднимаем игрока");
                ragdollController.ToggleDie(false);
                oxygenUI.ToggleDeathPanel(false);
                recoveryCoroutine = null;
                yield break;
            }

            oxygenUI.UpdateDeathUI(timer, oxygenRecoveryTime);


            yield return null;
        }

        // Если время вышло, но кислород восстановился
        if (currentOxygen.Value >= requiredOxygenLevel)
        {
            Debug.Log($"Время вышло, кислород {currentOxygen.Value:F1}, поднимаем игрока");
            ragdollController.ToggleDie(false);
            oxygenUI.ToggleDeathPanel(false);
        }
        else
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            GlobalEventManager.DeathPlayer?.Invoke(localClientId);
            Debug.Log("Player died from oxygen depletion");
            Debug.Log($"Время вышло, но кислород ({currentOxygen.Value:F1}) недостаточен для восстановления");
        }
        recoveryCoroutine = null;
    }

    private void OnOxygenChanged(float oldValue, float newValue)
    {
        if (newValue <= criticalOxygenLevel && oldValue > criticalOxygenLevel)
        {
            OnCriticalOxygen();
        }

        if (newValue <= 1f & currentDopOxygen.Value <= 1f)
        {
            OnOxygenKnock();
        }
        oxygenUI?.UpdateOxygenSlider();
    }

    private void OnMaxOxygenChanged(float oldValue, float newValue)
    {
        oxygenUI?.UpdateOxygenUiPenalty();
    }

    private void OnDopOxygenChanged(float oldValue, float newValue)
    {
        if (newValue <= 1f & currentOxygen.Value <= 1f)
        {
            OnOxygenKnock();
        }

        oxygenUI?.UpdateDopOxygenUI();
    }

    private void OnCriticalOxygen()
    {
        // Эффекты критического уровня кислорода
    }


    public float GetCurrentOxygen() => currentOxygen.Value;
    public float GetMaxOxygen() => currentMaxOxygen.Value;
    public float GetDopOxygen() => currentDopOxygen.Value;



    public bool HasPenalty(string penaltyId) => activePenaltiesDict.ContainsKey(penaltyId);

    public List<OxygenPenalty> GetInfinitePenalties()
    {
        var result = new List<OxygenPenalty>();
        foreach (var penalty in activePenaltiesDict.Values)
        {
            if (penalty.isTemporary)
            {
                result.Add(penalty);
            }
        }
        return result;
    }

    // Восстановление кислорода
    [ServerRpc(RequireOwnership = false)]
    public void RestoreOxygenServerRpc(float amount)
    {
        currentOxygen.Value = Mathf.Min(currentOxygen.Value + amount, currentMaxOxygen.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreOxygenPercentageServerRpc(float percentage)
    {
        float amount = currentMaxOxygen.Value * (percentage / 100f);
        currentOxygen.Value = Mathf.Min(currentOxygen.Value + amount, currentMaxOxygen.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreOxygenToFullServerRpc()
    {
        currentOxygen.Value = currentMaxOxygen.Value;
    }

    public void RestoreOxygen(float amount) => RestoreOxygenServerRpc(amount);
    public void RestoreOxygenPercentage(float percentage) => RestoreOxygenPercentageServerRpc(percentage);
    public void RestoreOxygenToFull() => RestoreOxygenToFullServerRpc();


    [ServerRpc(RequireOwnership = false)]
    public void AddDopOxygenServerRpc(float amount)
    {
        currentDopOxygen.Value = Mathf.Max(0, currentDopOxygen.Value + amount);
    }


}