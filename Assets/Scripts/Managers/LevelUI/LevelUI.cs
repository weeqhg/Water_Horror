using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using TMPro;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup levelUI;
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private LocalizeStringEvent dayText;
    [SerializeField] private LocalizeStringEvent dayLoadGame;

    public void ToggleLevelUI(bool enable)
    {
        levelUI.alpha = enable ? 1.0f : 0.0f;
    }

    public void Initialized(int day, float value)
    {
        ToggleLevelUI(true);

        mainText.text = "" + value + "$";

        dayLoadGame.StringReference.Arguments = new object[] { day };
        dayText.RefreshString();

        dayText.StringReference.Arguments = new object[] { day };
        dayText.RefreshString();
    } 
}
