using DG.Tweening;
using System.Collections;
using UnityEngine;

public class StaticEnemy : MonoBehaviour
{
    private AddPenalty addPenalty;
    private AudioSource audioSource;
    private ParticleSystem particleSystemEnemy;
    private Vector3 originalScale;

    private float duration = 7f;
    private bool isActive = true;

    private void Start()
    {
        originalScale = transform.localScale;
        addPenalty = GetComponent<AddPenalty>();
        audioSource = GetComponentInChildren<AudioSource>();
        particleSystemEnemy = GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            if (other.CompareTag("Player"))
            {
                OxygenSystem playerOxygen = other.GetComponent<OxygenSystem>();

                audioSource.Play();
                ActiveParticle();
                addPenalty.AddPenaltyPlayer(playerOxygen);

                transform.DOScale(originalScale * 0.5f, 0.2f)
            .OnComplete(() =>
            transform.DOScale(originalScale, 0.2f));
                StartCoroutine(ActiveRecharge());
            }
        }
    }

    private IEnumerator ActiveRecharge()
    {
        isActive = false;
        yield return new WaitForSeconds(duration);
        isActive = true;
    }
    private void ActiveParticle()
    {
        particleSystemEnemy.Play();
    }
}
