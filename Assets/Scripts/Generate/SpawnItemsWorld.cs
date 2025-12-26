using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using WaterHorror.Data;


public class SpawnItems : MonoBehaviour
{
    [Header("Center Safe Zone")]
    [Tooltip("Радиус центральной зоны, где объекты не будут спавниться")]
    public float centerSafeZoneRadius = 25f;
    [Tooltip("Включить безопасную зону в центре")]
    public bool enableCenterSafeZone = true;


    [SerializeField] private float posY = 60f;
    [SerializeField] private int maxTotalItems = 0;


    private List<InteractiveObject> items = new();

    private float terrainSize = 500f;

    private Vector3 terrainCenter;

    private List<Vector3> spawnPoints = new();


    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        if (world == null) return;

        terrainSize = world.terrainSize;
        items.AddRange(world.spawnItemsForSell);
        items.AddRange(world.spawnItemsForUse);

        terrainCenter = transform.position + new Vector3(terrainSize / 2f, 0f, terrainSize / 2f);
    }

    public void StartSpawnItems()
    {
        GeneratePosition();

        SpawnItemsOnWorld();
    }


    private void GeneratePosition()
    {
        spawnPoints.Clear();


        foreach (var item in items)
        {
            for (int i = 0; i < item.count; i++)
            {
                if (spawnPoints.Count >= maxTotalItems) return;

                Vector3? validPosition = FindValidSpawnPosition();

                if (validPosition.HasValue)
                {
                    spawnPoints.Add(validPosition.Value);
                }

            }
        }
    }


    private Vector3? FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float spawnX = Random.Range(0, terrainSize) + transform.position.x;
            float spawnZ = Random.Range(0, terrainSize) + transform.position.z;

            Vector3 potentialPosition = new(spawnX, 0, spawnZ);

            if (enableCenterSafeZone && IsPositionInSafeZone(potentialPosition))
            {
                continue; // Пропускаем эту позицию
            }

            return potentialPosition;
        }

        return null;
    }

    private void SpawnItemsOnWorld()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points generated");
            return;
        }

        int spawnIndex = 0;

        foreach (var item in items)
        {
            for (int i = 0; i < item.count; i++)
            {
                if (spawnIndex >= spawnPoints.Count) break;

                Vector3 spawnPos = spawnPoints[spawnIndex];
                Vector3 newSpawnPos = new Vector3(spawnPos.x, posY, spawnPos.z);

                SpawnItemsAtPositionSafe(item.prefab, newSpawnPos);

                spawnIndex++;
            }
        }
    }



    private void SpawnItemsAtPositionSafe(GameObject item, Vector3 position)
    {
        GameObject itemInstance = Instantiate(item, position, Quaternion.identity);
        NetworkObject networkObject = itemInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(true);
            networkObject.transform.SetParent(transform);
            Debug.Log($"Spawned enemy at position {position}");
        }
        else
        {
            Debug.LogError("Enemy prefab doesn't have NetworkObject component!");
            Destroy(itemInstance);
        }
    }


    private bool IsPositionInSafeZone(Vector3 position)
    {
        // Игнорируем высоту Y для проверки расстояния по горизонтали
        Vector2 centerXZ = new Vector2(terrainCenter.x, terrainCenter.z);
        Vector2 positionXZ = new Vector2(position.x, position.z);

        float distanceToCenter = Vector2.Distance(centerXZ, positionXZ);

        return distanceToCenter <= centerSafeZoneRadius;
    }
}

