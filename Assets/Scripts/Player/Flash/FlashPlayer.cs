using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class FlashPlayer : NetworkBehaviour
{
    [SerializeField] private Light flash;
    private PlayerController playerController;
    private bool isFlash = false;
    private float minDensity = 0.3f;
    private float maxDensity = 0.1f;
    [SerializeField] private Material fog;
    private Coroutine flashCoroutine;

    private float transitionDuration = 1.0f; // Длительность перехода в секундах

    // NetworkVariable для индивидуальных фонариков
    private NetworkVariable<bool> networkFlashState = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner  // Только владелец может менять
    );

    public override void OnNetworkSpawn()
    {
        // Подписываемся на изменения NetworkVariable
        networkFlashState.OnValueChanged += OnFlashStateChanged;
        isFlash = networkFlashState.Value;

        if (IsOwner)
        {
            playerController = GetComponent<PlayerController>();

            ChangeFog(playerController.InWater);
        }
        // Все клиенты получают начальное состояние из NetworkVariable
        OnFlash(isFlash);
    }

    public void OnFlashPlayer(bool enable)
    {
        if (!IsOwner) return;

        isFlash = enable;
        // ОБНОВЛЯЕМ NetworkVariable - это синхронизирует состояние
        networkFlashState.Value = isFlash;
        OnFlash(isFlash);

    }

    private void OnFlashStateChanged(bool oldValue, bool newValue)
    {
        // Вызывается при изменении NetworkVariable на всех клиентах
        isFlash = newValue;
        flash.enabled = newValue;
    }

    private void OnFlash(bool enable)
    {
        if (!IsOwner) return;
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);


        flashCoroutine = StartCoroutine(ChangeFogDistance(enable ? maxDensity : minDensity));

    }

    private IEnumerator ChangeFogDistance(float targetDensity)
    {
        //float startDensity = fog.GetFloat("_Density");
        float startDensity = RenderSettings.fogDensity;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;

            // Плавная интерполяция
            //fog.SetFloat("_Density", Mathf.Lerp(startDensity, targetDensity, t));
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, t);
            yield return null;
        }

        RenderSettings.fogDensity = targetDensity;
        // Гарантируем, что достигли целевого значения
        //fog.SetFloat("_Density", targetDensity);

    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Dry"))
        {
            ChangeFog(false);
        }
        if (other.CompareTag("Water"))
        {
            ChangeFog(true);
        }

    }

    private void ChangeFog(bool enable)
    {
        if (enable)
        {
            //minDensity = 2.2f;
            //maxDensity = 1f;

            minDensity = 0.2f;
            maxDensity = 0.05f;

            OnFlash(isFlash);
        }
        else
        {
            //minDensity = 0.5f;
            //maxDensity = 0.3f;

            minDensity = 0.1f;
            maxDensity = 0.01f;


            OnFlash(isFlash);
        }
    }
}
