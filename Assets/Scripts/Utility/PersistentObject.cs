using UnityEngine;
using Unity.Netcode;

public class PersistentNetObject : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DontDestroyOnLoad(gameObject);
        }
        base.OnNetworkSpawn();
    }
}