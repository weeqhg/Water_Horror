//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class PenaltyUIElement : MonoBehaviour
//{
//    [SerializeField] private Image penaltyIcon;
//    [SerializeField] private TextMeshProUGUI timeText;
//    [SerializeField] private Slider timeSlider;

//    private string penaltyId;
//    private float currentTime;
//    private float initialDuration;
//    private bool isActive = false;
//    private bool isInfinite = false;

//    public void Initialize(string id, float duration)
//    {
//        penaltyId = id;
//        initialDuration = duration;
//        currentTime = duration;
//        isActive = true;
//        isInfinite = false;

//        if (timeSlider != null)
//        {
//            timeSlider.maxValue = duration;
//            timeSlider.value = duration;
//        }

//        UpdateDisplay();
//    }

//    public void SetInfinite()
//    {
//        isInfinite = true;
//        isActive = false; // Отключаем таймер для бесконечных штрафов

//        if (timeText != null)
//        {
//            timeText.text = "∞";
//        }

//        if (timeSlider != null)
//        {
//            timeSlider.value = timeSlider.maxValue;
//        }
//    }

//    // ДОБАВИТЬ ЭТОТ МЕТОД
//    public void UpdateTime(float newTime)
//    {
//        if (isInfinite) return;

//        isActive = true;
//        currentTime = newTime;
//        UpdateDisplay();

//        // Если время истекло, отключаем
//        if (currentTime <= 0)
//        {
//            isActive = false;
//        }
//    }
//    private void Update()
//    {
//        if (!isActive || isInfinite) return;

//        // Уменьшаем время каждый кадр
//        currentTime -= Time.deltaTime;
//        UpdateDisplay();

//        // Проверяем окончание времени
//        if (currentTime <= 0)
//        {
//            isActive = false;
//        }
//    }

//    private void UpdateDisplay()
//    {
//        if (timeText != null)
//        {
//            timeText.text = Mathf.CeilToInt(currentTime).ToString();
//        }

//        if (timeSlider != null)
//        {
//            timeSlider.value = currentTime;
//        }
//    }
//}