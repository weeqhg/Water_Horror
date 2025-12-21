using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Netcode;

public class Gateway : MonoBehaviour
{
    [Header("Animation Names")]
    [SerializeField] private string upWater = "Up";
    [SerializeField] private string downWater = "Down";

    [Header("References")]
    [SerializeField] private Animator waterAnimator;
    [SerializeField] private Transform submarineCollision;

    [Header("Door Settings")]
    [SerializeField] private Transform innerDoor;
    [SerializeField] private Transform outerDoor;
    [SerializeField] private AudioRandomizer innerDoorAudio;
    [SerializeField] private AudioRandomizer outerDoorAudio;
    
    [SerializeField] private float doorCloseDelay = 0.5f;

    [Header("Sequence Settings")]
    [SerializeField] private float waterAnimationDelay = 0.3f;

    private bool isWater = false;

    private Vector3 originalScale;
    private bool isAnimating = false;

    void Start()
    {
        if (submarineCollision != null)
        {
            originalScale = submarineCollision.localScale;
        }
    }

    
    public void ToggleWater()
    {
        if (isAnimating) return;

        StartCoroutine(ToggleWaterSequence());
    }

    private IEnumerator ToggleWaterSequence()
    {
        if (isAnimating) yield break;

        isAnimating = true;

        bool targetWaterState = !isWater;

        Debug.Log($"Server: Starting water toggle to {(targetWaterState ? "UP" : "DOWN")}");

        // 1. Закрываем нужную дверь
        if (targetWaterState)
        {
            // Поднимаем воду - закрываем внутреннюю
            innerDoor.DORotate(new Vector3(0, 90, 0), doorCloseDelay);

            innerDoorAudio.ToggleAudio();
        }
        else
        {
            // Опускаем воду - закрываем внешнюю
            outerDoor.DORotate(new Vector3(15, 0, 0), doorCloseDelay);

            outerDoorAudio.ToggleAudio();
        }

        // 2. Ждем закрытия двери
        yield return new WaitForSeconds(doorCloseDelay);

        // 3. Анимация воды на всех клиентах
        waterAnimator.Play(targetWaterState ? upWater : downWater);


        // Ждем завершения scale анимации
        yield return new WaitForSeconds(waterAnimationDelay);

        Vector3 targetScale = targetWaterState ?
            new Vector3(originalScale.x * 0.81f, originalScale.y, originalScale.z) :
            originalScale;

        submarineCollision.DOScale(targetScale, 0f);

        // 6. Обновляем NetworkVariable (автоматически синхронизируется с клиентами)
        isWater = targetWaterState;

        // 7. Открываем другую дверь
        if (targetWaterState)
        {
            outerDoorAudio.ToggleAudio();
            outerDoor.DORotate(new Vector3(15, -90, 0), doorCloseDelay);
        }
        else
        {
            innerDoorAudio.ToggleAudio();
            innerDoor.DORotate(new Vector3(0, 0, 0), doorCloseDelay);
        }

        yield return new WaitForSeconds(doorCloseDelay);

        isAnimating = false;
       
    }
}