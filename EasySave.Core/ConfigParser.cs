using System.Text.Json;
using System.Text.Json.Nodes;
using EasySave.Core.Localization;
using EasySave.Models;

// Parses and manages application JSON configuration
// Loads initial config and saves modifications
public class ConfigParser
{
    private readonly string _configPath;

    public JsonNode? Config { get; private set; }

    // Initializes parser and loads configuration
    // @param configPath - path to config.json file
    public ConfigParser(string configPath)
    {
        _configPath = configPath;
        LoadConfig();
    }

    // Loads configuration from JSON file
    // Creates file from template if absent
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

    // Reloads configuration from disk
    private void SaveConfig()
    {
        string appDirectory = AppContext.BaseDirectory;
        string fullConfigPath = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(appDirectory, _configPath);

        string jsonContent = File.ReadAllText(fullConfigPath);
        Config = JsonNode.Parse(jsonContent);
    }

    // Updates configuration with new values and saves
    // @param newConfig - new JSON configuration object
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

    // Saves job list to configuration
    // @param jobs - list of jobs to persist
    public void saveJobs(List<Job> jobs)
    {
        if (Config is JsonObject configObject)
        {
            configObject["jobs"] = JsonSerializer.SerializeToNode(jobs, new JsonSerializerOptions { WriteIndented = true });
            EditAndSaveConfig(configObject);
        }
    }

    // Gets file extensions to encrypt from config
    // @returns list of extensions (e.g. .docx, .xlsx)
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

    // Gets path of logs directory
    // @returns absolute path to logs folder
    public string GetLogsPath()
    {
        string logsPath = Config?["config"]?["logsPath"]?.GetValue<string>() ?? "./logs/";
        string appDirectory = AppContext.BaseDirectory;
        return Path.IsPathRooted(logsPath) ? logsPath : Path.Combine(appDirectory, logsPath);
    }

    // Gets configured log format (json or xml)
    // @returns current log format
    public string GetLogFormat()
    {
        return Config?["config"]?["logFormat"]?.GetValue<string>() ?? "json";
    }

    // Changes log format and persists
    // @param format - new format (json or xml)
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

    // Gets list of configured business applications
    // @returns list of application names (e.g. CryptoSoft, etc)
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

    // Gets maximum concurrent jobs
    // @returns number of jobs that can run simultaneously (default: 3)
    public int GetMaxConcurrentJobs()
    {
        var maxConcurrentJobs = Config?["config"]?["maxConcurrentJobs"]?.GetValue<int>();
        return maxConcurrentJobs ?? 3;
    }

    // Gets priority file extensions
    // @returns list of extensions to process first
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

    // Gets large file size threshold in KB
    // @returns size limit in KB (-1 = no limit)
    public long GetLargeFileSizeLimitKb()
    {
        var limitNode = Config?["config"]?["largeFileSizeLimitKb"];
        if (limitNode != null)
        {
            return limitNode.GetValue<long>();
        }
        return -1; // -1 means no limit
    }

    // Gets EasyLog server enabled status
    // @returns true if server is enabled, false otherwise
    public bool GetEasyLogServerEnabled()
    {
        return Config?["easyLogServer"]?["enabled"]?.GetValue<bool>() ?? false;
    }

    // Gets EasyLog server mode
    // @returns server mode
    public string GetEasyLogServerMode()
    {
        return Config?["easyLogServer"]?["mode"]?.GetValue<string>() ?? "N/A";
    }

    // Gets EasyLog server host address
    // @returns host address or IP
    public string GetEasyLogServerHost()
    {
        return Config?["easyLogServer"]?["host"]?.GetValue<string>() ?? "N/A";
    }

    // Gets EasyLog server port number
    // @returns port number
    public int GetEasyLogServerPort()
    {
        return Config?["easyLogServer"]?["port"]?.GetValue<int>() ?? 0;
    }

    // Sets EasyLog server enabled status
    // @param enabled - true to enable, false to disable
    public void SetEasyLogServerEnabled(bool enabled)
    {
        if (Config is JsonObject configObject && configObject["easyLogServer"] is JsonObject serverConfig)
        {
            serverConfig["enabled"] = enabled;
            EditAndSaveConfig(configObject);
        }
    }

    // Sets EasyLog server mode
    // @param mode - new server mode (local_only, server_only, both)
    public void SetEasyLogServerMode(string mode)
    {
        if (Config is JsonObject configObject && configObject["easyLogServer"] is JsonObject serverConfig)
        {
            serverConfig["mode"] = mode;
            EditAndSaveConfig(configObject);
        }
    }

    // Sets EasyLog server host address
    // @param host - new host address or IP
    public void SetEasyLogServerHost(string host)
    {
        if (Config is JsonObject configObject && configObject["easyLogServer"] is JsonObject serverConfig)
        {
            serverConfig["host"] = host;
            EditAndSaveConfig(configObject);
        }
    }

    // Sets EasyLog server port number
    // @param port - new port number
    public void SetEasyLogServerPort(int port)
    {
        if (Config is JsonObject configObject && configObject["easyLogServer"] is JsonObject serverConfig)
        {
            serverConfig["port"] = port;
            EditAndSaveConfig(configObject);
        }
    }
}
