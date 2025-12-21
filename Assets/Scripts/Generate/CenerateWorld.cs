using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WaterHorror.Data;

public class GenerateWorld: NetworkBehaviour
{
    private int width = 512;
    private int height = 512;
    private float terrainSize = 500f;

    private float scale = 50f;
    private float heightMultiplier = 80f;

    private int octaves = 4;
    private float persistence = 0.5f;
    private float lacunarity = 2f;

    private Material terrainMaterial;

    [Header("Collider Settings")]
    public bool generateCollider = true;
    public PhysicMaterial physicMaterial;

    [Header("Randomization Settings")]
    public bool randomizeOnStart = true;
    [Range(0, 1000f)] public float randomOffsetRange;

    [Header("Network Settings")]
    public bool generateOnServer = true;

    [Header("Object Spawning Settings")]
    [SerializeField] private List<SpawnObject> spawnObjects = new();
    public bool spawnObjectsOnStart = true;
    public int maxTotalObjects = 200;

    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;
    private TerrainData terrainData;
    private float[,] heightmapData;

    // Network Variables
    [SerializeField] private NetworkVariable<Vector2> networkOffset = new();
    [SerializeField] private NetworkVariable<int> objectGenerationSeed = new();
    [SerializeField] private NetworkVariable<int> networkCurrentLocationId = new(-1);

    // Object Spawning
    private List<GameObject> spawnedObjects = new();
    private int currentTotalObjects = 0;

    // Детерминистический генератор
    private System.Random deterministicRandom;
    private Vector2 offset;
    private int currentLocationId = -1;

    // Флаги готовности
    private bool isOffsetReady = false;
    private bool isSeedReady = false;


    private string depth;
    public string GetDepth() => depth;

    [Header("Center Safe Zone")]
    [Tooltip("Радиус центральной зоны, где объекты не будут спавниться")]
    public float centerSafeZoneRadius = 25f;
    [Tooltip("Включить безопасную зону в центре")]
    public bool enableCenterSafeZone = true;



    // Центр террейна (для безопасной зоны)
    private Vector3 terrainCenter;

    [System.Serializable]
    public struct DeterministicObjectData
    {
        public int prefabIndex;     // Индекс в spawnObjects
        public Vector3 position;    // Абсолютная позиция
        public float rotationY;     // Вращение по Y
        public float scale;         // Масштаб
    }

    // Для хранения сгенерированных данных
    private List<DeterministicObjectData> generatedObjectData = new();

    #region Инициализация
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            networkOffset.OnValueChanged += OnOffsetChanged;
            objectGenerationSeed.OnValueChanged += OnSeedChanged;
            networkCurrentLocationId.OnValueChanged += OnLocationIdChanged;

