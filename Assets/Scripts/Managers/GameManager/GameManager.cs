using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GamePhaseManager gamePhaseManager;
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private ShopManager shopManager;

    // Для отслеживания клиентов
    private HashSet<ulong> processedClients = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GlobalEventManager.StartGame.AddListener(StartGame);
            NetworkManager.Singleton.OnClientConnectedCallback += OnNewClientConnected;

            // Обрабатываем уже подключенных клиентов (кроме хоста)
            StartCoroutine(InitializeExistingClients());
        }
    }

    private IEnumerator InitializeExistingClients()
    {
        yield return null; // Ждем один кадр для инициализации

        Debug.Log($"Initializing existing clients. Count: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Обрабатываем всех уже подключенных клиентов
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (!processedClients.Contains(client.Key))
            {
                SpawnClient(client.Key);
                processedClients.Add(client.Key);
            }
        }
    }

    // Логика спавна 
    private void SpawnClient(ulong clientId)
    {
        Debug.Log($"Spawning regular client with ID: {clientId}");

        // Обычные клиенты спавнятся по другой логике
        gamePhaseManager.LoadGame(clientId);
    }

   

    private void OnNewClientConnected(ulong clientId)
    {
        Debug.Log($"New client connected: {clientId}");

        // Проверяем, не обрабатывали ли уже этого клиента
        if (processedClients.Contains(clientId))
        {
            Debug.Log($"Client {clientId} already processed");
            return;
        }

        // Спавним нового клиента
        SpawnClient(clientId);
        processedClients.Add(clientId);
    }

    private void StartGame(int worldId)
    {
        if (!IsServer) return;

        Debug.Log($"Starting game with world ID: {worldId}");

        // Запускаем игру для всех игроков
        gamePhaseManager.StartGame(worldId);
        shopManager.FullShop(worldId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnNewClientConnected;
        GlobalEventManager.StartGame.RemoveListener(StartGame);

        processedClients.Clear();

        Debug.Log("GameManager despawned");
    }
}