using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    [Header("Language Dropdown")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        // Инициализация dropdown
        if (languageDropdown != null)
        {
            // Загружаем сохраненный язык
            LoadSavedLanguage();
        }
    }

    public void OnDropdownValueChanged(int selectedIndex)
    {
        string languageCode = selectedIndex switch
        {
            0 => "ru", // English
            1 => "en", // Russian
            _ => "en"
        };

        ChangeLanguage(languageCode);
    }

    private void ChangeLanguage(string languageCode)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;

            // Сохраняем выбор
            PlayerPrefs.SetString("SelectedLanguage", languageCode);
            PlayerPrefs.Save();
        }
    }

    private void LoadSavedLanguage()
    {
        string savedLang = PlayerPrefs.GetString("SelectedLanguage", "en");

        // Устанавливаем правильное значение в dropdown
        int dropdownIndex = savedLang switch
        {
            "en" => 0,
            "ru" => 1,
            "es" => 2,
            _ => 0
        };

        // Временно отключаем обработчик чтобы избежать рекурсии
        if (languageDropdown != null)
        {
            languageDropdown.value = dropdownIndex;
        }
    }
}