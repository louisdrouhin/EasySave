using System.Text.Json;
using System.Text.Json.Nodes;
using EasySave.Core.Localization;
using EasySave.Models;

// Parse et gère la configuration JSON de l'application
// Charge la configuration initiale et sauvegarde les modifications
public class ConfigParser
{
    private readonly string _configPath;

    public JsonNode? Config { get; private set; }

    // Initialise le parseur et charge la configuration
    // @param configPath - chemin du fichier config.json
    public ConfigParser(string configPath)
    {
        _configPath = configPath;
        LoadConfig();
    }

    // Charge la configuration depuis le fichier JSON
    // Crée le fichier à partir du template si absent
    public void LoadConfig()
    {
        string appDirectory = AppContext.BaseDirectory;
        string fullConfigPath = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(appDirectory, _configPath);
        string pathTemplate = Path.Combine(appDirectory, "config.example.json");

        if (!File.Exists(fullConfigPath))
        {
            if (File.Exists(pathTemplate))
            {
                File.Copy(pathTemplate, fullConfigPath);
            }
            else
            {
                throw new FileNotFoundException(LocalizationManager.Get("Error_ConfigFileNotFound"));
            }
        }

        string jsonContent = File.ReadAllText(fullConfigPath);
        Config = JsonNode.Parse(jsonContent);
    }

    // Recharge la configuration depuis le disque
    private void SaveConfig()
    {
        string appDirectory = AppContext.BaseDirectory;
        string fullConfigPath = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(appDirectory, _configPath);

        string jsonContent = File.ReadAllText(fullConfigPath);
        Config = JsonNode.Parse(jsonContent);
    }

    // Met à jour la configuration avec de nouvelles valeurs et sauvegarde
    // @param newConfig - nouvel objet de configuration JSON
    public void EditAndSaveConfig(JsonNode newConfig)
    {
        string appDirectory = AppContext.BaseDirectory;
        string fullConfigPath = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(appDirectory, _configPath);

        if (Config is JsonObject configObject && newConfig is JsonObject newConfigObject)
        {
            foreach (var property in newConfigObject)
            {
                configObject[property.Key] = property.Value?.DeepClone();
            }
        }
        else
        {
            Config = newConfig;
        }

        string jsonString = Config?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";
        File.WriteAllText(fullConfigPath, jsonString);
    }

    // Sauvegarde la liste des jobs dans la configuration
    // @param jobs - liste des jobs à persister
    public void saveJobs(List<Job> jobs)
    {
        if (Config is JsonObject configObject)
        {
            configObject["jobs"] = JsonSerializer.SerializeToNode(jobs, new JsonSerializerOptions { WriteIndented = true });
            EditAndSaveConfig(configObject);
        }
    }

    // Récupère les extensions de fichiers à chiffrer depuis la config
    // @returns liste des extensions (ex: .docx, .xlsx)
    public List<string> GetEncryptionExtensions()
    {
        var extensions = new List<string>();
        var encryptionNode = Config?["config"]?["encryption"];

        if (encryptionNode == null)
        {
            return extensions;
        }

        var extensionsArray = encryptionNode["extensions"]?.AsArray();

        if (extensionsArray != null)
        {
            foreach (var ext in extensionsArray)
            {
                var extension = ext?.GetValue<string>();
                if (!string.IsNullOrEmpty(extension))
                {
                    extensions.Add(extension.ToLower());
                }
            }
        }

        return extensions;
    }

    // Récupère le chemin du répertoire des logs
    // @returns chemin absolu du dossier logs
    public string GetLogsPath()
    {
        string logsPath = Config?["config"]?["logsPath"]?.GetValue<string>() ?? "./logs/";
        string appDirectory = AppContext.BaseDirectory;
        return Path.IsPathRooted(logsPath) ? logsPath : Path.Combine(appDirectory, logsPath);
    }

    // Récupère le format de log configuré (json ou xml)
    // @returns format de log actuel
    public string GetLogFormat()
    {
        return Config?["config"]?["logFormat"]?.GetValue<string>() ?? "json";
    }

    // Change le format de log et le persiste
    // @param format - nouveau format (json ou xml)
    public void SetLogFormat(string format)
    {
        if (format != "json" && format != "xml")
        {
            throw new ArgumentException(LocalizationManager.Get("Error_InvalidLogFormat"), nameof(format));
        }

        if (Config is JsonObject configObject && configObject["config"] is JsonObject configSection)
        {
            configSection["logFormat"] = format;
            EditAndSaveConfig(configObject);
        }
    }

    // Récupère la liste des applications métier configurées
    // @returns liste des noms d'applications (ex: CryptoSoft, etc)
    public List<string> GetBusinessApplications()
    {
        var applications = new List<string>();
        var businessAppsNode = Config?["config"]?["businessApplications"];

        if (businessAppsNode == null)
        {
            return applications;
        }

        var applicationsArray = businessAppsNode.AsArray();

        if (applicationsArray != null)
        {
            foreach (var app in applicationsArray)
            {
                var appName = app?.GetValue<string>();
                if (!string.IsNullOrEmpty(appName))
                {
                    applications.Add(appName.ToLower());
                }
            }
        }

        return applications;
    }

    // Récupère le nombre maximal de jobs concurrents
    // @returns nombre de jobs pouvant s'exécuter simultanément (défaut: 3)
    public int GetMaxConcurrentJobs()
    {
        var maxConcurrentJobs = Config?["config"]?["maxConcurrentJobs"]?.GetValue<int>();
        return maxConcurrentJobs ?? 3;
    }

    // Récupère les extensions de fichiers prioritaires
    // @returns liste des extensions à traiter en priorité
    public List<string> GetPriorityExtensions()
    {
        var extensions = new List<string>();
        var priorityExtensionsNode = Config?["config"]?["priorityExtensions"];

        if (priorityExtensionsNode == null)
        {
            return extensions;
        }

        var extensionsArray = priorityExtensionsNode.AsArray();

        if (extensionsArray != null)
        {
            foreach (var ext in extensionsArray)
            {
                var extension = ext?.GetValue<string>();
                if (!string.IsNullOrEmpty(extension))
                {
                    extensions.Add(extension.ToLower());
                }
            }
        }

        return extensions;
    }

    // Récupère le seuil de taille pour les fichiers volumineux en KB
    // @returns limite de taille en KB (-1 = pas de limite)
    public long GetLargeFileSizeLimitKb()
    {
        var limitNode = Config?["config"]?["largeFileSizeLimitKb"];
        if (limitNode != null)
        {
            return limitNode.GetValue<long>();
        }
        return -1; // -1 means no limit
    }
}
