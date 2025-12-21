using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class ButtonInteract : NetworkBehaviour
{
    [SerializeField] private bool isPos;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private bool isScale;
    [SerializeField] private Vector3 scaleMultiplier;
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public UnityEvent OnButtonInteractStart;
    private bool isAnimation;

    private Vector3 startPosition;
    private Vector3 startScale;
    public override void OnNetworkSpawn()
    {
        // Сохраняем начальные значения
        startPosition = transform.localPosition;
        startScale = transform.localScale;
    }

    public void OnButtonInteract()
    {
        if (IsClient && !isAnimation)
        {
            ToggleServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleServerRpc()
    {

        ToggleServerClientRpc();
    }

    [ClientRpc]
    public void ToggleServerClientRpc()
    {
        StartCoroutine(AnimateButton());
        OnButtonInteractStart?.Invoke();

    }
    private IEnumerator AnimateButton()
    {
        isAnimation = true;

        Vector3 targetPosition = isPos ? startPosition + positionOffset : transform.localPosition;
        Vector3 targetScale = isScale ? Vector3.Scale(startScale, scaleMultiplier) : transform.localScale;

        if (isPos)
        {
            yield return transform.DOLocalMove(targetPosition, animationDuration)
                .SetEase(animationCurve)
                .WaitForCompletion();
        }
        if (isScale)
        {
            yield return transform.DOScale(targetScale, animationDuration)
               .SetEase(animationCurve)
               .WaitForCompletion();
        }


        transform.localPosition = startPosition;
        transform.localScale = startScale;
        isAnimation = false;
    }
}
