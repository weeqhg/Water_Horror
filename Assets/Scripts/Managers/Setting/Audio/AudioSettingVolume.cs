using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingVolume : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Volume Texts")]
    [SerializeField] private TMP_Text masterVolumeText;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private TMP_Text voiceVolumeText;

    [Header("Mixer Parameters")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string voiceVolumeParam = "VoiceVolume";


    private void Start()
    {
        InitializeSliders();
        LoadVolumeSettings();
    }

    private void InitializeSliders()
    {
        // Мастер-громкость
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0.0001f; // Минимальное значение для AudioMixer
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        // Музыка
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0.0001f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        // Звуковые эффекты
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0.0001f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // Голосовой чат
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.minValue = 0.0001f;
            voiceVolumeSlider.maxValue = 1f;
            voiceVolumeSlider.onValueChanged.AddListener(SetVoiceVolume);
        }
    }

    private void LoadVolumeSettings()
    {
        // Загружаем сохраненные настройки
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        SetVoiceVolume(PlayerPrefs.GetFloat("VoiceVolume", 1f));

        // Устанавливаем значения слайдерам
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (voiceVolumeSlider != null)
            voiceVolumeSlider.value = PlayerPrefs.GetFloat("VoiceVolume", 1f);
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(masterVolumeParam, volume, masterVolumeText, "Master");
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        SetVolume(musicVolumeParam, volume, musicVolumeText, "Music");
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        SetVolume(sfxVolumeParam, volume, sfxVolumeText, "SFX");
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetVoiceVolume(float volume)
    {
        SetVolume(voiceVolumeParam, volume, voiceVolumeText, "Voice");
        PlayerPrefs.SetFloat("VoiceVolume", volume);
        PlayerPrefs.Save();
    }

    private void SetVolume(string parameter, float volume, TMP_Text volumeText, string label)
    {
        // Конвертируем линейное значение (0-1) в децибелы (-80 to 0)
        float dB = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;

        // Устанавливаем значение в AudioMixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat(parameter, dB);
        }

        // Обновляем текст
        if (volumeText != null)
        {
            volumeText.text = $"{Mathf.RoundToInt(volume * 100)}";
        }
    }
}
