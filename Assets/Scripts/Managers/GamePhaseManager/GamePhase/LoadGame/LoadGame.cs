using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LoadGame : NetworkBehaviour
{
    [SerializeField] private LevelUI levelUI;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private Button nextLevel;
    [SerializeField] private Button startAgain;
    [SerializeField] private CoinManager coinManager;
    private int day = 0;
    private float requirePoints = 0;
    public float RequirePoints => requirePoints;

    public override void OnNetworkSpawn()
    {
        nextLevel.onClick.AddListener(StartNextLevelServerRpc);
        startAgain.onClick.AddListener(RestartGameServerRpc);

        if (IsServer) СalculationScore();
    }

    private void Start()
    {
        LoadNextLevel();
    }

    public void СalculationScore()
    {
        int count = NetworkManager.Singleton.ConnectedClients.Count;

        day++;
        requirePoints = count * 100f * day;

        InitializedClientRpc(day, requirePoints);
    }

    [ClientRpc]
    private void InitializedClientRpc(int day, float requirePoints)
    {
        levelUI.Initialized(day , requirePoints);
    }

    public void StartNextLevel()
    {
        СalculationScore();

        StartNextLevelClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartNextLevelServerRpc()
    {
        СalculationScore();

        StartNextLevelClientRpc();
    }

    [ClientRpc]
    private void StartNextLevelClientRpc()
    {
        GlobalEventManager.RebornPlayer?.Invoke();
        GlobalEventManager.TeleportPos?.Invoke(Vector3.zero);
        eventManager.StartEventManager();
    }

    public void LoadNextLevel()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GlobalEventManager.RebornPlayer?.Invoke();
        GlobalEventManager.TeleportPos?.Invoke(Vector3.zero);
        eventManager.StartEventManager();
    }


    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        day = 0;

        coinManager.ResetCoin();

        СalculationScore();

        StartNextLevelClientRpc();
    }
}
