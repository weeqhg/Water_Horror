using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using TMPro;

[System.Serializable]
public class InformationShop
{
    public LocalizeStringEvent nameItem;
    public TextMeshProUGUI price;
    public LocalizeStringEvent description;
}

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private TVPanelAnimation tvPanelAnimation;
    [SerializeField] private Button exitMenu;
    [SerializeField] private Button buyItem;
    [SerializeField] private ShopManager shopManager;

    [SerializeField] private Transform containerShowcase;
    [SerializeField] private GameObject prefabCell;
    [SerializeField] private InformationShop informationShop;
    [SerializeField] private Transform basketContainer;

    private GameObject currentSelect;
    private GameObject previousSelect;

    private List<GameObject> cells = new();
    private void Start()
    {
        shopCanvas.SetActive(false);
        exitMenu.onClick.AddListener(ExitShopMenu);
        buyItem.onClick.AddListener(OnBuyItem);
    }

    public void ShowPanel()
    {
        shopCanvas.SetActive(true);
        tvPanelAnimation.ShowPanel();
    }

    public void HidePanel()
    {
        tvPanelAnimation.HidePanel();
    }

    private void ExitShopMenu()
    {
        shopManager.ExitShopMenu();
    }


    public void CreateShowcase(GameObject itemPrefab)
    {
        GameObject cellobject = Instantiate(prefabCell);

        cellobject.transform.SetParent(containerShowcase);

        cellobject.transform.localScale = Vector3.one;

        cells.Add(cellobject);

        Item item = itemPrefab.GetComponent<Item>();

        CellItem cellItem = cellobject.GetComponent<CellItem>();

        cellItem.InitializedCell(item, informationShop, shopManager, this);

    }

    public void ClearShopUI()
    {
        if (cells.Count > 0)
        {
            foreach (var cell in cells)
            {
                Destroy(cell);
            }

            cells.Clear();
        }
    }

    public void SelectItem(GameObject gameObject)
    {
        // Если кликаем на уже выбранный объект - отменяем выбор
        if (currentSelect == gameObject)
        {
            DeselectItem();
            return;
        }

        // Если есть предыдущий выбранный - возвращаем его
        if (currentSelect != null)
        {
            currentSelect.transform.SetParent(containerShowcase);
        }

        // Выбираем новый
        currentSelect = gameObject;
        currentSelect.transform.SetParent(basketContainer);

        // Обновляем предыдущий
        previousSelect = currentSelect;
    }

    private void DeselectItem()
    {
        if (currentSelect != null)
        {
            currentSelect.transform.SetParent(containerShowcase);
            previousSelect = currentSelect;
            currentSelect = null;
        }
    }

    public void OnBuyItem()
    {
        shopManager.BuyItemServerRpc();
    }
}
