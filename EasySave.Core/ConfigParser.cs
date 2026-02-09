using System.Text.Json;
using System.Text.Json.Nodes;
using EasySave.Core.Localization;
using EasySave.Models;

public class ConfigParser
{
    private readonly string _configPath;

    public JsonNode? Config { get; private set; }

    public ConfigParser(string configPath)
    {
        _configPath = configPath;
        LoadConfig();
    }

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

    private void SaveConfig()
    {
        string appDirectory = AppContext.BaseDirectory;
        string fullConfigPath = Path.IsPathRooted(_configPath) ? _configPath : Path.Combine(appDirectory, _configPath);

        string jsonContent = File.ReadAllText(fullConfigPath);
        Config = JsonNode.Parse(jsonContent);
    }

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

    public void saveJobs(List<Job> jobs)
    {
        if (Config is JsonObject configObject)
        {
            configObject["jobs"] = JsonSerializer.SerializeToNode(jobs, new JsonSerializerOptions { WriteIndented = true });
            EditAndSaveConfig(configObject);
        }
    }

    public void SetLogFormat(string format)
    {
        if (format != "json" && format != "xml")
        {
            throw new ArgumentException("Le format de log doit être 'json' ou 'xml'", nameof(format));
        }

        if (Config is JsonObject configObject && configObject["config"] is JsonObject configSection)
        {
            configSection["logFormat"] = format;
            EditAndSaveConfig(configObject);
        }
    }

    public string GetLogFormat()
    {
        return Config?["config"]?["logFormat"]?.GetValue<string>() ?? "json";
    }
}