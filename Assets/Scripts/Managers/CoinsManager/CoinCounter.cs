using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    private CoinManager coinManager;
    [SerializeField] private AudioSource audioSource;
    public void Initialized(CoinManager coinManager)
    {
        this.coinManager = coinManager;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {      
            Item item = other.gameObject.GetComponentInParent<Item>();

            if (item != null)
            {
                Debug.Log(coinManager);
                if (coinManager != null) coinManager.PlusCoinServerRpc(item.Price);
                if (audioSource != null) audioSource.Play();
                item.HideItemServerRpc();
            }
        }
    }
}
