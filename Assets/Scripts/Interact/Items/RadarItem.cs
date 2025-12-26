using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RadarItem : Item
{
    private RadarSystem radarSystem;

    public override void SelectItem(GameObject player)
    {
        if (radarSystem == null) radarSystem = player.GetComponent<RadarSystem>();

        radarSystem.ToggleRadarUI(true);
        radarSystem.ToggleRadar(true);
    }

    public override void InteractItem() { }

    public override void ChangeItem()
    {
        radarSystem.ToggleRadar(false);

        radarSystem.ToggleRadarUI(false);





    }

    public override void DropItem()
    {
        if (radarSystem == null) return;

        radarSystem.ToggleRadar(false);

        radarSystem.ToggleRadarUI(false);

        radarSystem = null;
    }

    public override void GetSlider(Slider slider) { }

}
