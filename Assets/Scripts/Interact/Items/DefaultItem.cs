using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DefaultItem : Item
{
    [SerializeField] private float value = 0f;
    private OxygenSystem oxygenSystem;
    private Slider slider;
    private NetworkVariable<float> currentValue = new NetworkVariable<float>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentValue.Value = value;
        }
        currentValue.OnValueChanged += OnValueChanged;
    }

    public override void SelectItem(GameObject player)
    {
        if (currentValue.Value <= 0) return;
        if (oxygenSystem == null) oxygenSystem = player.GetComponent<OxygenSystem>();

    }

    public override void InteractItem()
    {
        if (oxygenSystem == null || currentValue.Value <= 0) return;

        //UseDefaultServerRpc(oxygenSystem.GetComponent<NetworkObject>().NetworkObjectId);
    }

    public override void ChangeItem()
    {
        
    }

    public override void DropItem()
    {
        if (oxygenSystem == null) return;
        oxygenSystem = null;
    }

    public override void GetSlider(Slider slider)
    {
        this.slider = slider;
        this.slider.maxValue = value;
        this.slider.value = currentValue.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseDefaultServerRpc(ulong oxygenSystemId)
    {
        if (!IsServer || currentValue.Value <= 0) return;

        // Находим oxygenSystem по ID на сервере
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            oxygenSystemId, out NetworkObject oxygenSystemObject))
        {
            OxygenSystem oxygenSystem = oxygenSystemObject.GetComponent<OxygenSystem>();

            // Получаем конкретный штраф для лечения
            List<OxygenSystem.OxygenPenalty> healablePenalties = oxygenSystem.GetPenaltiesByCureItem("aid");

            if (healablePenalties.Count > 0)
            {
                float remainingHealth = currentValue.Value;

                foreach (var penalty in healablePenalties)
                {
                    if (remainingHealth <= 0) break;

                    float healAmount = Mathf.Min(penalty.penaltyAmount, remainingHealth);
                    oxygenSystem.RemovePenaltyServerRpc("heal", healAmount);
                    remainingHealth -= healAmount;

                    Debug.Log($"Healed {healAmount} from penalty {penalty.id}");
                }

                currentValue.Value = remainingHealth;
                Debug.Log($"Remaining health in aid: {currentValue.Value}");
            }
            else
            {
                Debug.Log("No healable penalties found");
            }
        }
    }


    private void OnValueChanged(float oldValue, float newValue)
    {
        if (slider != null)
            slider.value = newValue;
    }

}
