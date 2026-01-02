using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public enum GamePhase
{
    LoadGame,
    StartGame,
    FinishGame,
    GameOver
}

public class GamePhaseManager : NetworkBehaviour
{
    [SerializeField] private SpawnPlayers spawnPlayers;
    [SerializeField] private LoadGame loadGame;
    [SerializeField] private StartGame startGame;
    [SerializeField] private FinishGame finishGame;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GlobalEventManager.StartGame.AddListener(StartGame);
            GlobalEventManager.LoadGame.AddListener(LoadGame);
            GlobalEventManager.FinishedGame.AddListener(FinishGame);
        }
    }

    private void LoadGame(ulong value)
    {
        spawnPlayers.SpawnPlayer(value);
    }

    private void StartGame(int worldId)
    {
        startGame.StartCurrentGame(worldId);
    }

    private void FinishGame()
    {
        finishGame.FinishedGame();
    }

    public override void OnNetworkDespawn()
    {
        GlobalEventManager.StartGame.RemoveListener(StartGame);
    }
}
