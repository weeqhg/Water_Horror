using UnityEngine;

[CreateAssetMenu(fileName = "New Oxygen Penalty", menuName = "Oxygen System/Oxygen Penalty")]
public class OxygenPenaltyScriptableObject : ScriptableObject
{
    public string id;
    public string displayName;
    public float penaltyAmount;
    public bool isTemporary;
    public Color penaltyColor;
    public string cureItem;
}