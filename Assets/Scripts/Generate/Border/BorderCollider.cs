using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderCollider : MonoBehaviour
{
    [SerializeField] private OxygenPenaltyScriptableObject borderPenalty;
    [SerializeField] private float durationOnDamager = 1f;
    private OxygenSystem oxygenSystem;
    private Coroutine damageCoroutine;

    private IEnumerator DeathZone()
    {
        while (true)
        {
            // Ждем 1 секунду
            yield return new WaitForSeconds(durationOnDamager);
            TakeDamage();
        }
    }

    private void TakeDamage()
    {
        oxygenSystem.AddPenaltyServerRpc(
                    borderPenalty.id,
                    borderPenalty.displayName,
                    borderPenalty.penaltyAmount,
                    borderPenalty.isTemporary,
                    borderPenalty.penaltyColor,
                    borderPenalty.cureItem
                );
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            oxygenSystem = other.gameObject.GetComponent<OxygenSystem>();

            if (oxygenSystem == null) return;

            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }

            TakeDamage();
            damageCoroutine = StartCoroutine(DeathZone());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            oxygenSystem = null;

            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }
}
