using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class LockerInteract : NetworkBehaviour
{
    private GameObject currentPlayer;
    [SerializeField] private Transform pos;
    [SerializeField] private GameObject menu;
    [SerializeField] private Canvas canvasMenu;
    private InteractableObject interactableObject;
    private bool isEnterLocker;

    private void Start()
    {
        interactableObject = GetComponent<InteractableObject>();
        GlobalEventManager.KeyCancel.AddListener(ExitLocker);
    }

    public void OnInteractLocker()
    {
        currentPlayer = interactableObject.GetPlayer();
        
        if (currentPlayer != null)
        {
            currentPlayer.transform.position = pos.position;
            currentPlayer.transform.rotation = pos.rotation;
        }
        EnterLocker();
    }

    private void EnterLocker()
    {
        isEnterLocker = true;
        StartCoroutine(SetMenu(isEnterLocker, 0f));
        SetPlayerCamera(!isEnterLocker);
    }

    private void ExitLocker()
    {
        if (currentPlayer == null) return;
        isEnterLocker = false;
        StartCoroutine(SetMenu(isEnterLocker, 0.5f));
        SetPlayerCamera(!isEnterLocker);
        currentPlayer = null;
    }

    private IEnumerator SetMenu(bool enabled, float duration)
    {
        canvasMenu.enabled = enabled;
        yield return new WaitForSeconds(duration);

        menu.SetActive(enabled);

        Cursor.lockState = enabled ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = enabled;
    }

    public void ChangeColor(Button button)
    {
        Color buttonColor = button.colors.normalColor;
        ChangeColorPlayer changeColorPlayer = currentPlayer.GetComponent<ChangeColorPlayer>();
        changeColorPlayer.ChangeColorMaterial(buttonColor);

    }

    private void SetPlayerCamera(bool enabled)
    {
        Camera playerCamera = currentPlayer.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.enabled = enabled;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentPlayer == other.gameObject)
        {
            ExitLocker();
        }
    }
}
