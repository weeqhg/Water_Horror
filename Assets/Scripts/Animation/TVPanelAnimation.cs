using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TVPanelAnimation : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private float startScale = 0.01f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private Color startColor = Color.black;
    [SerializeField] private Color endColor = Color.white;

    private readonly Sequence animationSequence;


    public void ShowPanel()
    {
        panelRect.localScale = Vector3.one * startScale;

        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }

        Sequence hideSequence = DOTween.Sequence();

        // Обратная анимация
        hideSequence.Append(
            panelRect.DOScale(endScale, animationDuration * 0.7f)
                .SetEase(Ease.OutBack)
        );

        if (canvasGroup != null)
        {
            hideSequence.Join(
                canvasGroup.DOFade(1f, animationDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
            );
        }

        if (panelImage != null)
        {
            hideSequence.Join(
                panelImage.DOColor(endColor, animationDuration * 0.5f)
            );
        }

        hideSequence.OnComplete(() => {
        });
    }

    public void HidePanel()
    {
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }

        Sequence hideSequence = DOTween.Sequence();

        // Обратная анимация
        hideSequence.Append(
            panelRect.DOScale(startScale, animationDuration * 0.7f)
                .SetEase(Ease.InBack)
        );

        if (canvasGroup != null)
        {
            hideSequence.Join(
                canvasGroup.DOFade(0f, animationDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
            );
        }

        if (panelImage != null)
        {
            hideSequence.Join(
                panelImage.DOColor(startColor, animationDuration * 0.5f)
            );
        }
        hideSequence.OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

    void OnDestroy()
    {
        animationSequence?.Kill();
    }
}