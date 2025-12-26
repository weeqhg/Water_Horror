using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class CoinCounterUI : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> texts;

    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease easeType = Ease.OutQuad;
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = " $";

    private float currentValue = 0f;
    private Tween countingTween;
    public void UpdateUI(float targetValue)
    {
        // Отменяем предыдущую анимацию
        if (countingTween != null && countingTween.IsActive())
        {
            countingTween.Kill();
        }

        float startValue = currentValue;
        currentValue = targetValue;

        // Создаем анимацию счетчика
        countingTween = DOVirtual.Float(startValue, targetValue, duration, value =>
        {
            UpdateAllTexts(value);
        })
        .SetEase(easeType)
        .OnComplete(() =>
        {
            // Гарантируем точное конечное значение
            UpdateAllTexts(targetValue);
        })
        .OnKill(() =>
        {
            UpdateAllTexts(targetValue);
        });
    }

    private void UpdateAllTexts(float value)
    {
        string formatted = FormatCasinoNumber(value);

        foreach (var text in texts)
        {
            if (text != null)
            {
                text.text = prefix + formatted + suffix;
            }
        }
    }
    private string FormatCasinoNumber(float value)
    {
        // Форматирование как в казино (с запятыми)
        return string.Format("{0:N0}", Mathf.Round(value));

        // Альтернатива: с разделением пробелами
        // return string.Format("{0:#,0}", Mathf.Round(value)).Replace(',', ' ');
    }






    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private int shakeVibrato = 10;

    [SerializeField] private Color originalColor;

    public void NotMoney()
    {
        foreach (var text in texts)
        {
            if (text != null)
            {
                string originalText = text.text;

                // Меняем текст на красный с значением
                text.color = errorColor;

                // Анимация тряски
                Sequence sequence = DOTween.Sequence();

                // 1. Тряска
                sequence.Append(text.transform.DOShakePosition(
                    shakeDuration,
                    new Vector3(shakeStrength, 0, 0),
                    shakeVibrato,
                    90f,
                    false
                ));

                // 2. Возвращаем цвет
                sequence.Append(text.DOColor(originalColor, 0.3f));

                // 3. Возвращаем оригинальный текст
                sequence.OnComplete(() =>
                {
                    text.transform.localPosition = Vector3.zero;
                });
            }
        }
    }
}
