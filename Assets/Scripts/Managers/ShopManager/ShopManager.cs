using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WaterHorror.Data;




public class ShopManager : NetworkBehaviour
{
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private Transform posSpawn;
    [SerializeField] private Transform parent;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip minusMoney;
    [SerializeField] private AudioClip notMoney;

    private NetworkVariable<int> netWorldId = new(0);

    private ShopUI shopUI;
    private bool isEnable = false;

    private List<InteractiveObject> itemsForUse = new();

    private GameObject currentItem;
    private float currentPrice = 0;
    public bool IsEnable
    {
        get => isEnable;
        set
        {
            if (isEnable == value) return;

            isEnable = value;

            if (isEnable)
            {
                shopUI.ShowPanel();

                GlobalEventManager.BlockMove?.Invoke();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (!isEnable)
            {       
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                shopUI.HidePanel();
            }
        }
    }




    public override void OnNetworkSpawn()
    {

        shopUI = GetComponent<ShopUI>();

        GlobalEventManager.KeyCancel.AddListener(ExitShopMenu);

        if (IsClient)
        {
            netWorldId.OnValueChanged += OnWorldIdChanged;
        }

        if (IsClient) SearhcWorld(0);
    }

    //Cервер
    public void FullShop(int worldId)
    {
        netWorldId.Value = worldId;

        SearhcWorld(worldId);
    }


    //Общий
    private void SearhcWorld(int worldId)
    {
        string name = worldId.ToString();

        var world = Resources.Load<WorldScriptableObject>($"Worlds/{name}");

        itemsForUse = world.spawnItemsForUse;
        
        FullShowcase();
    }

    private void FullShowcase()
    {
        shopUI.ClearShopUI();

        foreach (var item in itemsForUse)
        {
            shopUI.CreateShowcase(item.prefab);
        }
    }

    public void ToggleInteract()
    {
        IsEnable = !IsEnable;
    }


    public void ExitShopMenu()
    {
        IsEnable = false;
    }


    private void OnWorldIdChanged(int oldValue, int newValue)
    {
        SearhcWorld(newValue);
    }

    public void SelectCell(Item item)
    {
        foreach (var selectItem in itemsForUse)
        {
            Item checkId = selectItem.prefab.GetComponent<Item>();
            if (checkId.Id == item.Id)
            {
                currentItem = selectItem.prefab;
                currentPrice = checkId.Price;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectCellOnServerRpc(int itemId)
    {
        foreach (var selectItem in itemsForUse)
        {
            Item checkId = selectItem.prefab.GetComponent<Item>();
            if (checkId.Id == itemId)
            {
                currentItem = selectItem.prefab;
                currentPrice = checkId.Price;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void BuyItemServerRpc()
    {
        if (currentItem == null) return;

        if (coinManager.CoinAmount < currentPrice)
        {
            coinManager.NotEnoughMoney();
            audioSource.clip = notMoney;
            audioSource.Play();
            return; 
        } 

        coinManager.MinusCoinServerRpc(currentPrice);

        GameObject itemInstance = Instantiate(currentItem, posSpawn.position, Quaternion.identity);
        NetworkObject networkObject = itemInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            audioSource.clip = minusMoney;
            audioSource.Play();
            networkObject.Spawn(true);
            networkObject.transform.SetParent(parent);
            Debug.Log($"Spawned enemy at position {posSpawn}");
        }
        else
        {
            Debug.LogError("Enemy prefab doesn't have NetworkObject component!");
            Destroy(itemInstance);
        }
    }
}

