using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Models;

// Interface en ligne de commande pour EasySave
// Permet à l'utilisateur de créer, exécuter, supprimer et afficher des jobs de sauvegarde
namespace EasySave.Cli
{
    public class CLI
    {
        private JobManager _jobManager;

        // Initialise le CLI et le JobManager
        public CLI()
        {
            _jobManager = new JobManager();
        }

        // Affiche un message dans la console
        // @param message - texte à afficher
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        // Démarre le menu principal du CLI et gère les interactions de l'utilisateur
        // Affiche les options du menu, permet de naviguer avec les flèches, et d'exécuter les actions correspondantes
        public void start()
        {
            int selectedIndex = 0;
            bool isFinished = false;

            while (!isFinished)
            {
                string[] options = {
                    LocalizationManager.Get("Menu_CreateJob"),
                    LocalizationManager.Get("Menu_ExecuteJobs"),
                    LocalizationManager.Get("Menu_DeleteJob"),
                    LocalizationManager.Get("Menu_ShowJobs"),
                    LocalizationManager.Get("Menu_ChangeLanguage"),
                    LocalizationManager.Get("Menu_ChangeLogFormat"),
                    LocalizationManager.Get("Menu_Quit")
                };

                Console.Clear();
                Console.WriteLine($"=== {LocalizationManager.Get("Menu_Title")} ===\n");

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"> {options[i]} ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  {options[i]} ");
                    }
                }

                ConsoleKeyInfo toucheInfo = Console.ReadKey(true);
                ConsoleKey touche = toucheInfo.Key;

