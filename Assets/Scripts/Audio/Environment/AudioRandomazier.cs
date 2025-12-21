using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRandomizer : MonoBehaviour
{
    [Header("Audio Settings")]
    [Range(0.1f, 2f)] public float pitchMin = 0.9f;
    [Range(0.1f, 2f)] public float pitchMax = 1.1f;
    [Range(0f, 1f)] public float volumeMultiplier = 1f;
    [SerializeField] private AudioClip[] clips;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void ToggleAudio()
    {
        audioSource.clip = clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);

        audioSource.volume = Random.Range(0.8f, 1f) * volumeMultiplier;

        audioSource.Play();
    }

}
