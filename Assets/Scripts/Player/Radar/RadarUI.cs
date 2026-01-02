using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class RadarUI : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject radarUI;
    [SerializeField] private RectTransform radarRect;
    [SerializeField] private RectTransform waveRect;
    [SerializeField] private RectTransform submarineMarker; // Маркер игрока
    [SerializeField] private GameObject prefabMark;

    [SerializeField] private Sprite iconPlayer;
    [SerializeField] private Sprite iconItem;
    [SerializeField] private Sprite iconEnemy;
    [SerializeField] private Sprite iconOxygen;
    [SerializeField] private Sprite iconUncknow;
    [SerializeField] private Sprite iconHideItem;


    [Header("World Settings")]
    [SerializeField] private float worldRadius = 100f; // Радиус мира в юнитах

    [Header("Radar Settings")]
    [SerializeField] private float radarRadius = 200f; // Радиус радара в пикселях
    [SerializeField] private float markerLifetime = 5f; // Время жизни маркера

    [Header("Pulse Settings")]
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private Transform playerTransform;
    private Vector3 posSubmarine;
    private float pulseDuration = 2f; // Длительность одного пульса
    private bool isSonarActive = false;
    private Tween submarineTween;
    private Image submarineImage;
    public void ToggleRadar(bool enable)
    {
        isSonarActive = enable;

        if (isSonarActive)
        {
            StartPulse();
            StartSubmarinePulse();
        }
        else
        {
            StopPulse();
            submarineTween.Kill();
        }
    }

    public void ShowRadarUI()
    {
        radarUI.SetActive(true);
    }
    public void HideRadarUI()
    {
        radarUI.SetActive(false);
    }

    public void Initialized(Transform transform, float duration, Vector3 posSubmarine)
    {
        playerTransform = transform;
        pulseDuration = duration;
        this.posSubmarine = posSubmarine;
        submarineImage = submarineMarker.gameObject.GetComponent<Image>();
    }


    private void Update()
    {
        if (!isSonarActive) return;

        UpdatePositionSubmarineMarker();

        float playerAngle = playerTransform.eulerAngles.y;
        radarRect.localEulerAngles = new Vector3(0, 0, playerAngle);
    }

    private void UpdatePositionSubmarineMarker()
    {
        Vector2 radarPos = WorldToRadarPosition(posSubmarine);
        submarineMarker.anchoredPosition = radarPos;

        // Компенсируем вращение радара - вращаем в обратную сторону
        submarineMarker.localEulerAngles = new Vector3(0, 0, -radarRect.localEulerAngles.z);
    }

    private void StartSubmarinePulse()
    {
        if (submarineImage == null) return;

        // Создаем пульсирующую анимацию для маркера подлодки
        submarineTween = DOTween.Sequence()
            .Append(submarineImage.DOFade(1f, 0f).SetEase(Ease.InOutSine))
            .Append(submarineImage.DOFade(0f, 1f).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Yoyo)
            .Play();
    }

    public void CreateOneMark(GameObject detectObj)
    {
        Sprite icon = iconHideItem;
        Color color = Color.white;

        switch (detectObj.tag)
        {
            case "Item":
                icon = iconItem;
                color = Color.green;
                break;
            case "Enemy":
                icon = iconEnemy;
                color = Color.red;
                break;
            case "Player":
                icon = iconPlayer;
                color = Color.yellow;
                break;
            case "Oxygen":
                icon = iconOxygen;
                color = Color.cyan;
                break;
            case "Uncknow":
                icon = iconUncknow;
                color = Color.magenta;
                break;
        }
        // Задержка перед следующим маркером (можно сделать зависимой от расстояния)
        float distance = Vector3.Distance(detectObj.transform.position, playerTransform.position);
        float delay = Mathf.Lerp(0.1f, 0.5f, distance / worldRadius);
        // Создаем маркер
        StartCoroutine(CreateOneMarker(detectObj, icon, color, delay));
    }


    private IEnumerator CreateOneMarker(GameObject worldObject, Sprite icon, Color color, float delay)
    {
        if (prefabMark == null || radarRect == null) yield break;

        yield return new WaitForSeconds(delay);


        GameObject markerObj = Instantiate(prefabMark, radarRect);
        RectTransform markerRT = markerObj.GetComponent<RectTransform>();
        Image markerImage = markerObj.GetComponent<Image>();

        // Начально делаем маркер невидимым
        if (markerImage != null)
        {
            markerImage.sprite = icon;
            markerImage.color = new Color(color.r, color.g, color.b, 0); // Прозрачный
        }

        Vector2 radarPos = WorldToRadarPosition(worldObject.transform.position);
        markerRT.anchoredPosition = radarPos;

        // Создаем последовательность анимаций
        Sequence markerSequence = DOTween.Sequence();

        // 1. Анимация появления
        markerSequence.Append(markerImage.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        markerSequence.Join(markerRT.DOScale(Vector3.one, 0.3f).From(Vector3.zero).SetEase(Ease.OutBack));

        // 2. Задержка перед исчезновением
        markerSequence.AppendInterval(markerLifetime - 0.8f);

        // 3. Анимация исчезновения
        markerSequence.Append(markerImage.DOFade(0f, 0.5f).SetEase(Ease.OutQuad));
        markerSequence.Join(markerRT.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

        // 4. Уничтожение объекта после анимации
        markerSequence.OnComplete(() => {
            if (markerObj != null)
            {
                Destroy(markerObj);
            }
        });

        markerSequence.Play();

    } 

    private Vector2 WorldToRadarPosition(Vector3 worldPosition)
    {
        if (playerTransform == null || radarRect == null)
            return Vector2.zero;

        // Вычисляем относительную позицию от игрока
        Vector3 relativePos = worldPosition - playerTransform.position;

        // Ограничиваем дальность
        float distance = relativePos.magnitude;
        if (distance > worldRadius)
        {
            relativePos = relativePos.normalized * worldRadius;
        }

        // Нормализуем позицию (от -1 до 1)
        float normalizedX = relativePos.x / worldRadius;
        float normalizedZ = relativePos.z / worldRadius;

        // Преобразуем в координаты радара (от -radarRadius до radarRadius)
        float radarX = normalizedX * radarRadius;
        float radarY = normalizedZ * radarRadius;

        return new Vector2(radarX, radarY);
    }

    private Tween pulseTween;

    public void StartPulse()
    {
        StopPulse(); // Сначала останавливаем существующую анимацию

        // Создаем новую анимацию
        pulseTween = waveRect.DOScale(Vector3.one, pulseDuration)
            .From(Vector3.zero)
            .SetEase(easeType)
            .OnStart(() => {
                audioSource.Play();
                audioSource.loop = true;
            })
            .OnComplete(() => {
                // Проверяем, что анимация все еще активна
                if (waveRect != null)
                {
                    waveRect.localScale = new Vector3(0f, 0f, 1f);
                }
            })
            .OnKill(() => {
                // Принудительный сброс при остановке
                if (waveRect != null)
                {
                    waveRect.localScale = new Vector3(0f, 0f, 1f);
                }
            })
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopPulse()
    {
        // Безопасное завершение анимации
        if (pulseTween != null)
        {
            if (pulseTween.IsActive())
            {
                pulseTween.Kill();
            }
            pulseTween = null;
        }

        // Сбрасываем scale
        if (waveRect != null)
        {
            waveRect.localScale = new Vector3(0f, 0f, 1f);
        }

        audioSource.Stop();
        audioSource.loop = false;
    }

    private void OnDestroy()
    {
        // Очищаем при уничтожении объекта
        StopPulse();
        submarineTween.Kill();
    }
}
