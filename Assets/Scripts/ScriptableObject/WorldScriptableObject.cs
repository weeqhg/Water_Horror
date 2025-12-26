using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using WaterHorror.Data;


[CreateAssetMenu(fileName = "New Location", menuName = "Location System/Location")]

public class WorldScriptableObject : ScriptableObject
{
    public int locationId;

    [Header("Настройки UI")]
    public Sprite Icon;
    public LocalizedString nameLocation;
    public string depth;
    public int difficulty;



    [Header("Настройки размера карты")]
    public int width = 512;
    public int height = 512;
    public float terrainSize = 500f;

    [Header("Настройка шума для генерации карты")]
    public float scale = 50f;
    public float heightMultiplier = 80f;

    [Header("Advanced Noise")]
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Terrain Material")]
    public Material terrainMaterial;

    [Header("Объекты на локации")]
    public List<SpawnObject> spawnObjects = new();

    [Header("Враги на локации")]
    public List<InteractiveObject> spawnEnemies = new();

    [Header("Предметы на локации для продажи")]
    public List<InteractiveObject> spawnItemsForSell = new();
    
    [Header("Предметы на локации для использования")]
    public List<InteractiveObject> spawnItemsForUse = new();
}
