using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot waterSnapshot;

    public void ToggleSnapshot(bool isWater)
    {
        if (!isWater) normalSnapshot.TransitionTo(1f);
        else waterSnapshot.TransitionTo(1f);
    }
}
