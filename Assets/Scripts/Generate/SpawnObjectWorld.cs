using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WaterHorror.Data;
public class SpawnObjectWorld : MonoBehaviour
{
    [Header("Center Safe Zone")]
    [Tooltip("Радиус центральной зоны, где объекты не будут спавниться")]
    public float centerSafeZoneRadius = 25f;
    [Tooltip("Включить безопасную зону в центре")]
    public bool enableCenterSafeZone = true;

    [SerializeField] private int maxTotalObjects = 1000;

    private Terrain terrain;
    private Vector3 terrainCenter;


    private float terrainSize = 500f;
    private List<SpawnObject> spawnObjectDatas = new();

    private System.Random deterministicRandom;


    // Object Spawning
    private List<GameObject> spawnedObjects = new();
    private int currentTotalObjects = 0;


    // Для хранения сгенерированных данных
    private List<DeterministicObjectData> generatedObjectData = new();


    [System.Serializable]
    public struct DeterministicObjectData
    {
        public int prefabIndex;     // Индекс в spawnObjects
        public Vector3 position;    // Абсолютная позиция
        public float rotationY;     // Вращение по Y
        public float scale;         // Масштаб
    }

    public void Initialized(Terrain terrain)
    {
        this.terrain = terrain;
    }


    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        if (world == null) return;

        terrainSize = world.terrainSize;
        spawnObjectDatas = world.spawnObjects;

        terrainCenter = new Vector3(terrainSize / 2f, 0f, terrainSize / 2f);

        Debug.Log($"X: {terrainCenter.x}, Z: {terrainCenter.z}");
    }

    public void SpawnObject(int seed)
    {
        deterministicRandom = new System.Random(seed);


        ClearAllObjects();

        GenerateAllObjectData();

        SpawnFromGeneratedData();
    }











    [ContextMenu("Clear All Objects")]
    public void ClearAllObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        currentTotalObjects = 0;
        Debug.Log("All objects cleared");
    }

    private void GenerateAllObjectData()
    {
        generatedObjectData.Clear();
        int prefabIndex = 0;

        // Создаем ОТДЕЛЬНЫЙ random для позиций
        System.Random positionRandom = deterministicRandom;

        // И ОТДЕЛЬНЫЙ random для параметров объекта
        // Используем seed + смещение чтобы не пересекаться
        System.Random paramRandom = deterministicRandom;

        foreach (var spawn in spawnObjectDatas)
        {
            int objectCount = spawn.count;

            for (int i = 0; i < objectCount; i++)
            {
                if (currentTotalObjects >= maxTotalObjects) break;

                Vector3? position = null;

                for (int attempt = 0; attempt < 10; attempt++)
                {
                    float spawnX = (float)positionRandom.NextDouble() * terrainSize + transform.position.x;
                    float spawnZ = (float)positionRandom.NextDouble() * terrainSize + transform.position.z;

                    Vector3 terrainCheckPos = new(spawnX, 0, spawnZ);
                    float terrainHeight = terrain.SampleHeight(terrainCheckPos);

                    Vector3 worldPos = new(spawnX, terrainHeight + spawn.minHeight, spawnZ);

                    // ПРОВЕРКА НА НАХОЖДЕНИЕ В БЕЗОПАСНОЙ ЗОНЕ
                    if (enableCenterSafeZone && IsPositionInSafeZone(worldPos))
                    {
                        continue; // Пропускаем эту позицию
                    }

                    if (!IsPositionOccupiedInGeneratedData(worldPos, spawn.spawnDensity))
                    {
                        // ПРОВЕРКА РЕЛЬЕФА ДЛЯ ОБЪЕКТОВ С РАДИУСОМ
                        if (spawn.radius > 0.1f)
                        {

                            // Используем самую низкую точку для позиции Y
                            Vector3 adjustedPos = new Vector3(
                                worldPos.x,
                                IsTerrainFlatEnough(terrainCheckPos, spawn.radius) + spawn.minHeight, // Добавляем minHeight к самой низкой точке
                                worldPos.z
                            );
                            position = adjustedPos;


                        }
                        else
                        {
                            // Для объектов без радиуса используем обычную позицию
                            position = worldPos;
                            break;
                        }
                    }
                }

                if (position.HasValue)
                {
                    // Параметры генерируются из ДРУГОГО random - не зависит от попыток позиции!
                    float rotationY = (float)paramRandom.NextDouble() * 360f;
                    float scaleValue = 1f;

                    if (spawn.randomScale != 0)
                    {
                        scaleValue = 1f + ((float)paramRandom.NextDouble() * 2f - 1f) * spawn.randomScale;
                    }

                    generatedObjectData.Add(new DeterministicObjectData
                    {
                        prefabIndex = prefabIndex,
                        position = position.Value,
                        rotationY = rotationY,
                        scale = scaleValue
                    });

                    currentTotalObjects++;
                }
            }

            prefabIndex++;
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

    private bool IsPositionOccupiedInGeneratedData(Vector3 position, float checkRadius)
    {
        foreach (var data in generatedObjectData)
        {
            if (Vector3.Distance(data.position, position) < checkRadius)
            {
                return true;
            }
        }
        return false;
    }

    private float IsTerrainFlatEnough(Vector3 centerPosition, float radius)
    {
        // Количество точек для проверки по окружности
        int checkPoints = 20; // Увеличим для лучшего покрытия

        // Получаем высоты во всех точках
        List<float> heights = new List<float>();

        // Центральная точка
        float centerHeight = terrain.SampleHeight(centerPosition);
        heights.Add(centerHeight);

        // Точки по окружности
        for (int i = 0; i < checkPoints; i++)
        {
            float angle = i * (360f / checkPoints);
            float x = centerPosition.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = centerPosition.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            Vector3 checkPos = new Vector3(x, 0, z);
            float checkHeight = terrain.SampleHeight(checkPos);
            heights.Add(checkHeight);
        }

        // Дополнительные точки внутри радиуса
        System.Random checkRandom = new System.Random(deterministicRandom.Next());
        for (int i = 0; i < 6; i++)
        {
            float randomAngle = (float)checkRandom.NextDouble() * 360f;
            float randomRadius = (float)checkRandom.NextDouble() * radius;

            float x = centerPosition.x + Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomRadius;
            float z = centerPosition.z + Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomRadius;

            Vector3 checkPos = new Vector3(x, 0, z);
            float checkHeight = terrain.SampleHeight(checkPos);
            heights.Add(checkHeight);
        }

        // Находим минимальную и максимальную высоту
        float minHeight = heights.Min();

        // Сохраняем самую низкую точку
        return minHeight;
    }










    private void SpawnFromGeneratedData()
    {
        foreach (var data in generatedObjectData)
        {
            if (data.prefabIndex >= 0 && data.prefabIndex < spawnObjectDatas.Count)
            {
                var spawn = spawnObjectDatas[data.prefabIndex];
                SpawnObjectFromData(spawn, data);
            }
        }
    }

    private void SpawnObjectFromData(SpawnObject spawn, DeterministicObjectData data)
    {
        GameObject spawnedObject = Instantiate(spawn.prefab, data.position, Quaternion.identity);
        spawnedObject.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
        spawnedObject.transform.localScale = new Vector3(data.scale, data.scale, data.scale);
        spawnedObject.transform.SetParent(transform);
        spawnedObjects.Add(spawnedObject);

    }
}
