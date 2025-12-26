using SimpleVoiceChat;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class AudioSettingMicrophone : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown microphoneDropdown;

    [Header("Display Mode Dropdown")]
    [SerializeField] private TMP_Dropdown ruDropdown;
    [SerializeField] private TMP_Dropdown enDropdown;

    public enum MicrophoneMode
    {
        VoiceActivation,  // Активация по голосу
        PushToTalk        // Активация по кнопке
    }

    void Start()
    {
        InitializeMicrophoneDropdown();
    }
    #region Microphone Selection
    // Инициализация списка микрофонов
    private void InitializeMicrophoneDropdown()
    {
        if (microphoneDropdown == null) return;

        // Очищаем текущие опции
        microphoneDropdown.ClearOptions();

        // Получаем список доступных микрофонов
        string[] microphones = Microphone.devices;

        if (microphones.Length == 0)
        {
            microphoneDropdown.interactable = false;
            microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("No microphones found"));
            return;
        }

        // Добавляем микрофоны в dropdown
        microphoneDropdown.AddOptions(microphones.ToList());

        // Выбираем микрофон по умолчанию
        string savedMic = PlayerPrefs.GetString("SelectedMicrophone", "");
        int savedIndex = System.Array.IndexOf(microphones, savedMic);

        if (savedIndex >= 0)
        {
            microphoneDropdown.value = savedIndex;
        }
        else if (Microphone.devices.Length > 0)
        {
            // Используем первый микрофон по умолчанию
            microphoneDropdown.value = 0;
            OnMicrophoneSelected(0);
        }

        microphoneDropdown.RefreshShownValue();
    }

    // Обработка выбора микрофона
    private void OnMicrophoneSelected(int index)
    {
        if (index < 0 || index >= Microphone.devices.Length) return;

        string selectedMic = Microphone.devices[index];

        // Сохраняем выбор
        PlayerPrefs.SetString("SelectedMicrophone", selectedMic);
        PlayerPrefs.Save();

        Debug.Log($"Microphone selected: {selectedMic}");

        // Можно сразу применить настройку к системе голосового чата
        ApplyMicrophoneToVoiceSystem(selectedMic);
    }

    private void ApplyMicrophoneToVoiceSystem(string deviceName)
    {
        if (Recorder.Instance != null)
        {
            Recorder.Instance.SetMicrophone(deviceName);

            Debug.Log($"Applying microphone to voice system: {deviceName}");
        }
    }
    #endregion

    #region Microphone Mode Selection

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        UpdateDropdownBasedOnLocale();
        LoadSave();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateDropdownBasedOnLocale();
    }

    private void UpdateDropdownBasedOnLocale()
    {
        var locale = LocalizationSettings.SelectedLocale;
        string language = locale?.Identifier.Code.ToLower() ?? "en";

        // Скрываем оба dropdown
        if (ruDropdown != null) ruDropdown.gameObject.SetActive(false);
        if (enDropdown != null) enDropdown.gameObject.SetActive(false);

        // Показываем нужный
        if (language.StartsWith("ru") && ruDropdown != null)
        {
            ruDropdown.gameObject.SetActive(true);
            // Настраиваем русские опции
            SetupRussianDropdown();
        }
        else if (enDropdown != null)
        {
            enDropdown.gameObject.SetActive(true);
            // Настраиваем английские опции
            SetupEnglishDropdown();
        }
    }
    private void SetupRussianDropdown()
    {
        ruDropdown.ClearOptions();
        ruDropdown.AddOptions(new List<string> { "Активация голосом", "Активация по кнопке" });

        // Загружаем сохраненное значение
        int savedValue = PlayerPrefs.GetInt("MicrophoneMode", 0);
        ruDropdown.value = savedValue;

        ruDropdown.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetInt("MicrophoneMode", value);
            OnMicrophoneModeChanged(value);
        });
    }

    private void SetupEnglishDropdown()
    {
        enDropdown.ClearOptions();
        enDropdown.AddOptions(new List<string> { "Voice activation", "Push-to-Talk" });

        // Загружаем сохраненное значение
        int savedValue = PlayerPrefs.GetInt("MicrophoneMode", 0);
        enDropdown.value = savedValue;

        enDropdown.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetInt("MicrophoneMode", value);
            OnMicrophoneModeChanged(value);
        });
    }
    private void OnMicrophoneModeChanged(int value)
    {
        if (Recorder.Instance != null)
        {
            if ((MicrophoneMode)value == MicrophoneMode.VoiceActivation)
            {
                Recorder.Instance.SetMode(true);
            }
            else
            {
                Recorder.Instance.SetMode(false);
            }
        }
    }

    private void LoadSave()
    {
        int savedValue = PlayerPrefs.GetInt("MicrophoneMode", 0);
        OnMicrophoneModeChanged(savedValue);
    }
    #endregion
}
