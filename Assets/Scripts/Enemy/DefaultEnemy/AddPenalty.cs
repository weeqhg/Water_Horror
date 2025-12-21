using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPenalty : MonoBehaviour
{
    [SerializeField] private OxygenPenaltyScriptableObject attackData;


    public void AddPenaltyPlayer(OxygenSystem playerOxygen)
    {
        playerOxygen.AddPenaltyServerRpc(
            attackData.id,
            attackData.displayName,
            attackData.penaltyAmount,
            attackData.isTemporary,
            attackData.penaltyColor,
            attackData.cureItem
        );
    }

    // Метод для смены атаки во время выполнения
    public void SetAttackData(OxygenPenaltyScriptableObject newOxygenPenalty)
    {
        attackData = newOxygenPenalty;
    }
}
