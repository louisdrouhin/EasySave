using System.Globalization;
using System.Resources;
using System.Reflection;

namespace EasySave.Core.Localization
{
    // Arguments d'événement levé quand la langue change
    public class LanguageChangedEventArgs : EventArgs
    {
        public string LanguageCode { get; set; }
        public CultureInfo Culture { get; set; }

        // @param languageCode - code de la nouvelle langue (fr, en)
        // @param culture - objet CultureInfo associé
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

        // Récupère la traduction d'une clé dans la langue actuelle
        // @param key - clé de traduction
        // @returns texte traduit ou [clé] si absent
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

        // Récupère une traduction formatée avec des paramètres
        // @param key - clé de traduction
        // @param args - paramètres pour le formatage (ex: {0}, {1})
        // @returns texte traduit et formaté
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

        // Change la langue active de l'application
        // @param cultureCode - code de la langue (fr, en)
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

        // Change la langue active avec un objet CultureInfo
        // @param culture - objet CultureInfo de la nouvelle langue
        public static void SetLanguage(CultureInfo culture)
        {
            _currentCulture = culture ?? new CultureInfo("fr");
            OnLanguageChanged(_currentCulture.TwoLetterISOLanguageName, _currentCulture);
        }

        // Déclenche l'événement LanguageChanged
        // @param languageCode - code de la langue
        // @param culture - objet CultureInfo
        private static void OnLanguageChanged(string languageCode, CultureInfo culture)
        {
            LanguageChanged?.Invoke(null, new LanguageChangedEventArgs(languageCode, culture));
        }

        // Récupère la liste des langues disponibles
        // @returns array des codes de langues supportées
        public static string[] GetAvailableLanguages()
        {
            return new[] { "fr", "en" };
        }

        // Vérifie si une langue est disponible
        // @param cultureCode - code de la langue à vérifier
        // @returns true si disponible, false sinon
        public static bool IsLanguageAvailable(string cultureCode)
        {
            return cultureCode == "fr" || cultureCode == "en";
        }
    }
}
