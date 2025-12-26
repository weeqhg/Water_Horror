using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class OxygenUI : MonoBehaviour
{
    [Header("Main Oxygen")]
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private Image oxygenFill;

    [Header("Penalties")]
    [SerializeField] private RectTransform penaltiesContainer;
    [SerializeField] private GameObject penaltyPrefab;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color lowColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Дополнительный кислород")]
    [SerializeField] private RectTransform dopOxygen;

    [Header("Черепок для смерти")]
    [SerializeField] private GameObject panelDeath;
    [SerializeField] private Image iconHeadSkeleton;
    private float totalWidth;
    private OxygenSystem oxygenSystem;

    //private Tween dopOxygenTween;
    private class PenaltyUIElements
    {
        public GameObject gameObject;
        public LayoutElement layoutElement;
        public Image image;
    }

    private Dictionary<string, PenaltyUIElements> penaltyUIElements = new Dictionary<string, PenaltyUIElements>();

    private float lastPenaltyUpdateTime = 0f;
    private const float PENALTY_UPDATE_INTERVAL = 0.05f; // 20 раз в секунду

    private void Start()
    {
        panelDeath.SetActive(false);
        totalWidth = penaltiesContainer.GetComponent<RectTransform>().rect.width;
    }

    public void Initialize(OxygenSystem system)
    {
        oxygenSystem = system;
        oxygenSlider.maxValue = system.GetMaxOxygen();
        oxygenSlider.value = system.GetCurrentOxygen();
        // Инициализируем UI дополнительного кислорода
        UpdateDopOxygenUI();
        UpdatePenaltyUI();
    }

    public void UpdateOxygenSlider()
    {
        if (oxygenSystem == null) return;
        oxygenSlider.value = oxygenSystem.GetCurrentOxygen();
        UpdatePenaltyUI();
    }
    public void UpdateOxygenUiPenalty()
    {
        if (oxygenSystem == null) return;
        UpdatePenaltyUI();
    }

    private void Update()
    {
        if (oxygenSystem == null) return;
        UpdateColors();
        // Обновляем размеры штрафов с интервалом
        if (Time.time - lastPenaltyUpdateTime >= PENALTY_UPDATE_INTERVAL)
        {
            UpdatePenaltySizes();
            lastPenaltyUpdateTime = Time.time;
        }
    }

    public void UpdateDopOxygenUI()
    {
        if (oxygenSystem == null) return;

        // Получаем текущее значение доп. кислорода
        float dopOxygenValue = oxygenSystem.GetDopOxygen();

        // Масштабируем: 100 единиц = 768 пикселей
        float maxDopOxygen = oxygenSystem.GetMaxOxygen(); ; // Максимальное значение в системе
        float maxWidth = 768f;     // Максимальная ширина в пикселях


        float targetWidth = (dopOxygenValue / maxDopOxygen) * maxWidth;
        // Вычисляем процент заполнения
        float targetWidthDop = Mathf.Clamp(targetWidth, 0f, maxWidth);


        // Останавливаем предыдущую анимацию
        //if (dopOxygenTween != null && dopOxygenTween.IsActive())
        //{
        //    dopOxygenTween.Kill();
        //}
        // Плавная анимация ширины
        dopOxygen.DOSizeDelta(
            new Vector2(targetWidthDop, dopOxygen.sizeDelta.y),
            0.5f) // Длительность 0.5 секунды
            .SetEase(Ease.OutCubic); // Плавное замедление в конце
    }

    public void UpdatePenaltyUI()
    {
        var penalties = oxygenSystem.GetActivePenalties();

        // Очищаем уничтоженные объекты из словаря
        List<string> toRemoveDestroyed = new List<string>();
        foreach (var kvp in penaltyUIElements)
        {
            if (kvp.Value == null)
            {
                toRemoveDestroyed.Add(kvp.Key);
            }
        }
        foreach (string id in toRemoveDestroyed)
        {
            penaltyUIElements.Remove(id);
        }

        // Удаляем старые штрафы
        List<string> currentPenalties = new List<string>();
        foreach (var penalty in penalties)
            currentPenalties.Add(penalty.id);

        List<string> toRemove = new List<string>();
        foreach (var penaltyId in penaltyUIElements.Keys)
        {
            if (!currentPenalties.Contains(penaltyId))
            {
                if (penaltyUIElements[penaltyId] != null)
                {
                    Destroy(penaltyUIElements[penaltyId].gameObject);
                }
                toRemove.Add(penaltyId);
            }
        }
        foreach (string id in toRemove)
        {
            penaltyUIElements.Remove(id);
        }

        // Добавляем/обновляем штрафы
        foreach (var penalty in penalties)
        {
            if (!penaltyUIElements.ContainsKey(penalty.id))
            {
                CreatePenaltyUIElement(penalty);
            }
        }
    }

    private void UpdatePenaltySizes()
    {
        var penalties = oxygenSystem.GetActivePenalties();

        foreach (var penalty in penalties)
        {
            if (penaltyUIElements.TryGetValue(penalty.id, out var uiElements) &&
                uiElements.layoutElement != null)
            {
                float newPenaltyWidth = (totalWidth * penalty.penaltyAmount) / 100f;
                uiElements.layoutElement.preferredWidth = newPenaltyWidth;
            }
        }
    }


    private void CreatePenaltyUIElement(OxygenSystem.OxygenPenalty penalty)
    {
        GameObject penaltyObj = Instantiate(penaltyPrefab, penaltiesContainer);

        var uiElements = new PenaltyUIElements
        {
            gameObject = penaltyObj,
            layoutElement = penaltyObj.GetComponent<LayoutElement>(),
            image = penaltyObj.GetComponentInChildren<Image>()
        };

        if (uiElements.image != null)
        {
            uiElements.image.color = penalty.penaltyColor;
        }

        if (uiElements.layoutElement != null)
        {
            float penaltyWidth = (totalWidth * penalty.penaltyAmount) / 100f;
            uiElements.layoutElement.preferredWidth = penaltyWidth;
        }

        penaltyUIElements[penalty.id] = uiElements;
    }

    private void UpdateColors()
    {
        if (oxygenSystem.IsCritical)
            oxygenFill.color = criticalColor;
        else if (oxygenSystem.IsLow)
            oxygenFill.color = lowColor;
        else
            oxygenFill.color = normalColor;
    }


    public void ToggleDeathPanel(bool enable)
    {
        panelDeath.SetActive(enable);
        iconHeadSkeleton.fillAmount = 0f;
    }
    public void UpdateDeathUI(float timer, float duration)
    {
        // Безопасная проверка параметров
        if (duration <= 0)
        {
            iconHeadSkeleton.fillAmount = 0f;
            return;
        }
        if (timer < 0)
        {
            timer = 0;
        }
        // Нормализация таймера
        float fillAmount = Mathf.Clamp01(timer / duration);
        iconHeadSkeleton.fillAmount = fillAmount;
    }
}