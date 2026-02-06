using System;
using EasySave.Core;
using EasySave.Core.Localization;
using EasySave.Models;

namespace EasySave.Cli
{
    public class CLI
    {
        private JobManager _jobManager;

        public CLI()
        {
            _jobManager = new JobManager();
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void start()
        {
            int indexSelectionne = 0;
            bool continuer = true;

            while (continuer)
            {
                string[] options = {
                    LocalizationManager.Get("Menu_CreateJob"),
                    LocalizationManager.Get("Menu_ExecuteJobs"),
                    LocalizationManager.Get("Menu_DeleteJob"),
                    LocalizationManager.Get("Menu_ShowJobs"),
                    LocalizationManager.Get("Menu_ChangeLanguage"),
                    LocalizationManager.Get("Menu_Quit")
                };

                Console.Clear();
                Console.WriteLine($"=== {LocalizationManager.Get("Menu_Title")} ===\n");

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == indexSelectionne)
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
                    indexSelectionne = (indexSelectionne == 0) ? options.Length - 1 : indexSelectionne - 1;
                }
                else if (touche == ConsoleKey.DownArrow)
                {
                    indexSelectionne = (indexSelectionne == options.Length - 1) ? 0 : indexSelectionne + 1;
                }
                else if (touche == ConsoleKey.Enter)
                {
                    Console.Clear();
                    switch (indexSelectionne)
                    {
                        case 0:
                            CreateJob();
                            break;
                        case 1:
                            ExecuteJobs();
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
                            continuer = false;
                            _jobManager.Close();
                            Console.WriteLine(LocalizationManager.Get("Common_Closing"));
                            continue;
                    }

                    if (continuer)
                    {
                        Console.WriteLine($"\n{LocalizationManager.Get("Common_PressKey")}");
                        Console.ReadKey(true);
                    }
                }
            }
        }

        private void CreateJob()
        {
            Console.WriteLine(LocalizationManager.Get("CreateJob_Title"));
            Console.WriteLine();

            // Afficher les jobs existants
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

        private void DeleteJob()
        {
            Console.WriteLine(LocalizationManager.Get("DeleteJob_Title"));
            Console.WriteLine();

            // Afficher les jobs existants
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
            string nom = Console.ReadLine() ?? "";

            _jobManager.removeJob(nom);

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("DeleteJob_Success", nom));
        }

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

        private void ChangeLanguage()
        {
            Console.WriteLine(LocalizationManager.Get("Language_Title"));
            Console.WriteLine();

            var langues = LocalizationManager.GetAvailableLanguages();
            Console.WriteLine("1. Français (fr)");
            Console.WriteLine("2. English (en)");
            Console.WriteLine();

            Console.Write(LocalizationManager.Get("Language_Selection"));
            string choix = Console.ReadLine() ?? "1";

            string nouvelleLangue = choix == "2" ? "en" : "fr";
            LocalizationManager.SetLanguage(nouvelleLangue);

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("Language_Changed", nouvelleLangue));
        }

        private void ExecuteJobs()
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

                    foreach (var job in jobsToExecute)
                    {
                        try
                        {
                            Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_ExecutingJob", job.Name));
                            _jobManager.LaunchJob(job);
                            Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_Success", job.Name));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJobs_Error", job.Name, ex.Message));
                        }
                        Console.WriteLine();
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
