using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GraphicsManager : MonoBehaviour
{
    [Header("Display Mode Dropdown")]
    public TMP_Dropdown displayModeDropdownRu;
    public TMP_Dropdown displayModeDropdownEng;

    private TMP_Dropdown currentDisplayModeDropdown;
    private bool isLanguageChanging = false;

    [Header("Resolution Dropdown")]
    public TMP_Dropdown resolutionDropdown;

    private FullScreenMode currentDisplayMode;
    private Resolution currentResolution;

    // Список поддерживаемых режимов отображения
    private List<FullScreenMode> displayModes = new List<FullScreenMode>
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed
    };

    // Словарь для локализованных названий режимов
    private Dictionary<string, Dictionary<FullScreenMode, string>> localizedModeNames =
        new Dictionary<string, Dictionary<FullScreenMode, string>>();

    // Список доступных разрешений
    private List<Resolution> availableResolutions;

    private void Start()
    {
        InitializeLocalizedNames();
        InitializeDisplaySettings();
        SetupResolutionDropdown();
        SetupDisplayModeDropdowns(); // Новый метод
        LoadDisplaySettings();

        InitializeCurrentLanguageDropdown();

        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void InitializeLocalizedNames()
    {
        // Русские названия
        localizedModeNames["ru"] = new Dictionary<FullScreenMode, string>
        {
            { FullScreenMode.ExclusiveFullScreen, "Полный экран" },
            { FullScreenMode.FullScreenWindow, "Оконный без рамки" },
            { FullScreenMode.Windowed, "Оконный режим" }
        };

        // Английские названия
        localizedModeNames["en"] = new Dictionary<FullScreenMode, string>
        {
            { FullScreenMode.ExclusiveFullScreen, "Fullscreen" },
            { FullScreenMode.FullScreenWindow, "Fullscreen Window" },
            { FullScreenMode.Windowed, "Windowed" }
        };
    }

    private void SetupDisplayModeDropdowns()
    {
        // Настраиваем оба dropdown
        SetupSingleDisplayModeDropdown(displayModeDropdownRu, "ru");
        SetupSingleDisplayModeDropdown(displayModeDropdownEng, "en");
    }

    private void SetupSingleDisplayModeDropdown(TMP_Dropdown dropdown, string languageCode)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();

        List<string> options = new List<string>();

        // Добавляем локализованные опции
        if (localizedModeNames.ContainsKey(languageCode))
        {
            foreach (var mode in displayModes)
            {
                if (localizedModeNames[languageCode].ContainsKey(mode))
                {
                    options.Add(localizedModeNames[languageCode][mode]);
                }
            }
        }
        else
        {
            // Fallback на английский
            foreach (var mode in displayModes)
            {
                options.Add(mode.ToString());
            }
        }

        dropdown.AddOptions(options);

        // Добавляем слушатель (будет удален при переключении языка)
        dropdown.onValueChanged.AddListener(OnDisplayModeChanged);
    }

    private void InitializeDisplaySettings()
    {
        // Получаем текущие настройки
        currentDisplayMode = Screen.fullScreenMode;
        currentResolution = Screen.currentResolution;

        // Получаем список доступных разрешений
        availableResolutions = new List<Resolution>(Screen.resolutions);

        // Сортируем разрешения по убыванию
        availableResolutions.Sort((a, b) =>
            b.width != a.width ? b.width.CompareTo(a.width) : b.height.CompareTo(a.height));
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();

        // Убираем дубликаты разрешений
        HashSet<string> uniqueResolutions = new HashSet<string>();

        foreach (var resolution in availableResolutions)
        {
            string option = $"{resolution.width} × {resolution.height}";
            if (uniqueResolutions.Add(option)) // Add возвращает true если элемент новый
            {
                resolutionOptions.Add(option);
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        // Устанавливаем текущее разрешение
        SetCurrentResolutionInDropdown();
    }

    private void InitializeCurrentLanguageDropdown()
    {
        string currentLang = LocalizationSettings.SelectedLocale.Identifier.Code;
        SwitchDisplayModeDropdown(currentLang);
        SetCurrentDisplayModeInDropdown(); // Важно: установить текущее значение
    }

    private void SwitchDisplayModeDropdown(string languageCode)
    {
        // Сохраняем текущий режим экрана перед сменой dropdown
        FullScreenMode currentMode = currentDisplayMode;

        // Отписываемся от старого dropdown
        if (currentDisplayModeDropdown != null)
        {
            currentDisplayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);
        }

        // Выбираем нужный dropdown
        TMP_Dropdown newDropdown = null;

        switch (languageCode)
        {
            case "ru":
                newDropdown = displayModeDropdownRu;
                if (displayModeDropdownRu != null)
                {
                    displayModeDropdownRu.gameObject.SetActive(true);
                    // Обновляем опции если нужно
                    if (displayModeDropdownRu.options.Count == 0)
                        SetupSingleDisplayModeDropdown(displayModeDropdownRu, "ru");
                }
                if (displayModeDropdownEng != null)
                    displayModeDropdownEng.gameObject.SetActive(false);
                break;

            case "en":
                newDropdown = displayModeDropdownEng;
                if (displayModeDropdownEng != null)
                {
                    displayModeDropdownEng.gameObject.SetActive(true);
                    if (displayModeDropdownEng.options.Count == 0)
                        SetupSingleDisplayModeDropdown(displayModeDropdownEng, "en");
                }
                if (displayModeDropdownRu != null)
                    displayModeDropdownRu.gameObject.SetActive(false);
                break;
        }

        if (newDropdown == null)
        {
            Debug.LogError($"No dropdown found for language: {languageCode}");
            return;
        }

        currentDisplayModeDropdown = newDropdown;

        // Добавляем слушатель
        currentDisplayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);

        // Восстанавливаем текущее значение
        SetCurrentDisplayModeInDropdown();
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        if (isLanguageChanging) return;
        isLanguageChanging = true;

        try
        {
            string languageCode = newLocale.Identifier.Code;
            SwitchDisplayModeDropdown(languageCode);
        }
        finally
        {
            isLanguageChanging = false;
        }
    }

    private void SetCurrentDisplayModeInDropdown()
    {
        if (currentDisplayModeDropdown == null) return;

        int currentModeIndex = displayModes.IndexOf(currentDisplayMode);
        if (currentModeIndex >= 0 && currentModeIndex < currentDisplayModeDropdown.options.Count)
        {
            currentDisplayModeDropdown.value = currentModeIndex;
        }
    }

    private void SetCurrentResolutionInDropdown()
    {
        if (resolutionDropdown == null) return;

        string currentResString = $"{currentResolution.width} × {currentResolution.height}";

        for (int i = 0; i < resolutionDropdown.options.Count; i++)
        {
            if (resolutionDropdown.options[i].text == currentResString)
            {
                resolutionDropdown.value = i;
                break;
            }
        }
    }

    private void OnDisplayModeChanged(int index)
    {
        if (index < 0 || index >= displayModes.Count) return;

        FullScreenMode newMode = displayModes[index];

        // Применяем новый режим с текущим разрешением
        Screen.SetResolution(currentResolution.width, currentResolution.height, newMode);
        currentDisplayMode = newMode;

        Debug.Log($"Display mode changed to: {newMode}");
    }

    private void OnResolutionChanged(int index)
    {
        if (resolutionDropdown == null || index < 0 || index >= availableResolutions.Count) return;

        Resolution selectedResolution = availableResolutions[index];

        // Применяем новое разрешение с текущим режимом
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, currentDisplayMode);
        currentResolution = selectedResolution;

        Debug.Log($"Resolution changed to: {selectedResolution.width}x{selectedResolution.height}");
    }

    public void ApplySettings()
    {
        SaveDisplaySettings();
        Debug.Log("Graphics settings applied and saved");
    }

    private void SaveDisplaySettings()
    {
        PlayerPrefs.SetInt("ScreenWidth", currentResolution.width);
        PlayerPrefs.SetInt("ScreenHeight", currentResolution.height);
        PlayerPrefs.SetInt("FullScreenMode", (int)currentDisplayMode);
        PlayerPrefs.Save();
    }

    private void LoadDisplaySettings()
    {
        if (PlayerPrefs.HasKey("ScreenWidth"))
        {
            int width = PlayerPrefs.GetInt("ScreenWidth");
            int height = PlayerPrefs.GetInt("ScreenHeight");
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("FullScreenMode");

            // Находим ближайшее доступное разрешение
            Resolution closestRes = FindClosestResolution(width, height);

            // Применяем сохраненные настройки
            Screen.SetResolution(closestRes.width, closestRes.height, mode);

            // Обновляем переменные
            currentResolution = closestRes;
            currentDisplayMode = mode;

            // Обновляем UI
            SetCurrentResolutionInDropdown();
            SetCurrentDisplayModeInDropdown();
        }
    }

    private Resolution FindClosestResolution(int targetWidth, int targetHeight)
    {
        Resolution closest = availableResolutions[0];
        int minDiff = int.MaxValue;

        foreach (var res in availableResolutions)
        {
            int diff = Mathf.Abs(res.width - targetWidth) + Mathf.Abs(res.height - targetHeight);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = res;
            }
        }

        return closest;
    }

    public void ResetToDefaults()
    {
        // Сбрасываем к настройкам по умолчанию
        Resolution defaultRes = availableResolutions[0]; // Самое высокое разрешение
        FullScreenMode defaultMode = FullScreenMode.FullScreenWindow;

        // Применяем настройки
        Screen.SetResolution(defaultRes.width, defaultRes.height, defaultMode);

        // Обновляем переменные
        currentResolution = defaultRes;
        currentDisplayMode = defaultMode;

        // Обновляем UI
        SetCurrentResolutionInDropdown();
        SetCurrentDisplayModeInDropdown();

        Debug.Log("Graphics settings reset to defaults");
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (displayModeDropdownRu != null)
            displayModeDropdownRu.onValueChanged.RemoveListener(OnDisplayModeChanged);

        if (displayModeDropdownEng != null)
            displayModeDropdownEng.onValueChanged.RemoveListener(OnDisplayModeChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }
}