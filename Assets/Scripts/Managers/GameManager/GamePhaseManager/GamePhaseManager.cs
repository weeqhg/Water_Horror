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
    private NetworkVariable<GamePhase> currentGamePhase = new(GamePhase.LoadGame);

    public void ProcessConnectedClient(ulong value)
    {
        if (currentGamePhase.Value == GamePhase.LoadGame) GlobalEventManager.LoadGame?.Invoke();
    }

    public void LoadGame(ulong value)
    {
        spawnPlayers.SpawnPlayer(value);
        loadGame.StartLoad(value);
    }

    public void StartGame(int worldId)
    {
        startGame.StartCurrentGame(worldId);
    }


 
}
