using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Models;

// Command-line interface for EasySave
// Allows users to create, execute, delete, and display backup jobs
namespace EasySave.Cli
{
    public class CLI
    {
        private JobManager _jobManager;

        // Initializes the CLI and JobManager
        public CLI()
        {
            _jobManager = new JobManager();
        }

        // Displays a message in the console
        // @param message - text to display
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        // Starts the main CLI menu and handles user interactions
        // Displays menu options, allows navigation with arrow keys, and executes corresponding actions
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

        // Allows users to create a new backup job
        // Displays existing jobs, then prompts the user to enter details for the new job (name, source, destination, type)
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

        // Allows users to delete an existing backup job
        // Displays existing jobs, then prompts the user to select the one to delete
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

        // Displays the list of existing backup jobs
        // If no jobs exist, displays a message indicating there are no jobs and instructions to create one
        // Otherwise, displays each job with its index number
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

        // Allows users to change the interface language
        // Displays available languages, then prompts the user to select a language
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

        // Allows users to change the log format for backups
        // Displays the current format, then offers the user to choose between JSON and XML
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

        // Allows users to execute one or more backup jobs
        // Displays available jobs, then prompts the user to select which ones to execute (by entering numbers or "all")
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
