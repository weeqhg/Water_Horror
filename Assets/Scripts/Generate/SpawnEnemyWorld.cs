using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WaterHorror.Data;

public class SpawnEnemyWorld : NetworkBehaviour
{
    [Header("Center Safe Zone")]
    [Tooltip("Радиус центральной зоны, где объекты не будут спавниться")]
    public float centerSafeZoneRadius = 25f;
    [Tooltip("Включить безопасную зону в центре")]
    public bool enableCenterSafeZone = true;

    [SerializeField] private int maxTotalEnemy = 0;


    private List<InteractiveObject> enemies = new();
    private float terrainSize = 500f;

    private Vector3 terrainCenter;

    private List<Vector3> spawnPoints = new();


    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        if (world == null) return;

        terrainSize = world.terrainSize;
        enemies = world.spawnEnemies;

        terrainCenter = transform.position + new Vector3(terrainSize / 2f, 0f, terrainSize / 2f);
    }

    public void StartSpawnEnemy()
    {
        GeneratePosition();

        SpawnEnemies();
    }


    private void GeneratePosition()
    {
        spawnPoints.Clear();


        foreach (var enemy in enemies)
        {
            for (int i = 0; i < enemy.count; i++)
            {
                if (spawnPoints.Count >= maxTotalEnemy) return;

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
            float spawnX = Random.Range(0, terrainSize / 2) + transform.position.x;
            float spawnZ = Random.Range(0, terrainSize / 2) + transform.position.z;

            Vector3 potentialPosition = new(spawnX, 0, spawnZ);

            if (enableCenterSafeZone && IsPositionInSafeZone(potentialPosition))
            {
                continue; // Пропускаем эту позицию
            }

            return potentialPosition;
        }

        return null;
    }

    private void SpawnEnemies()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points generated");
            return;
        }

        int spawnIndex = 0;

        foreach (var enemy in enemies)
        {
            for (int i = 0; i < enemy.count; i++)
            {
                if (spawnIndex >= spawnPoints.Count) break;

                Vector3 spawnPos = spawnPoints[spawnIndex];
                SpawnEnemyAtPositionSafe(enemy.prefab, spawnPos);

                spawnIndex++;
            }
        }
    }



    private void SpawnEnemyAtPositionSafe(GameObject enemy, Vector3 position)
    {
        GameObject enemyInstance = Instantiate(enemy, position, Quaternion.identity);
        NetworkObject networkObject = enemyInstance.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(true);
            Debug.Log($"Spawned enemy at position {position}");
        }
        else
        {
            Debug.LogError("Enemy prefab doesn't have NetworkObject component!");
            Destroy(enemyInstance);
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
