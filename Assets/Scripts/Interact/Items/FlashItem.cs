using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class FlashItem : Item
{
    [SerializeField]private FlashPlayer flashPlayer;
    private bool isActive = false;
    private Slider flashSlider;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 1f;
    private NetworkVariable<float> currentBattery = new NetworkVariable<float>();

    private Coroutine myCoroutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentBattery.Value = maxBattery;
        }
        currentBattery.OnValueChanged += OnBatteryChanged;
    }

    public override void SelectItem(GameObject player)
    {
        if (currentBattery.Value <= 0) return;
        if (flashPlayer == null) flashPlayer = player.GetComponent<FlashPlayer>();
        isActive = true;
        BatteryDrainServerRpc(true);
        flashPlayer.OnFlashPlayer(true);
    }

    public override void InteractItem()
    {
        if (flashPlayer == null || currentBattery.Value <= 0) return;
        isActive = !isActive;

        BatteryDrainServerRpc(isActive);

        flashPlayer.OnFlashPlayer(isActive);
    }

    public override void ChangeItem()
    {
        if (flashPlayer == null || currentBattery.Value <= 0) return;
        isActive = false;
        BatteryDrainServerRpc(false);
        flashPlayer.OnFlashPlayer(false);
    }

    public override void DropItem()
    {
        if (flashPlayer == null) return;
        flashPlayer.OnFlashPlayer(false);
        BatteryDrainServerRpc(false);
        flashPlayer = null;
        isActive = false;
    }

    public override void GetSlider(Slider slider)
    {
        flashSlider = slider;
        flashSlider.maxValue = maxBattery;
        flashSlider.value = currentBattery.Value;
    }



    [ServerRpc(RequireOwnership = false)]
    public void BatteryDrainServerRpc(bool enable)
    {
        if (!IsServer) return;
        // Теперь это выполняется на сервере
        if (enable)
        {
            if (myCoroutine != null) StopCoroutine(myCoroutine);
            myCoroutine = StartCoroutine(StartDrain());
        }
        else
        {
            if (myCoroutine != null)
            {
                StopCoroutine(myCoroutine);
                myCoroutine = null;
            }
        }
    }

    private IEnumerator StartDrain()
    {
        Debug.Log("Запуск на сервере");
        while (currentBattery.Value > 0 && isActive)
        {
            yield return new WaitForSeconds(batteryDrainRate);
            currentBattery.Value -= 1; // ✅ Теперь на сервере - нет ошибки
            Debug.Log(currentBattery.Value);

            if (currentBattery.Value <= 0)
            {
                FlashlightOffClientRpc();
                break;
            }
        }
    }

    [ClientRpc]
    private void FlashlightOffClientRpc()
    {
        isActive = false;
        if (flashPlayer != null)
        {
            flashPlayer.OnFlashPlayer(false);
        }
    }

    private void OnBatteryChanged(float oldValue, float newValue)
    {
        if (flashSlider != null)
        {
            flashSlider.value = newValue;
        }
    }

}