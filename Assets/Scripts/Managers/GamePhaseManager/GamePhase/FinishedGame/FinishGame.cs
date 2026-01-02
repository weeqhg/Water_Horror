using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class FinishGame : NetworkBehaviour
{
    [SerializeField] private SubmarineMain submarineMain;
    [SerializeField] private CreateWorld createWorld;
    [SerializeField] private LoadGame loadGame;
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private EventManager eventManager;

    [SerializeField] private GameObject winMenu;
    [SerializeField] private GameObject lossMenu;
    private bool isWin = false;

    private int currentDeathPlayer = 0;
    private bool isCounting = true;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GlobalEventManager.DeathPlayer.AddListener(DeathPlayerCounterServerRpc);
        }

        winMenu.SetActive(false);
        lossMenu.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            GlobalEventManager.DeathPlayer.RemoveListener(DeathPlayerCounterServerRpc);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeathPlayerCounterServerRpc(ulong playerId)
    {
        if (!isCounting) return;

        currentDeathPlayer++;

        // Получаем количество активных игроков
        int activePlayers = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            // Пропускаем отключившихся игроков
            var playerObj = NetworkManager.SpawnManager?.GetPlayerNetworkObject(client.Key);
            if (playerObj != null && playerObj.IsSpawned)
            {
                activePlayers++;
            }
        }

        Debug.Log($"Death: {currentDeathPlayer}/{activePlayers}");

        if (currentDeathPlayer >= activePlayers)
        {
            isCounting = false; // Останавливаем подсчет
            FinishedGame();
        }
    }
    public void FinishedGame()
    {
        if (coinManager.CoinAmount >= loadGame.RequirePoints)
        {
            isWin = true;
            loadGame.StartNextLevel();
        }
        else
        {
            isWin = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            lossMenu.SetActive(true);
        }

        createWorld.ClearWorldForServer();

        FinishGameForClientRpc();

        // Сбрасываем через некоторое время
        Invoke(nameof(ResetCounter), 5f);
    }

    [ClientRpc]
    private void FinishGameForClientRpc()
    {
        eventManager.StartEventManager();

        createWorld.ClearWorldForClient();
    }

    private void ResetCounter()
    {
        currentDeathPlayer = 0;
        isCounting = true;
    }
}
