using Unity.Netcode;
using UnityEngine;

public class CustomNetworkManager : NetworkBehaviour
{
    [SerializeField] private MainMenuController menuController;
    
    private void Start()
    {
        // Настройка NetworkManager
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
        
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"Host started, client {clientId} connected");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
    }
}