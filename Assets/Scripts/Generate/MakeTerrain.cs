using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeTerrainWorld : MonoBehaviour
{
    private Terrain terrain;
    private Vector3 terrainCenter;
    private TerrainCollider terrainCollider;
    private TerrainData terrainData;
    private float[,] heightmapData;

    private Vector2 offset;
    [Range(0, 1000f)] public float randomOffsetRange;

    private int width = 512;
    private int height = 512;
    private float terrainSize = 500f;
    private string depth;
    private float scale = 50f;
    private float heightMultiplier = 80f;
    private int octaves = 4;
    private float persistence = 0.5f;
    private float lacunarity = 2f;
    private Material terrainMaterial;

    private System.Random deterministicRandom;


    public Vector3 GetCentre() => terrainCenter;
    public string GetDepth() => depth;

    public void Initialized(Terrain terrain, TerrainCollider terrainCollider)
    {
        this.terrain = terrain;
        this.terrainCollider = terrainCollider;
    }

    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        if (world == null) return;

        width = world.width;
        height = world.height;
        terrainSize = world.terrainSize;
        depth = world.depth;
        scale = world.scale;
        heightMultiplier = world.heightMultiplier;
        octaves = world.octaves;
        persistence = world.persistence;
        lacunarity = world.lacunarity;
        terrainMaterial = world.terrainMaterial;
    }

    public void GenerateTerrain(int seed)
    {
        deterministicRandom = new System.Random(seed);

        InitializeTerrain();

        GenerateHeightmap();

        FinishMake();

        terrainCenter = terrain.transform.position + new Vector3(terrainSize / 2f, 0f, terrainSize / 2f);

        Debug.Log($"X: {terrainCenter.x}, Z: {terrainCenter.z}");
    }


    private void InitializeTerrain()
    {
        terrainData = new TerrainData();

        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(terrainSize, heightMultiplier, terrainSize);
        terrain.terrainData = terrainData;

        offset = GenerateOffsetFromSeed();
    }

    private Vector2 GenerateOffsetFromSeed()
    {
        return new Vector2(
            (float)deterministicRandom.NextDouble() * randomOffsetRange,
            (float)deterministicRandom.NextDouble() * randomOffsetRange
        );
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


    private void FinishMake()
    {
        if (terrainMaterial != null) terrain.materialTemplate = terrainMaterial;

        if (terrainCollider != null) terrainCollider.terrainData = terrainData;   
        Debug.Log("Server: Terrain generated");
    }

}
