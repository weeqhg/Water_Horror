using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


/// <summary>
/// Главный менеджер здесь будет происходить всё управление игрой.
/// управление происходит через смены фаз
/// </summary>
public class GameManager : NetworkBehaviour
{
    [SerializeField] private GamePhaseManager gamePhaseManager;



    //Локальный Id Подключенного клиента
    [SerializeField] private ulong clientId = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnNewClientConnected;
            gamePhaseManager.Initialize();
        }
    }

    //Тут при подключение новых игроков требуется проверка какая сейчас стадия игры, чтобы определять что делать с игроком
    private void OnNewClientConnected(ulong value)
    {
        this.clientId = value;
        Debug.Log($"Подключен новый игрок с {clientId}");

        gamePhaseManager.ProcessConnectedClient(clientId);
    }


    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnNewClientConnected;
    }

}
