using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace EasySave.GUI.Pages;

public class SimpleLogEntry
{
    public string LogText { get; set; } = string.Empty;
}

public partial class LogsPage : UserControl
{
    private readonly ObservableCollection<SimpleLogEntry> _logs;
    private readonly ConfigParser _configParser;
    private FileSystemWatcher? _fileWatcher;
    private string _currentLogFilePath = string.Empty;

    public LogsPage() : this(null)
    {
    }

    public LogsPage(ConfigParser? configParser)
    {
        _logs = new ObservableCollection<SimpleLogEntry>();
        _configParser = configParser ?? new ConfigParser("config.json");

        InitializeComponent();

        // Assigner l'ItemsSource après InitializeComponent
        var logsListBox = this.FindControl<ListBox>("LogsListBox");
        if (logsListBox != null)
        {
            Console.WriteLine("[LogsPage] ListBox trouvé, assignation de ItemsSource");
            logsListBox.ItemsSource = _logs;
        }
        else
        {
            Console.WriteLine("[LogsPage] ERREUR: ListBox non trouvé!");
        }

        // Attacher les gestionnaires d'événements
        var openFolderButton = this.FindControl<Button>("OpenFolderButton");
        if (openFolderButton != null)
        {
            openFolderButton.Click += OnOpenFolderClick;
        }

        // Charger les logs initiaux
        LoadLogs();

        // Démarrer la surveillance du fichier
        StartFileWatcher();
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            // Obtenir le chemin absolu
            string absolutePath = Path.GetFullPath(logsPath);

            // Ouvrir l'explorateur de fichiers
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = absolutePath,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", absolutePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", absolutePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'ouverture du dossier: {ex.Message}");
        }
    }

    private void LoadLogs()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();
            string logFormat = _configParser.GetLogFormat();

            Console.WriteLine($"[LogsPage] LoadLogs() - logsPath: {logsPath}");
            Console.WriteLine($"[LogsPage] LoadLogs() - logFormat: {logFormat}");

            // Créer le dossier des logs s'il n'existe pas
            if (!Directory.Exists(logsPath))
            {
                Console.WriteLine($"[LogsPage] Le dossier de logs n'existe pas: {logsPath}");
                Directory.CreateDirectory(logsPath);
                return;
            }

            // Obtenir le nom du fichier de logs du jour
            string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.{logFormat}";
            _currentLogFilePath = Path.Combine(logsPath, todayLogFileName);

            Console.WriteLine($"[LogsPage] Chemin du fichier de logs: {_currentLogFilePath}");
            Console.WriteLine($"[LogsPage] Le fichier existe: {File.Exists(_currentLogFilePath)}");

            if (!File.Exists(_currentLogFilePath))
            {
                Console.WriteLine($"[LogsPage] Le fichier de logs n'existe pas encore");
                return;
            }

            // Lire et parser les logs selon le format
            if (logFormat == "json")
            {
                LoadJsonLogs(_currentLogFilePath);
            }
            else if (logFormat == "xml")
            {
                LoadXmlLogs(_currentLogFilePath);
            }

            UpdateLogsCount();
            Console.WriteLine($"[LogsPage] Nombre de logs chargés: {_logs.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogsPage] Erreur lors du chargement des logs: {ex.Message}");
            Console.WriteLine($"[LogsPage] StackTrace: {ex.StackTrace}");
        }
    }

    private void LoadJsonLogs(string filePath)
    {
        try
        {
            Console.WriteLine($"[LogsPage] LoadJsonLogs() - Lecture du fichier: {filePath}");

            // IMPORTANT: Utiliser FileShare.ReadWrite car le fichier est ouvert en écriture par EasyLog
            string jsonContent;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                jsonContent = reader.ReadToEnd();
            }

            Console.WriteLine($"[LogsPage] Contenu JSON lu, longueur: {jsonContent.Length}");

            // Vérifier si le contenu est vide
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Console.WriteLine($"[LogsPage] Le contenu JSON est vide");
                return;
            }

            // Diviser les logs individuellement
            Console.WriteLine($"[LogsPage] Division des logs");
            _logs.Clear();

            // Chercher toutes les entrées de log (entre {"timestamp": et })
            var matches = System.Text.RegularExpressions.Regex.Matches(
                jsonContent,
                @"\{""timestamp"":""[^""]+""[^}]*\}",
                System.Text.RegularExpressions.RegexOptions.None);

            Console.WriteLine($"[LogsPage] Nombre de logs trouvés: {matches.Count}");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string logEntry = match.Value;
                _logs.Add(new SimpleLogEntry
                {
                    LogText = logEntry
                });
            }

            Console.WriteLine($"[LogsPage] Total de logs ajoutés: {_logs.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogsPage] Erreur lors de la lecture du fichier JSON: {ex.Message}");
            Console.WriteLine($"[LogsPage] StackTrace: {ex.StackTrace}");
        }
    }

    private void LoadXmlLogs(string filePath)
    {
        try
        {
            // TODO: Implémenter la lecture des logs XML si nécessaire
            // Pour l'instant, on se concentre sur JSON
            Console.WriteLine("La lecture des logs XML n'est pas encore implémentée");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la lecture du fichier XML: {ex.Message}");
        }
    }

    private void StartFileWatcher()
    {
        try
        {
            string logsPath = _configParser.GetLogsPath();

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            // Configurer le FileSystemWatcher
            _fileWatcher = new FileSystemWatcher(logsPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*.json", // Surveiller principalement les fichiers JSON
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnLogFileChanged;
            _fileWatcher.Created += OnLogFileChanged;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la configuration du FileSystemWatcher: {ex.Message}");
        }
    }

    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        // Vérifier si c'est le fichier de logs du jour
        string todayLogFileName = $"{DateTime.Now:yyyy-MM-dd}_logs.json";

        if (Path.GetFileName(e.FullPath) == todayLogFileName)
        {
            // Exécuter sur le thread UI
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Attendre un peu pour s'assurer que le fichier est complètement écrit
                System.Threading.Thread.Sleep(100);
                LoadLogs();
            });
        }
    }

    private void UpdateLogsCount()
    {
        var logsCountText = this.FindControl<TextBlock>("LogsCountText");
        if (logsCountText != null)
        {
            logsCountText.Text = _logs.Count.ToString();
        }

        Console.WriteLine($"[LogsPage] UpdateLogsCount - Total: {_logs.Count}");
    }

    // Nettoyer le FileSystemWatcher à la destruction
    ~LogsPage()
    {
        _fileWatcher?.Dispose();
    }
}
