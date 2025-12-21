using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CreateWorld : NetworkBehaviour
{

    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;

    [SerializeField] private MakeTerrainWorld makeTerrainWorld;
    [SerializeField] private SpawnObjectWorld spawnObjectWorld;
    [SerializeField] private SubmarineMain submarineMain;
    [SerializeField] private SpawnEnemyWorld spawnEnemyWorld;

    [SerializeField] private NetworkVariable<int> netWorldId = new(0);
    [SerializeField] private NetworkVariable<int> netWorldSeed = new(0);


    public string GetDepth() => makeTerrainWorld.GetDepth();


    public override void OnNetworkSpawn()
    {
        makeTerrainWorld.Initialized(terrain, terrainCollider);
        spawnObjectWorld.Initialized(terrain);

        if (IsClient && !IsHost)
        {
            netWorldId.OnValueChanged += OnWorldIdChanged;
            netWorldSeed.OnValueChanged += OnWorldSeedChanged;

            StartCoroutine(GenerateForLateClient());
        }

    }

    #region Серверная логика

    public void CreateWorldOnServer(int worldId)
    {
        netWorldId.Value = worldId;

        SearchWorldId(worldId);

        RandomizeParameters();

        StartCreateWorld(netWorldSeed.Value);

        submarineMain.Fall(makeTerrainWorld.GetCentre());

        spawnEnemyWorld.StartSpawnEnemy();
    }

    private void RandomizeParameters()
    {
        int seed = Random.Range(int.MinValue, int.MaxValue);
        netWorldSeed.Value = seed;

        Debug.Log($"Server generated Seed: {seed}");
    }

    private IEnumerator GenerateForLateClient()
    {
        // Даем время NetworkVariable синхронизироваться
        yield return new WaitForSeconds(0.5f);

        if (netWorldId.Value < 0 || netWorldSeed.Value < 0) yield break;

        Debug.Log("Late client: Starting generation with existing data...");

        SearchWorldId(netWorldId.Value);

        StartCreateWorld(netWorldSeed.Value);
    }
    #endregion




    #region Общая логика

    private void SearchWorldId(int worldId)
    {
        string name = worldId.ToString();

        var world = Resources.Load<WorldScriptableObject>($"Worlds/{name}");

        makeTerrainWorld.ApplyWorldSetting(world);

        spawnObjectWorld.ApplyWorldSetting(world);

        spawnEnemyWorld.ApplyWorldSetting(world);

        Debug.Log($"{(IsServer ? "Server" : "Client")} loaded location: {world.name}");
    }

    private void StartCreateWorld(int seed)
    {
        makeTerrainWorld.GenerateTerrain(seed);
        spawnObjectWorld.SpawnObject(seed);
    }
    #endregion









    #region Для клиентов
    private void OnWorldIdChanged(int oldValue, int newValue)
    {
        SearchWorldId(newValue);
    }

    private void OnWorldSeedChanged(int oldValue, int newValue)
    {
        StartCreateWorld(newValue);
    }
    #endregion
}
