using UnityEngine;
using System.Collections;

public class SimpleCRTFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [SerializeField] private float minBrightness = 0.95f;
    [SerializeField] private float maxBrightness = 1.05f;
    [SerializeField] private float flickerSpeed = 10f;
    [SerializeField] private float strongFlickerChance = 0.1f;
    [SerializeField] private float strongFlickerAmount = 0.2f;
    
    [Header("Scan Lines")]
    [SerializeField] private bool useScanLines = true;
    [SerializeField] private GameObject scanLinePrefab;
    [SerializeField] private float scanLineSpeed = 30f;
    [SerializeField] private int maxScanLines = 3;
    
    private CanvasGroup canvasGroup;
    private float baseAlpha;
    private Coroutine flickerCoroutine;
    
    public void StartCRTFlicker()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        baseAlpha = canvasGroup.alpha;
        
        StartFlickering();
        
        if (useScanLines)
        {
            CreateScanLines();
        }
    }
    
    void StartFlickering()
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);
        
        flickerCoroutine = StartCoroutine(FlickerRoutine());
    }
    
    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Основное мерцание (60Hz)
            float time = Time.time * flickerSpeed;
            float flicker = Mathf.PerlinNoise(time, 0) * 2f - 1f;
            flicker *= 0.02f; // Небольшое мерцание
            
            // Случайные сильные мерцания
            if (Random.value < strongFlickerChance)
            {
                flicker += Random.Range(-strongFlickerAmount, strongFlickerAmount);
            }
            
            // Применяем мерцание
            float targetAlpha = baseAlpha + flicker;
            targetAlpha = Mathf.Clamp(targetAlpha, minBrightness, maxBrightness);
            
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 20f);
            
            yield return null;
        }
    }
    
    void CreateScanLines()
    {
        if (scanLinePrefab == null) return;
        
        for (int i = 0; i < maxScanLines; i++)
        {
            GameObject scanLine = Instantiate(scanLinePrefab, transform);
            RectTransform rt = scanLine.GetComponent<RectTransform>();
            
            // Настраиваем размер и позицию
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(0, 2);
            rt.anchoredPosition = new Vector2(0, -i * 20f);
            
            // Запускаем движение
            StartCoroutine(ScanLineMovement(rt, i * 0.3f));
        }
    }
    
    IEnumerator ScanLineMovement(RectTransform scanLine, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        float height = GetComponent<RectTransform>().rect.height;
        
        while (true)
        {
            // Движение сверху вниз
            float startY = 0;
            float endY = -height;
            
            float duration = height / scanLineSpeed;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                scanLine.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, progress));
                yield return null;
            }
            
            // Возврат наверх
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }
    
    void OnDestroy()
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);
    }
}