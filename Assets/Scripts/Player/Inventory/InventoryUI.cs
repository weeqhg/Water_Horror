using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Image progressBarDrop;
    [SerializeField] private Image[] slotUI;
    [SerializeField] private Image[] selectedSlotUI;
    [SerializeField] private GameObject[] sliders;
    [SerializeField] private Sprite noneIcon;
    private int lastIndex = -1;


    public Slider GetSlider(int index)
    {
        return sliders[index].GetComponent<Slider>();
    }


    public void FullSlot(int index, Sprite icon)
    {
        if (index >= 0 && index < slotUI.Length)
        {
            if (icon != null)
            {
                slotUI[index].sprite = icon;
            }
            else
            {
                slotUI[index].sprite = noneIcon;
            }
        }
    }

    public void SelectSlot(int index)
    {
        // Сбрасываем предыдущий выделенный слот
        if (lastIndex != -1)
        {
            selectedSlotUI[lastIndex].enabled = false;
        }

        if (index < 0 || index >= selectedSlotUI.Length)
        {

            lastIndex = -1;
            return;
        }

        if (index >= 0 && index < selectedSlotUI.Length)
        {
            // Выделяем новый слот
            selectedSlotUI[index].enabled = true;
            lastIndex = index;
        }
    }

    public void SelectSliderSlot(int index, bool enable)
    {
        if (index >= 0 && index < sliders.Length)
        {
            sliders[index].SetActive(enable);
        }
    }

    public void ProgressBarDrop(float amount)
    {
        progressBarDrop.fillAmount = amount;
    }
}