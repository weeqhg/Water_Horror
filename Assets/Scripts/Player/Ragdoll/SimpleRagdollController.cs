using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SimpleRagdollController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Collider dopCollider;
    [SerializeField] private Rigidbody mainRigidbody;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform followCamera;

    [Header("Foot Settings")]
    [SerializeField] private bool freezeFeetInRagdoll = true;
    [SerializeField] private float footMassMultiplier = 0.5f; // Меньшая масса для стоп

    private bool isDie = false;
    void Start()
    {
        // Автоматически находим компоненты
        if (animator == null) animator = GetComponent<Animator>();
        if (mainCollider == null) mainCollider = GetComponent<Collider>();
        if (mainRigidbody == null) mainRigidbody = GetComponent<Rigidbody>();

        // Изначально выключаем Ragdoll
        SetRagdoll(false);
    }

    // Включить/выключить Ragdoll одной строкой
    public void SetRagdoll(bool enabled)
    {
        // Включаем/выключаем аниматор
        if (animator != null) animator.enabled = !enabled;

        // Включаем/выключаем основной коллайдер
        if (mainCollider != null) mainCollider.enabled = !enabled;

        // Настраиваем основной Rigidbody
        if (mainRigidbody != null)
        {
            mainRigidbody.isKinematic = enabled;
            mainRigidbody.useGravity = !enabled;
        }

        // Находим ВСЕ Rigidbody в детях (это кости Ragdoll)
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in allRigidbodies)
        {
            // Пропускаем основной Rigidbody
            if (rb == mainRigidbody) continue;

            // Включаем/выключаем физику для костей
            rb.isKinematic = !enabled;
            rb.useGravity = enabled;

            // Особые настройки для стоп
            if (freezeFeetInRagdoll && enabled)
            {
                ConfigureFootPhysics(rb);
            }
        }



        // Находим ВСЕ Collider в детях
        Collider[] allColliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in allColliders)
        {
            // Пропускаем основной Collider
            if (col == mainCollider) continue;
            if (col == dopCollider) continue;
            // Включаем/выключаем коллайдеры костей
            col.enabled = enabled;
        }

        if (enabled)
        {
            followCamera.localPosition = new Vector3(0f, 0f, -0.3f);
        }
        else
        {
            followCamera.localPosition = new Vector3(0f, 0f, 0f);
        }

        Debug.Log($"Ragdoll {(enabled ? "enabled" : "disabled")}");
    }

    private void ConfigureFootPhysics(Rigidbody rb)
    {
        // Проверяем, является ли это стопой
        string boneName = rb.gameObject.name.ToLower();
        bool isFoot = boneName.Contains("foot") || boneName.Contains("ankle") ||
                      boneName.Contains("левая стопа") || boneName.Contains("правая стопа");

        if (isFoot)
        {
            // Уменьшаем массу для меньшей инерции
            rb.mass *= footMassMultiplier;

            // Ограничиваем движение по осям
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ |
                            RigidbodyConstraints.FreezePositionY;

            // Добавляем пружину для возврата в исходное положение
            SpringJoint spring = rb.gameObject.AddComponent<SpringJoint>();
            spring.connectedBody = mainRigidbody;
            spring.spring = 50f;
            spring.damper = 5f;
            spring.tolerance = 0.01f;
        }
    }
    // Для нокаута
    public void Knockout(float duration = 2f)
    {
        if (isDie) return;

        SetRagdoll(true);
        playerController.Knock(true);
        Invoke(nameof(GetUp), duration);
    }

    // Для смерти
    public void ToggleDie(bool enable)
    {
        isDie = enable;
        playerController.Die(enable);
        SetRagdoll(enable);

        if (!enable)
        {
            GetUp();
            animator.SetTrigger("GetUp");
        }
    }


    // Подняться
    public void GetUp()
    {
        if (isDie) return;

        playerController.Knock(false);
        SetRagdoll(false);
    }
}
