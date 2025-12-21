using UnityEngine;
using Unity.Netcode;

public class LoadGame : NetworkBehaviour
{
    [SerializeField] private SpawnPlayers spawnPlayers;
    private EventManager visualEffectActive;


    private ulong localClientId;

    private void Start()
    {
        visualEffectActive = GetComponent<EventManager>();
    }


    public void StartLoad(ulong value)
    {
        localClientId = value;

        spawnPlayers.SpawnPlayer(localClientId);

        StartLoadClientRpc(localClientId);
    }

    [ClientRpc]
    private void StartLoadClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        visualEffectActive.StartEventManager();
    }
}