            StartCoroutine(GenerateForLateClient());
        }
    }

    private IEnumerator GenerateForLateClient()
    {
        Debug.Log("Late client: Starting generation with existing data...");

        // Даем время NetworkVariable синхронизироваться
        yield return new WaitForSeconds(0.5f);

        // Устанавливаем значения
        offset = networkOffset.Value;

        // Инициализируем random
        deterministicRandom = new System.Random(objectGenerationSeed.Value);

        currentLocationId = networkCurrentLocationId.Value;

        if (currentLocationId == -1) yield break;

        string name = currentLocationId.ToString();
        var location = Resources.Load<WorldScriptableObject>($"Worlds/{name}");
        ApplyLocation(location);

        yield return new WaitForSeconds(0.5f);

        if (offset == Vector2.zero || deterministicRandom == null) yield break;
        // Устанавливаем флаги готовности
        isOffsetReady = true;
        isSeedReady = true;

        StartCoroutine(GenerateEverythingOnClient());
    }
    #endregion

    #region Сетевая логика
    [ServerRpc(RequireOwnership = false)]
    public void GetCurrentSettingLocationServerRpc(int locationId)
    {
        networkCurrentLocationId.Value = locationId;
        string name = locationId.ToString();

        var location = Resources.Load<WorldScriptableObject>($"Worlds/{name}");
        ApplyLocation(location);
        StartGeneration();
    }


    void ApplyLocation(WorldScriptableObject location)
    {
        if (location == null) return;


        width = location.width;
        height = location.height;
        terrainSize = location.terrainSize;
        scale = location.scale;
        heightMultiplier = location.heightMultiplier;
        octaves = location.octaves;
        persistence = location.persistence;
        lacunarity = location.lacunarity;
        terrainMaterial = location.terrainMaterial;
        spawnObjects = location.spawnObjects;
        depth = location.depth;

        Debug.Log($"{(IsServer ? "Server" : "Client")} loaded location: {location.name}");
    }
    #endregion

    #region Генерация на сервере
    private void StartGeneration()
    {
        if (IsServer)
        {
            InitializeServer();
        }
    }

    private void InitializeServer()
    {
        if (randomizeOnStart)
        {
            RandomizeParameters();
        }

        GenerateTerrain();
        SpawnObjectsDeterministic();
    }

    public void RandomizeParameters()
    {
        // Генерируем offset для террейна
        offset = new Vector2(
            Random.Range(0, randomOffsetRange),
            Random.Range(0, randomOffsetRange)
        );
        networkOffset.Value = offset;

        // Генерируем seed для объектов
        int seed = Random.Range(int.MinValue, int.MaxValue);
        objectGenerationSeed.Value = seed;

        // Сервер инициализирует свой random
        deterministicRandom = new System.Random(seed);

        Debug.Log($"Server generated - Offset: {offset}, Seed: {seed}");
    }

    private void GenerateTerrain()
    {
        InitializeTerrain();
        GenerateHeightmap();
        AssignMaterial();
        SetupCollider();

        terrainCenter = terrain.transform.position + new Vector3(terrainSize / 2f, 0f, terrainSize / 2f);
        Debug.Log("Server: Terrain generated");
    }
    #endregion

    #region Обработка NetworkVariables на клиенте
    private void OnOffsetChanged(Vector2 oldValue, Vector2 newValue)
    {
        offset = newValue;
        isOffsetReady = true;
        Debug.Log($"Client: Offset received: {newValue}");

        TryGenerateOnClient();
    }

    private void OnSeedChanged(int oldValue, int newValue)
    {
        // Клиент инициализирует random с тем же seed
        deterministicRandom = new System.Random(newValue);
        isSeedReady = true;
        Debug.Log($"Client: Seed received: {newValue}");

        TryGenerateOnClient();
    }

    private void OnLocationIdChanged(int oldValue, int newValue)
    {
        currentLocationId = newValue;
        string name = currentLocationId.ToString();
        var location = Resources.Load<WorldScriptableObject>($"Worlds/{name}");
        ApplyLocation(location);

        TryGenerateOnClient();
    }


    private void OnGenerationReadyChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("Client: Server generation ready");
            TryGenerateOnClient();
        }
    }

    private void TryGenerateOnClient()
    {
        if (!IsClient) return;

        // Проверяем все условия
        if (isOffsetReady && isSeedReady)
        {
            StartCoroutine(GenerateEverythingOnClient());
        }
        else
        {
            Debug.Log($"Waiting for: Offset={isOffsetReady}, Seed={isSeedReady}");
        }
    }

    private IEnumerator GenerateEverythingOnClient()
    {

        Debug.Log("Client: Starting generation...");

        // 1. Генерируем террейн
        GenerateTerrain();

        // 2. Ждем один кадр чтобы террейн обновился
        yield return null;

        // 3. Генерируем объекты
        SpawnObjectsDeterministic();

        Debug.Log("Client: Generation complete!");
    }
    #endregion

    #region Общая генерация (одинаковая на сервере и клиенте)
    private void InitializeTerrain()
    {
        if (terrainData == null)
        {
            terrainData = new TerrainData();
        }

        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(terrainSize, heightMultiplier, terrainSize);
        terrain.terrainData = terrainData;
    }

    private void GenerateHeightmap()
    {
        heightmapData = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heightmapData[x, y] = CalculateHeight(x, y);
            }
        }

        terrainData.SetHeights(0, 0, heightmapData);
    }

    private float CalculateHeight(int x, int y)
    {
        float noiseValue = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            float sampleX = (x + offset.x) / scale * frequency;
            float sampleY = (y + offset.y) / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

            noiseValue += perlinValue * amplitude;

            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        noiseValue = noiseValue / maxAmplitude;
        noiseValue = (noiseValue + 1f) * 0.5f;

        return noiseValue;
    }

    private void AssignMaterial()
    {
        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }
    }

    private void SetupCollider()
    {
        if (!generateCollider) return;
        terrainCollider.terrainData = terrainData;
    }

    // ОБЩИЙ метод для сервера и клиента
    public void SpawnObjectsDeterministic()
    {
        if (deterministicRandom == null)
        {
            Debug.LogError("Deterministic random not initialized!");
            return;
        }

        ClearAllObjects();

        // Генерируем ВСЕ данные сначала
        GenerateAllObjectData();

        // Затем спавним объекты
        SpawnFromGeneratedData();

        Debug.Log($"{(IsServer ? "Server" : "Client")}: Spawned {currentTotalObjects} objects");
    }

    // Этот метод должен быть ИДЕНТИЧЕН на сервере и клиенте
    private void GenerateAllObjectData()
    {
        generatedObjectData.Clear();
        int prefabIndex = 0;

        // Создаем ОТДЕЛЬНЫЙ random для позиций
        System.Random positionRandom = new System.Random(objectGenerationSeed.Value);

        // И ОТДЕЛЬНЫЙ random для параметров объекта
        // Используем seed + смещение чтобы не пересекаться
        System.Random paramRandom = new System.Random(objectGenerationSeed.Value + 123456);

        foreach (var spawn in spawnObjects)
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


    /// <summary>
    /// Проверяет, находится ли позиция в центральной безопасной зоне
    /// </summary>
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
            if (data.prefabIndex >= 0 && data.prefabIndex < spawnObjects.Count)
            {
                //if (spawnObjects[data.prefabIndex].isNet == false)
                //{
                //    var spawn = spawnObjects[data.prefabIndex];
                //    SpawnObjectFromData(spawn, data);
                //}
                //else if (IsServer)
                //{
                //    var spawn = spawnObjects[data.prefabIndex];
                //    SpawnObjectOnServer(spawn, data);
                //}
            }
        }
    }

    private void SpawnObjectFromData(SpawnObject spawn, DeterministicObjectData data)
    {
        GameObject spawnedObject = Instantiate(spawn.prefab, data.position, Quaternion.identity);
        spawnedObject.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
        spawnedObject.transform.localScale = new Vector3(data.scale, data.scale, data.scale);
        spawnedObject.transform.SetParent(terrain.transform);
        spawnedObjects.Add(spawnedObject);

    }



    private void SpawnObjectOnServer(SpawnObject spawn, DeterministicObjectData data)
    {

        // 1. Создаем объект
        GameObject spawnedObject = Instantiate(
            spawn.prefab,
            data.position,
            Quaternion.Euler(0f, data.rotationY, 0f)
        );

        // 2. Применяем локальный масштаб ДО спавна
        spawnedObject.transform.localScale = new Vector3(data.scale, data.scale, data.scale);

        // 3. Получаем NetworkObject
        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            // 4. СПАВНИМ через NetworkObject
            networkObject.Spawn();

            // 5. Устанавливаем родителя ПОСЛЕ спавна
            if (terrain != null)
            {
                spawnedObject.transform.SetParent(terrain.transform, true);
            }

            spawnedObjects.Add(spawnedObject);
        }
        else
        {
            Debug.LogError("Prefab doesn't have NetworkObject component!");
            Destroy(spawnedObject);
        }

    }
    #endregion

    #region Вспомогательные методы
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

    [ContextMenu("Reseed Objects")]
    public void ReseedObjects()
    {
        if (IsServer)
        {
            int newSeed = Random.Range(int.MinValue, int.MaxValue);
            objectGenerationSeed.Value = newSeed;
            deterministicRandom = new System.Random(newSeed);

            ClearAllObjects();
            SpawnObjectsDeterministic();

            Debug.Log($"Server: Reseeded objects with seed {newSeed}");
        }
        else
        {
            Debug.LogWarning("Only server can reseed objects!");
        }
    }

    public bool AreObjectsSpawned()
    {
        return spawnedObjects.Count > 0;
    }

    public bool IsTerrainReady()
    {
        return terrainData != null;
    }
    #endregion
}