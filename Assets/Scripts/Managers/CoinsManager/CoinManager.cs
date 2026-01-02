using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class CoinManager : NetworkBehaviour
{
    [SerializeField] private CoinCounterUI coinCounterUI;
    [SerializeField] private List<CoinCounter> coinCounters;
    [SerializeField] private NetworkVariable<float> coins = new(0f);

    public float CoinAmount => coins.Value;


    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            coins.OnValueChanged += OnCoinsChanged;
        }

        coinCounterUI = GetComponent<CoinCounterUI>();

        if (coinCounters.Count > 0)
        {
            foreach (CoinCounter coinCounter in coinCounters)
            {
                coinCounter.Initialized(this);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlusCoinServerRpc(float value)
    {
        if (!IsServer) return;

        coins.Value += value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MinusCoinServerRpc(float value)
    {
        if (!IsServer) return;

        coins.Value -= value;
    }
  

    public void NotEnoughMoney()
    {
        coinCounterUI.NotMoney();
    }

    private void OnCoinsChanged(float oldValue, float newValue)
    {
        coinCounterUI.UpdateUI(newValue);
    }

    public void ResetCoin()
    {
        coins.Value = 0f;
    }
}
