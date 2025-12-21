using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [SerializeField] private GameObject mainSetting;
    [SerializeField] private GameObject soundSetting;
    [SerializeField] private GameObject graphicSetting;


    [SerializeField] private Button mainButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button graphicButton;
    private void Start()
    {
        mainButton.onClick.AddListener(ShowMain);
        soundButton.onClick.AddListener(ShowSound);
        graphicButton.onClick.AddListener(ShowGraphics);
        ShowMain();
    }
    public void ShowMain()
    {
        mainSetting.SetActive(true);
        soundSetting.SetActive(false);
        graphicSetting.SetActive(false);
    }

    public void ShowSound()
    {
        mainSetting.SetActive(false);
        soundSetting.SetActive(true);
        graphicSetting.SetActive(false);
    }

    public void ShowGraphics()
    {
        mainSetting.SetActive(false);
        soundSetting.SetActive(false);
        graphicSetting.SetActive(true);
    }
}
