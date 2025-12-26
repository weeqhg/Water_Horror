using System.Collections.Generic;
using UnityEngine;
using SimpleVoiceChat;
using TMPro;
using UnityEngine.UI;

public class VoiceSettingManager : MonoBehaviour
{
    [SerializeField] private Transform parentVoiceSettingUI;
    [SerializeField] private GameObject voicePlayerUI;

    private static VoiceSettingManager _instance;
    public static VoiceSettingManager Instance => _instance;


    private Dictionary<ulong, Speaker> _speakers = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void Register(ulong clientId, Speaker speaker)
    {
        _speakers.Add(clientId, speaker);
        CreateUI(clientId, speaker);
    }

    private void CreateUI(ulong clientId, Speaker speaker)
    {
        GameObject gameObject = Instantiate(voicePlayerUI, parentVoiceSettingUI);
        TextMeshProUGUI name = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        Slider slider = gameObject.GetComponentInChildren<Slider>();
        name.text = "Player: " + clientId;
        slider.onValueChanged.AddListener((value) => OnVolumeChanged(clientId, value));
    }
    private void OnVolumeChanged(ulong clientId, float volume)
    {
        // Применяем к Speaker
        if (_speakers.TryGetValue(clientId, out Speaker speaker))
        {
            AudioSource voiceVolume = speaker.Source;

            voiceVolume.volume = volume;
        }
    }

    private void SetMicrophone(string deviceName)
    {

    }

}
