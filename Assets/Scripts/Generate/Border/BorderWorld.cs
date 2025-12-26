using DG.Tweening;
using UnityEngine;

public class BorderWorld : MonoBehaviour
{
    [SerializeField] private GameObject borderObject;
    [SerializeField] private float minSize = 0f; // Минимальный размер
    [SerializeField] private float duration = 60f;
    [SerializeField] private Ease shrinkEase = Ease.InOutCubic;
    private float initialHeight;
    private float initialWidth;

    private Tween shrinkTween;

    public bool isTest;
    private void Start()
    {
        if (isTest)
        {
            initialHeight = 512;
            initialWidth = 512;

            borderObject.transform.localScale = new Vector3(initialWidth, initialHeight, initialWidth);
            StartReduce();
        }
    }
    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        initialHeight = world.height;
        initialWidth = world.width;

        borderObject.transform.localScale = new Vector3(initialWidth, initialHeight, initialWidth);
    }

    public void StartReduce()
    {
        // Отменяем предыдущую анимацию
        if (shrinkTween != null && shrinkTween.IsActive())
        {
            shrinkTween.Kill();
        }

        // Создаем анимацию уменьшения
        float targetSize = minSize;

        shrinkTween = DOTween.To(
            () => borderObject.transform.localScale.x, // Получаем текущий размер
            x => {
                // Устанавливаем новый размер (равномерно по X и Z)
                borderObject.transform.localScale = new Vector3(x, initialHeight, x);   
            },
            targetSize,
            duration
        )
        .SetEase(shrinkEase)
        .OnUpdate(() => {
            // Дополнительные эффекты во время анимации
            UpdateShrinkingEffects();
        })
        .OnComplete(() => {
            borderObject.transform.localScale = new Vector3(minSize, minSize, minSize);
            Debug.Log("World shrinking completed!");
        });
    }
    private void UpdateShrinkingEffects()
    {
        // Визуальные эффекты при уменьшении
    }
}
