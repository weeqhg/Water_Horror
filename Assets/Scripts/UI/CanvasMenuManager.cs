using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject selectMenu;
    [SerializeField] private GameObject lockerMenu;

    [SerializeField] private GameObject menuPanelUI;
    [SerializeField] private GameObject settingPanelMenuUI;

    [SerializeField] private bool isMainMenu = false;

    private bool isEnable = false;

    private void Start()
    {
        menuPanelUI.SetActive(false);
        GlobalEventManager.KeyCancel.AddListener(ToggleCanvasMenu);
    }


    private void ToggleCanvasMenu()
    {
        if (selectMenu.activeSelf || lockerMenu.activeSelf) return;


        isEnable = !isEnable;

        if (isEnable)
        {
            //Блокируем движение игрока
            GlobalEventManager.BlockMove?.Invoke();

            ShowCanvasMenu();
        }
        else
        {
            HideCanvasMenu();
        }

    }

    public void ShowCanvasMenu()
    {
        if (isMainMenu) return;
       
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuPanelUI.SetActive(true);
        settingPanelMenuUI.SetActive(false);
    }

    private void HideCanvasMenu()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        menuPanelUI.SetActive(false);
        settingPanelMenuUI.SetActive(false);
    }

    public void ShowSettingMenu()
    {
        menuPanelUI.SetActive(false);
        settingPanelMenuUI.SetActive(true);
    }

}
