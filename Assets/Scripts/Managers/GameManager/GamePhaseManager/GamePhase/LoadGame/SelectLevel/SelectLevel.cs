using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SelectLevel : NetworkBehaviour
{
    [SerializeField] private GameObject selectMenu;
    [SerializeField] private List<WorldScriptableObject> worldsSOs;

    [SerializeField] private Image imageIconLocation;
    [SerializeField] private LocalizeStringEvent nameLocation;
    [SerializeField] private TextMeshProUGUI depthValue;
    [SerializeField] private Image[] star;


    [SerializeField] private Button exitMenu;
    [Header("Кнопки завершения локации")]
    [SerializeField] private GameObject finishedLocation;
    [SerializeField] private Button finishedLocationButton;


    [Header("Кнопки выбора локации")]
    [SerializeField] private GameObject selectButton;
    [SerializeField] private Button launchGame;
    [SerializeField] private Button nextLocation;
    [SerializeField] private Button lastLocation;

    private NetworkVariable<int> currentLocationIndex = new NetworkVariable<int>(0);
    private TVPanelAnimation tvPanelAnimation;

    private bool isEnable = false;

    private WorldScriptableObject currentSelectWorld;
    public bool IsEnable
    {
        get => isEnable;
        set
        {
            if (isEnable == value) return;

            isEnable = value;

            if (isEnable)
            {
                selectMenu.SetActive(true);
                //Блокируем движение игрока
                GlobalEventManager.BlockMove?.Invoke();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                tvPanelAnimation.ShowPanel();
            }
            if (!isEnable)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                tvPanelAnimation.HidePanel();
            }
        }
    }





    private void Start()
    {
        selectMenu.SetActive(false);
        IsEnable = false;
        tvPanelAnimation = selectMenu.GetComponent<TVPanelAnimation>();
        finishedLocation.SetActive(false);
        selectButton.SetActive(true);
        UpdateLocationUI();

        launchGame.onClick.AddListener(StartGameServerRpc);
        nextLocation.onClick.AddListener(NextLocationServerRpc);
        lastLocation.onClick.AddListener(LastLocationServerRpc);
        exitMenu.onClick.AddListener(ExitSelectMenu);
        finishedLocationButton.onClick.AddListener(FinishedGameServerRpc);

        GlobalEventManager.KeyCancel.AddListener(ExitSelectMenu);
    }




    public override void OnNetworkSpawn()
    {
        currentLocationIndex.OnValueChanged += OnChangedIndex;
    }








    private void OnChangedIndex(int oldVale, int newValue)
    {
        UpdateLocationUI();
    }



    private void ExitSelectMenu()
    {
        IsEnable = false;
        GlobalEventManager.UnBlockMove?.Invoke();
    }

    public void ToggleInteract()
    {
        IsEnable = !IsEnable;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        ShowButtonFinished();


        GlobalEventManager.StartGame?.Invoke(currentSelectWorld.locationId);


        UpdateUiClientRpc(true);
    }

    [ClientRpc]

    private void UpdateUiClientRpc(bool enable)
    {
        ExitSelectMenu();

        if (enable)
        {
            ShowButtonFinished();
        }
        else
        {
            ShowButtonSelect();
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void FinishedGameServerRpc()
    {
        ShowButtonSelect();

        GlobalEventManager.FinishedGame?.Invoke();

        UpdateUiClientRpc(false);
    }

    public int LocationIndex
    {
        get => currentLocationIndex.Value;
        set
        {
            if (value < 0 || value >= worldsSOs.Count) return;

            currentLocationIndex.Value = value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NextLocationServerRpc()
    {
        LocationIndex++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void LastLocationServerRpc()
    {
        LocationIndex--;
    }

    private void ShowButtonSelect()
    {
        selectButton.SetActive(true);
        finishedLocation.SetActive(false);
    }


    private void ShowButtonFinished()
    {
        selectButton.SetActive(false);
        finishedLocation.SetActive(true);
    }


    private void UpdateLocationUI()
    {

        currentSelectWorld = worldsSOs[currentLocationIndex.Value];

        imageIconLocation.sprite = currentSelectWorld.Icon;
        nameLocation.StringReference = currentSelectWorld.nameLocation;
        depthValue.text = currentSelectWorld.depth;
        int index = currentSelectWorld.difficulty;

        for (int i = 0; i < star.Length; i++)
        {
            if (i < index)
            {
                star[i].enabled = true;
            }
            else
            {
                star[i].enabled = false;
            }
        }
    }
}
