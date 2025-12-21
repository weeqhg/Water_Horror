using Unity.Netcode;
using UnityEngine;

public class NetworkManagerAutoSpawnDisabler : MonoBehaviour
{
    private void Start()
    {
        // Отключаем автоматический спавн игроков
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted()
    {
        // Отключаем автоматическое создание игрока для хоста
        if (NetworkManager.Singleton.IsHost)
        {
            // Хост не будет автоматически спавнить своего игрока
        }
    }
}