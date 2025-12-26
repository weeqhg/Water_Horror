using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarSystem : MonoBehaviour
{
    [SerializeField] private RadarUI radarUI;
    [SerializeField] private float radarRange;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private Transform player;

    [SerializeField] private Vector3 posSubmarine;

    [SerializeField] private float radarDuration = 10f;
    private float radarTimer = 0f;


    private bool isRadarActive = false;

    private List<GameObject> detectObjects = new();

    public void Inizialized(Vector3 posSubmarine)
    {
        this.posSubmarine = posSubmarine;

        radarUI.Initialized(player, radarDuration, posSubmarine);

        ToggleRadarUI(false);
    }


    public void ToggleRadarUI(bool enable)
    {
        if (enable)
        {
            radarUI.ShowRadarUI();
        }
        else
        {
            radarUI.HideRadarUI();
        }
    }


    public void ToggleRadar(bool enable)
    {
        if (enable)
        {
            Active();
        }
        else
        {
            DeActive();
        }
    }

    private void Active()
    {
        radarTimer = radarDuration - 1f;

        isRadarActive = true;

        radarUI.ToggleRadar(true);
    }
    private void DeActive()
    {
        radarTimer = radarDuration - 1f;

        isRadarActive = false;

        radarUI.ToggleRadar(false);
    }


    private void Update()
    {
        if (!isRadarActive) return;

        radarTimer += Time.deltaTime;

        if (radarTimer >= radarDuration)
        {
            DetectObjects();
            radarTimer = 0f;
        }
    }

    private void DetectObjects()
    {
        Debug.Log("Проверка вызовом");
        // ОЧИЩАЕМ список перед новым сканированием!
        detectObjects.Clear();

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radarRange, detectionLayer);

        foreach (var collider in hitColliders)
        {
            radarUI.CreateOneMark(collider.gameObject);
            //detectObjects.Add(collider.gameObject);
            SonarObjectDetected(collider.gameObject);
        }
        //sonarUI.CreateMark(detectObjects);

    }

    private void SonarObjectDetected(GameObject obj)
    {
        Debug.Log($"Sonar detected: {obj.name}");
    }
}
