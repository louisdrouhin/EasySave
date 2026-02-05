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
        string pathTemplate = "config.example.json";

        if (!File.Exists(_configPath))
        {
            if (File.Exists(pathTemplate))
            {
                File.Copy(pathTemplate, _configPath);
            }
            else
            {
                throw new FileNotFoundException(LocalizationManager.Get("Error_ConfigFileNotFound"));
            }
        }

        string jsonContent = File.ReadAllText(_configPath);
        Config = JsonNode.Parse(jsonContent);
    }

    public void EditAndSaveConfig(JsonNode newConfig)
    {
        // Fusion des configurations (spread operator style JS: {...Config, ...newConfig})
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
        File.WriteAllText(_configPath, jsonString);
    }

    public void saveJobs(List<Job> jobs)
    {
        if (Config is JsonObject configObject)
        {
            configObject["jobs"] = JsonSerializer.SerializeToNode(jobs, new JsonSerializerOptions { WriteIndented = true });
            EditAndSaveConfig(configObject);
        }
    }
}