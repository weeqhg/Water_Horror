using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.ParticleSystem;
public class StaticEnemy : MonoBehaviour
{
    private AddPenalty addPenalty;
    private AudioSource audioSource;
    private ParticleSystem particleSystem;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
        addPenalty = GetComponent<AddPenalty>();
        audioSource = GetComponentInChildren<AudioSource>();
        particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!IsOwner) return;
        if (other.CompareTag("Player"))
        {
            OxygenSystem playerOxygen = other.GetComponent<OxygenSystem>();

            audioSource.Play();
            ActiveParticleClientRpc();
            addPenalty.AddPenaltyPlayer(playerOxygen);

            transform.DOScale(originalScale * 0.8f, 0.2f)
        .OnComplete(() => transform.DOScale(originalScale, 0.2f));
        }
    }

    //[ClientRpc]
    private void ActiveParticleClientRpc()
    {
        particleSystem.Play();
    }
}
