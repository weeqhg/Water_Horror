using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class AidItem : Item
{
    private OxygenSystem oxygenSystem;
    private Slider aidSlider;
    [SerializeField] private float maxHealthScore = 30f;
    private NetworkVariable<float> healthScore = new NetworkVariable<float>();

    private Coroutine myCoroutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            healthScore.Value = maxHealthScore;
        }
        healthScore.OnValueChanged += OnAidChanged;
    }

    public override void SelectItem(GameObject player)
    {
        if (healthScore.Value <= 0) return;
        if (oxygenSystem == null) oxygenSystem = player.GetComponent<OxygenSystem>();

    }

    public override void InteractItem()
    {
        if (oxygenSystem == null || healthScore.Value <= 0) return;

        UseAidServerRpc(oxygenSystem.GetComponent<NetworkObject>().NetworkObjectId);
    }

    public override void ChangeItem()
    {
        //if (flashPlayer == null || currentBattery.Value <= 0) return;
        //isActive = false;
        //BatteryDrainServerRpc(false);
        //flashPlayer.OnFlashPlayer(false);
    }

    public override void DropItem()
    {
        if (oxygenSystem == null) return;
        //currentPlayer = null;
        oxygenSystem = null;
    }

    public override void GetSlider(Slider slider)
    {
        aidSlider = slider;
        aidSlider.maxValue = maxHealthScore;
        aidSlider.value = healthScore.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseAidServerRpc(ulong oxygenSystemId)
    {
        if (!IsServer || healthScore.Value <= 0) return;

        // Находим oxygenSystem по ID на сервере
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            oxygenSystemId, out NetworkObject oxygenSystemObject))
        {
            OxygenSystem oxygenSystem = oxygenSystemObject.GetComponent<OxygenSystem>();

            // Получаем конкретный штраф для лечения
            List<OxygenSystem.OxygenPenalty> healablePenalties = oxygenSystem.GetPenaltiesByCureItem(nameId);

            if (healablePenalties.Count > 0)
            {
                float remainingHealth = healthScore.Value;

                foreach (var penalty in healablePenalties)
                {
                    if (remainingHealth <= 0) break;

                    float healAmount = Mathf.Min(penalty.penaltyAmount, remainingHealth);
                    oxygenSystem.RemovePenaltyServerRpc(nameId, healAmount);
                    remainingHealth -= healAmount;

                    Debug.Log($"Healed {healAmount} from penalty {penalty.id}");
                }

                healthScore.Value = remainingHealth;
                Debug.Log($"Remaining health in aid: {healthScore.Value}");
            }
            else
            {
                Debug.Log("No healable penalties found");
            }
        }
    }


    private void OnAidChanged(float oldValue, float newValue)
    {
        if (aidSlider != null)
            aidSlider.value = newValue;
    }

}