using UnityEngine;
using DG.Tweening;


/// <summary>
/// Используется в лобби чтобы оживить сцену ссзади
/// </summary>
public class CameraRotation : MonoBehaviour
{
    [Header("Rotation Settings")] public float rotationAngle = 10f; // Угол вращения
    public float rotationDuration = 2f; // Длительность одного цикла
    public Ease easeType = Ease.InOutSine;


    private Sequence rotationSequence;

    void Start()
    {
        StartRotation();
    }

    void StartRotation()
    {
        // Создаем последовательность
        rotationSequence = DOTween.Sequence();

        // Вращение от 0 до +10 градусов
        rotationSequence.Append(transform.DORotate(new Vector3(rotationAngle, 0, 0), rotationDuration)
            .SetEase(easeType));

        // Вращение от +10 до -10 градусов
        rotationSequence.Append(transform.DORotate(new Vector3(-rotationAngle, 0, 0), rotationDuration * 2)
            .SetEase(easeType));

        // Вращение от -10 обратно к 0
        rotationSequence.Append(transform.DORotate(new Vector3(0, 0, 0), rotationDuration)
            .SetEase(easeType));

        // Зацикливаем анимацию
        rotationSequence.SetLoops(-1, LoopType.Restart);
    }

    void OnDestroy()
    {
        // Очищаем твины при уничтожении объекта
        rotationSequence?.Kill();
    }
}