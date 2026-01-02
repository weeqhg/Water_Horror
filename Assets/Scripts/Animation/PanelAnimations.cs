using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Везде где будет требоваться сделать переходы, можно использовать появление и исчезновение панели
/// </summary>
public class PanelAnimations : MonoBehaviour
{
    [Header("Настройки исчезновения")]
    [SerializeField] private float durationDisappearance; //Длительность исчезновения
    [SerializeField] private float delayVanishDisappearance;//задержка перед исчезновением

    [Header("Настройки появления")]
    [SerializeField] private float durationAppearance; //Длительность появления
    [SerializeField] private float delayVanishAppearance; //задержка после появлением
    private Image panel;

    private void Start()
    {
        panel = GetComponent<Image>();
        gameObject.SetActive(false);
    }


    #region StartDisappearanceAnimation()
    public void StartDisappearanceAnimation()
    {
        gameObject.SetActive(true);
        panel.DOFade(1f, 0f);
        StartCoroutine(WaitDisappearanceAnimation());
    }
    private IEnumerator WaitDisappearanceAnimation()
    {
        yield return new WaitForSeconds(delayVanishDisappearance);

        panel.DOFade(0f, durationDisappearance)
            .OnComplete(() => { gameObject.SetActive(false); });
    }
    #endregion

    #region StartAppearanceAnimation()
    public void StartAppearanceAnimation()
    {
        gameObject.SetActive(true);
        panel.DOFade(0f, 0f);
        StartCoroutine(WaitAppearanceAnimation());
    }

    private IEnumerator WaitAppearanceAnimation()
    {

        panel.DOFade(1f, durationAppearance)
           .OnComplete(() => {});
        yield return new WaitForSeconds(delayVanishAppearance);

        gameObject.SetActive(false);
    }
    #endregion
}
