using System.Globalization;
using System.Resources;
using System.Reflection;

namespace EasySave.Core.Localization
{
    // Event arguments raised when language changes
    public class LanguageChangedEventArgs : EventArgs
    {
        public string LanguageCode { get; set; }
        public CultureInfo Culture { get; set; }

        // @param languageCode - code of new language (fr, en)
        // @param culture - associated CultureInfo object
        public LanguageChangedEventArgs(string languageCode, CultureInfo culture)
        {
            LanguageCode = languageCode;
            Culture = culture;
        }
    }

    // Gère la localisation de l'application
    // Fournit les traductions et gère les changements de langue
    public static class LocalizationManager
    {
        private static ResourceManager? _resourceManager;
        private static CultureInfo _currentCulture;
        public static CultureInfo CurrentCulture => _currentCulture;
        public static event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

        static LocalizationManager()
        {
            _resourceManager = new ResourceManager(
                "EasySave.Core.Ressources.Strings",
                Assembly.GetExecutingAssembly()
            );

            var systemCulture = CultureInfo.CurrentUICulture;
            if (systemCulture.TwoLetterISOLanguageName == "en" || systemCulture.TwoLetterISOLanguageName == "fr")
            {
                _currentCulture = systemCulture;
            }
            else
            {
                _currentCulture = new CultureInfo("fr");
            }
        }

        // Gets translation of a key in current language
        // @param key - translation key
        // @returns translated text or [key] if missing
        public static string Get(string key)
        {
            if (_resourceManager == null)
            {
                return $"[{key}]";
            }

            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }

        // Gets formatted translation with parameters
        // @param key - translation key
        // @param args - parameters for formatting (e.g. {0}, {1})
        // @returns translated and formatted text
        public static string GetFormatted(string key, params object[] args)
        {
            var template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        // Changes active application language
        // @param cultureCode - language code (fr, en)
        public static void SetLanguage(string cultureCode)
        {
            try
            {
                _currentCulture = new CultureInfo(cultureCode);
                OnLanguageChanged(cultureCode, _currentCulture);
            }
            catch (CultureNotFoundException)
            {
                Console.WriteLine(GetFormatted("Error_CultureNotFound", cultureCode));
            }
        }

        // Changes active language with CultureInfo object
        // @param culture - CultureInfo object of new language
        public static void SetLanguage(CultureInfo culture)
        {
            _currentCulture = culture ?? new CultureInfo("fr");
            OnLanguageChanged(_currentCulture.TwoLetterISOLanguageName, _currentCulture);
        }

        // Triggers LanguageChanged event
        // @param languageCode - language code
        // @param culture - CultureInfo object
        private static void OnLanguageChanged(string languageCode, CultureInfo culture)
        {
            LanguageChanged?.Invoke(null, new LanguageChangedEventArgs(languageCode, culture));
        }

        // Gets list of available languages
        // @returns array of supported language codes
        public static string[] GetAvailableLanguages()
        {
            return new[] { "fr", "en" };
        }

        // Checks if language is available
        // @param cultureCode - language code to check
        // @returns true if available, false otherwise
        public static bool IsLanguageAvailable(string cultureCode)
        {
            return cultureCode == "fr" || cultureCode == "en";
        }
    }
}
