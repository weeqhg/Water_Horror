using DG.Tweening;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DoorOpen : NetworkBehaviour
{
    private bool isState = false;
    [SerializeField] private float doorCloseDelay = 0.5f;
    [SerializeField] private AudioRandomizer audioRandomizer;
    private bool isAnimating = false;



    [ServerRpc(RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        ToggleDoorClientRpc();
    }

    [ClientRpc]
    private void ToggleDoorClientRpc()
    {
        if (isAnimating) return;
        StartCoroutine(ToggleDoorCoroutine());
    }


    private IEnumerator ToggleDoorCoroutine()
    {
        if (isAnimating) yield break;

        isAnimating = true;
        audioRandomizer.ToggleAudio();
        bool targetState = !isState;


        // 1. Закрываем нужную дверь
        if (targetState)
        {
            // Поднимаем воду - закрываем внутреннюю
            transform.DORotate(new Vector3(0, 90, 0), doorCloseDelay);
        }
        else
        {
            // Опускаем воду - закрываем внешнюю
            transform.DORotate(new Vector3(0, 0, 0), doorCloseDelay);
        }

        // 2. Ждем закрытия двери
        yield return new WaitForSeconds(doorCloseDelay);
        isState = targetState;
        isAnimating = false;
    }



    public override void OnNetworkDespawn()
    {
        // Очищаем DOTween при уничтожении
        transform.DOKill();
    }
}