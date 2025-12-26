using Unity.Netcode;
using UnityEngine;

public class LoadGame : NetworkBehaviour
{
    private EventManager visualEffectActive;


    private ulong localClientId;

    private void Start()
    {
        visualEffectActive = GetComponent<EventManager>();
    }


    public void StartLoad(ulong clientId)
    {
        if (!IsServer) return;

        // Определяем тип клиента
        bool isHost = clientId == NetworkManager.Singleton.LocalClientId;

        if (isHost)
        {
            // Хост - прямая активация
            visualEffectActive.StartEventManager();
        }
        else
        {
            // Клиент - через RPC
            SendLoadRpcToClient(clientId);
        }
    }
    private void SendLoadRpcToClient(ulong clientId)
    {
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        StartLoadClientRpc(clientId, clientRpcParams);
    }

    [ClientRpc]
    public void StartLoadClientRpc(ulong value, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        visualEffectActive.StartEventManager();
    }
}
