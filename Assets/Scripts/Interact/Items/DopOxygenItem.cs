using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DopOxygenItem : Item
{
    private OxygenSystem oxygenSystem;
    private Slider oxygenSlider;
    [SerializeField] private float maxOxygenScore = 30f;
    private NetworkVariable<float> oxygenScore = new NetworkVariable<float>();

    private Coroutine myCoroutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            oxygenScore.Value = maxOxygenScore;
        }
        oxygenScore.OnValueChanged += OnOxygenChanged;
    }

    public override void SelectItem(GameObject player)
    {
        if (oxygenScore.Value <= 0) return;
        if (oxygenSystem == null) oxygenSystem = player.GetComponent<OxygenSystem>();

    }

    public override void InteractItem()
    {
        if (oxygenSystem == null || oxygenScore.Value <= 0) return;

        UseOxygenServerRpc(oxygenSystem.GetComponent<NetworkObject>().NetworkObjectId);
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
        oxygenSlider = slider;
        oxygenSlider.maxValue = maxOxygenScore;
        oxygenSlider.value = oxygenScore.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseOxygenServerRpc(ulong oxygenSystemId)
    {
        if (!IsServer || oxygenScore.Value <= 0) return;

        // Находим oxygenSystem по ID на сервере
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            oxygenSystemId, out NetworkObject oxygenSystemObject))
        {
            OxygenSystem oxygenSystem = oxygenSystemObject.GetComponent<OxygenSystem>();

            if (oxygenSystem != null)
            {
                float availableCapacity = oxygenSystem.AvailableCapacityDopOxygen();
                float amountToAdd = Mathf.Min(oxygenScore.Value, availableCapacity);

                if (amountToAdd > 0)
                {
                    oxygenSystem.AddDopOxygenServerRpc(amountToAdd);
                    oxygenScore.Value -= amountToAdd;
                }
            }
        }
    }


    private void OnOxygenChanged(float oldValue, float newValue)
    {
        if (oxygenSlider != null)
            oxygenSlider.value = newValue;
    }
}
