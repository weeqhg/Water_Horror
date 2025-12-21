using UnityEngine;
using Unity.Netcode;

public class SpawnPlayers : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefabs;

    public void SpawnPlayer(ulong clientId)
    {
        int spawnIndex = NetworkManager.Singleton.ConnectedClients.Count - 1;
        SpawnPlayerSafe(clientId, spawnIndex);
    }

    private void SpawnPlayerSafe(ulong clientId, int spawnIndex)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            Debug.LogWarning($"Client {clientId} not found in ConnectedClients");
            return;
        }

        NetworkObject existingPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (existingPlayer != null)
        {
            Debug.Log($"Player for client {clientId} already exists");
            return;
        }

        if (playerPrefabs == null)
        {
            Debug.LogError("Player prefab is not assigned in GamePlayerSpawner!");
            return;
        }

        Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];
        GameObject playerInstance = Instantiate(playerPrefabs, spawnPoint.position, spawnPoint.rotation);

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"Spawned player for client {clientId} at position {spawnPoint.position}");
    }






    //// Метод для безопасного уничтожения игрока (только на сервере)
    //[ServerRpc(RequireOwnership = false)]
    //public void DestroyPlayerServerRpc(ulong clientId)
    //{
    //    if (!IsServer) return;

    //    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
    //    {
    //        NetworkObject playerObject = client.PlayerObject;
    //        if (playerObject != null)
    //        {
    //            playerObject.Despawn();
    //            Debug.Log($"Destroyed player for client {clientId}");
    //        }
    //    }
    //}

}