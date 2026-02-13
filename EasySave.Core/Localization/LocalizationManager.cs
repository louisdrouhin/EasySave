using System.Globalization;
using System.Resources;
using System.Reflection;

namespace EasySave.Core.Localization
{
    public class LanguageChangedEventArgs : EventArgs
    {
        public string LanguageCode { get; set; }
        public CultureInfo Culture { get; set; }

        public LanguageChangedEventArgs(string languageCode, CultureInfo culture)
        {
            LanguageCode = languageCode;
            Culture = culture;
        }
    }


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

        public static void SetLanguage(CultureInfo culture)
        {
            _currentCulture = culture ?? new CultureInfo("fr");
            OnLanguageChanged(_currentCulture.TwoLetterISOLanguageName, _currentCulture);
        }

        private static void OnLanguageChanged(string languageCode, CultureInfo culture)
        {
            LanguageChanged?.Invoke(null, new LanguageChangedEventArgs(languageCode, culture));
        }

        public static string[] GetAvailableLanguages()
        {
            return new[] { "fr", "en" };
        }

        public static bool IsLanguageAvailable(string cultureCode)
        {
            return cultureCode == "fr" || cultureCode == "en";
        }
    }
}
