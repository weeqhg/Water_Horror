using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using TMPro;



public class CellItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Color outline;
    private Color baseColor;

    private Image boder;

    private Item currentItem;

    private InformationShop informationShop;
    private ShopManager shopManager;
    private ShopUI shopUI;

    private void Start()
    {
        boder = GetComponent<Image>();
        baseColor = boder.color;
    }


    public void InitializedCell(Item item, InformationShop informationShop, ShopManager shopManager, ShopUI shopUI)
    {
        currentItem = item;
        this.informationShop = informationShop;
        this.shopManager = shopManager;
        this.shopUI = shopUI;

        icon.sprite = item.Icon;

    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        boder.color = outline;
        transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem.NameItem != null) informationShop.nameItem.StringReference = currentItem.NameItem;
        if (currentItem.Price > 0) informationShop.price.text = currentItem.Price.ToString();
        if (currentItem.DescriptionItem != null) informationShop.description.StringReference = currentItem.DescriptionItem;

        shopManager.SelectCellOnServerRpc(currentItem.Id);
        shopUI.SelectItem(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        boder.color = baseColor;
        transform.DOScale(1f, 0.2f).SetEase(Ease.InBack);
    }
}
