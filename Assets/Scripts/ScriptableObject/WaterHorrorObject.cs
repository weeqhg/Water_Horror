using System;
using UnityEngine;

namespace WaterHorror.Data
{
    [System.Serializable]
    public class SpawnObject
    {
        public string objectName;
        public GameObject prefab;
        public int count = 5;
        public float minHeight = 0f;
        public float maxHeight = 1f;

        [Range(0f, 20f)] public float radius = 0f;
        [Range(0f, 1f)] public float randomScale = 0f;
        [Range(0f, 1f)] public float spawnDensity = 0.5f;
    }

    [System.Serializable]
    public class SpawnEnemy
    {
        public string objectName;
        public GameObject prefab;
        public int count = 5;
    }
}