                if (touche == ConsoleKey.UpArrow)
                {
                    selectedIndex = (selectedIndex == 0) ? options.Length - 1 : selectedIndex - 1;
                }
                else if (touche == ConsoleKey.DownArrow)
                {
                    selectedIndex = (selectedIndex == options.Length - 1) ? 0 : selectedIndex + 1;
                }
                else if (touche == ConsoleKey.Enter)
                {
                    Console.Clear();
                    switch (selectedIndex)
                    {
                        case 0:
                            CreateJob();
                            break;
                        case 1:
                            ExecuteJobsAsync().Wait();
                            break;
                        case 2:
                            DeleteJob();
                            break;
                        case 3:
                            ShowJobs();
                            break;
                        case 4:
                            ChangeLanguage();
                            break;
                        case 5:
                            ChangeLogFormat();
                            break;
                        case 6:
                            isFinished = true;
                            _jobManager.Close();
                            Console.WriteLine(LocalizationManager.Get("Common_Closing"));
                            continue;
                    }

                    if (!isFinished)
                    {
                        Console.WriteLine($"\n{LocalizationManager.Get("Common_PressKey")}");
                        Console.ReadKey(true);
                    }
                }
            }
        }

        // Permet à l'utilisateur de créer un nouveau job de sauvegarde
        // Affiche les jobs existants, puis demande à l'utilisateur de saisir les détails du nouveau job (nom, source, destination, type)
        private void CreateJob()
        {
            Console.WriteLine(LocalizationManager.Get("CreateJob_Title"));
            Console.WriteLine();

            var existingJobs = _jobManager.GetJobs();
            if (existingJobs.Count > 0)
            {
                Console.WriteLine(LocalizationManager.Get("CreateJob_ExistingJobs"));
                for (int i = 0; i < existingJobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {existingJobs[i]}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(LocalizationManager.Get("CreateJob_NoExistingJobs"));
                Console.WriteLine();
            }

            Console.Write(LocalizationManager.Get("CreateJob_JobName"));
            string nom = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_SourcePath"));
            string source = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_DestinationPath"));
            string destination = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_BackupType"));
            string typeInput = Console.ReadLine() ?? "1";

            JobType jobType = typeInput == "2" ? JobType.Differential : JobType.Full;
            string typeDisplay = typeInput == "2"
                ? LocalizationManager.Get("BackupType_Differential")
                : LocalizationManager.Get("BackupType_Full");

            _jobManager.CreateJob(nom, jobType, source, destination);

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Success", nom));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Source", source));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Destination", destination));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Type", typeDisplay));
        }

        // Permet à l'utilisateur de supprimer un job de sauvegarde existant
        // Affiche les jobs existants, puis demande à l'utilisateur de sélectionner celui à supprimer
        private void DeleteJob()
        {
            Console.WriteLine(LocalizationManager.Get("DeleteJob_Title"));
            Console.WriteLine();

            var existingJobs = _jobManager.GetJobs();
            if (existingJobs.Count > 0)
            {
                Console.WriteLine(LocalizationManager.Get("DeleteJob_ExistingJobs"));
                for (int i = 0; i < existingJobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {existingJobs[i]}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(LocalizationManager.Get("DeleteJob_NoJobsToDelete"));
                Console.WriteLine();
                return;
            }

            Console.Write(LocalizationManager.Get("DeleteJob_JobName"));
            string index = Console.ReadLine() ?? "";

            _jobManager.removeJob(int.Parse(index) - 1);

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("DeleteJob_Success", index));
        }

        // Affiche la liste des jobs de sauvegarde existants
        // Si aucun job n'existe, affiche un message indiquant qu'il n'y a pas de jobs et des instructions pour en créer un
        // Sinon, affiche chaque job avec son numéro d'index
        private void ShowJobs()
        {
            Console.WriteLine(LocalizationManager.Get("ShowJobs_Title"));
            Console.WriteLine();

            var jobs = _jobManager.GetJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(LocalizationManager.Get("ShowJobs_NoJobs"));
                Console.WriteLine();
                Console.WriteLine(LocalizationManager.Get("ShowJobs_Instructions"));
            }
            else
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {jobs[i]}");
                }
            }
        }

        // Permet à l'utilisateur de changer la langue de l'interface
        // Affiche les langues disponibles, puis demande à l'utilisateur de sélectionner une langue
        private void ChangeLanguage()
        {
            Console.WriteLine(LocalizationManager.Get("Language_Title"));
            Console.WriteLine();

            var langues = LocalizationManager.GetAvailableLanguages();
            Console.WriteLine("1. Français (fr)");
            Console.WriteLine("2. English (en)");
            Console.WriteLine();

            Console.Write(LocalizationManager.Get("Language_Selection"));
            string choice = Console.ReadLine() ?? "1";

            string newLanguage = choice == "2" ? "en" : "fr";
            LocalizationManager.SetLanguage(newLanguage);

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("Language_Changed", newLanguage));
        }

        // Permet à l'utilisateur de changer le format de journalisation des sauvegardes
        // Affiche le format actuel, puis propose à l'utilisateur de choisir entre JSON et XML
        private void ChangeLogFormat()
        {
            Console.WriteLine(LocalizationManager.Get("LogFormat_Title"));
            Console.WriteLine();

            string currentFormat = _jobManager.GetLogFormat();
            Console.WriteLine(LocalizationManager.GetFormatted("LogFormat_CurrentFormat", currentFormat.ToUpper()));
            Console.WriteLine();

            Console.WriteLine(LocalizationManager.Get("LogFormat_SelectFormat"));
            Console.WriteLine("1. " + LocalizationManager.Get("LogFormat_JSON"));
            Console.WriteLine("2. " + LocalizationManager.Get("LogFormat_XML"));
            Console.WriteLine();

            Console.Write("> ");
            string choice = Console.ReadLine() ?? "1";

            string newFormat = choice == "2" ? "xml" : "json";

            try
            {
                _jobManager.SetLogFormat(newFormat);
                Console.WriteLine();
                Console.WriteLine(LocalizationManager.GetFormatted("LogFormat_Changed", newFormat.ToUpper()));
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(LocalizationManager.Get("LogFormat_InvalidFormat"));
            }
        }

        // Permet à l'utilisateur d'exécuter un ou plusieurs jobs de sauvegarde
        // Affiche les jobs disponibles, puis demande à l'utilisateur de sélectionner ceux à exécuter (en entrant les numéros ou "all")
        private async Task ExecuteJobsAsync()
        {
            Console.WriteLine(LocalizationManager.Get("ExecuteJobs_Title"));
            Console.WriteLine();

            var jobs = _jobManager.GetJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(LocalizationManager.Get("ExecuteJobs_NoJobs"));
                Console.WriteLine();
                Console.WriteLine(LocalizationManager.Get("ExecuteJobs_Instructions"));
            }
            else
            {
                Console.WriteLine(LocalizationManager.Get("ExecuteJobs_AvailableJobs"));
                for (int i = 0; i < jobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {jobs[i]}");
                }
                Console.WriteLine();

                Console.Write(LocalizationManager.Get("ExecuteJobs_SelectPrompt"));
                string input = Console.ReadLine() ?? "";

                List<Job> jobsToExecute = new List<Job>();

                if (input.ToLower().Trim() == "all")
                {
                    jobsToExecute = jobs;
                }
                else
                {
                    string[] numbers = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string numStr in numbers)
                    {
                        if (int.TryParse(numStr.Trim(), out int jobNumber))
                        {
                            if (jobNumber > 0 && jobNumber <= jobs.Count)
                            {
                                jobsToExecute.Add(jobs[jobNumber - 1]);
                            }
                            else
                            {
                                Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_InvalidNumber", jobNumber));
                            }
                        }
                    }
                }

                Console.WriteLine();

                if (jobsToExecute.Count > 0)
                {
                    Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_Executing", jobsToExecute.Count));
                    Console.WriteLine();

                    string password = string.Empty;
                    bool validPassword = false;

                    while (!validPassword)
                    {
                        Console.Write(LocalizationManager.Get("ExecuteJobs_PasswordPrompt"));
                        password = Console.ReadLine() ?? string.Empty;
                        Console.WriteLine();

                        if (string.IsNullOrEmpty(password))
                        {
                            Console.WriteLine(LocalizationManager.Get("ExecuteJobs_PasswordEmpty"));
                            Console.WriteLine();
                            continue;
                        }

                        if (password.Contains(" "))
                        {
                            Console.WriteLine(LocalizationManager.Get("ExecuteJobs_PasswordWithSpaces"));
                            Console.WriteLine();
                            continue;
                        }

                        validPassword = true;
                    }

                    try
                    {
                        Console.WriteLine(LocalizationManager.Get("ExecuteJobs_ConcurrentExecution"));
                        Console.WriteLine();

                        await _jobManager.LaunchMultipleJobsAsync(jobsToExecute, password);

                        Console.WriteLine();
                        Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_AllCompleted", jobsToExecute.Count));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_Error", "jobs", ex.Message));
                    }
                }
                else
                {
                    Console.WriteLine(LocalizationManager.Get("ExecuteJobs_NoSelection"));
                }
            }
        }
    }
}
