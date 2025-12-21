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
    [SerializeField] private LoadGame loadGame;
    [SerializeField] private StartGame startGame;
    private NetworkVariable<GamePhase> currentGamePhase = new(GamePhase.LoadGame);


    private ulong localClientId = 0;
    public void Initialize()
    {
        GlobalEventManager.LoadGame.AddListener(LoadGame);
        GlobalEventManager.StartGame.AddListener(StartGame);
    }

    public void ProcessConnectedClient(ulong value)
    {
        localClientId = value;

        if (currentGamePhase.Value == GamePhase.LoadGame) GlobalEventManager.LoadGame?.Invoke();
    }


    private void LoadGame()
    {
        loadGame.StartLoad(localClientId);
    }

    private void StartGame(int worldId)
    {
        startGame.StartCurrentGame(worldId);
    }


 
}
