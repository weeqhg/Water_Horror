using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [Header("Настройка меню настроек")]
    [SerializeField] private List<GameObject> otherMenu = new();

    [SerializeField] private GameObject mainMenuSettingUI;
    [SerializeField] private GameObject settingPanelMenuUI;

    private bool isEnable = false;

    public bool IsEnable
    {
        get => isEnable;
        set
        {
            if (isEnable == value) return;

            isEnable = value;

            if (isEnable)
            {
                ShowCanvasMenu();

                GlobalEventManager.BlockMove?.Invoke();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (!isEnable)
            {   
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                HideCanvasMenu();
            }
        }
    }

    private void Start()
    {
        mainMenuSettingUI.SetActive(false);
        settingPanelMenuUI.SetActive(false);
        GlobalEventManager.KeyCancel.AddListener(ToggleCanvasMenu);
    }


    private void ToggleCanvasMenu()
    {
        if (otherMenu.Count > 0)
        {
            foreach (GameObject menu in otherMenu)
            {
                if (menu.activeSelf) return;
            }
        }

        IsEnable = !IsEnable;
    }

    public void ShowCanvasMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mainMenuSettingUI.SetActive(true);
        settingPanelMenuUI.SetActive(false);
    }

    private void HideCanvasMenu()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mainMenuSettingUI.SetActive(false);
        settingPanelMenuUI.SetActive(false);
    }

    public void ShowSettingMenu()
    {
        mainMenuSettingUI.SetActive(false);
        settingPanelMenuUI.SetActive(true);
    }

}
