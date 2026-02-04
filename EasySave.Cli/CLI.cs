using System;
using EasySave.Core.Localization;

namespace EasySave.Cli
{
    public class CLI
    {
        public CLI()
        {
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Demarrer()
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
                        case 0: // Créer un travail
                            CreerTravail();
                            break;
                        case 1: // Exécuter
                            ExecuterTravail();
                            break;
                        case 2: // Supprimer
                            SupprimerTravail();
                            break;
                        case 3: // Afficher
                            AfficherTravaux();
                            break;
                        case 4: // Changer langue
                            ChangerLangue();
                            break;
                        case 5: // Quitter
                            continuer = false;
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

        private void CreerTravail()
        {
            Console.WriteLine(LocalizationManager.Get("CreateJob_Title"));
            Console.WriteLine();

            Console.Write(LocalizationManager.Get("CreateJob_JobName"));
            string nom = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_SourcePath"));
            string source = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_DestinationPath"));
            string destination = Console.ReadLine() ?? "";

            Console.Write(LocalizationManager.Get("CreateJob_BackupType"));
            string typeInput = Console.ReadLine() ?? "1";
            string type = typeInput == "2"
                ? LocalizationManager.Get("BackupType_Differential")
                : LocalizationManager.Get("BackupType_Full");

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Success", nom));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Source", source));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Destination", destination));
            Console.WriteLine(LocalizationManager.GetFormatted("CreateJob_Type", type));
        }

        private void ExecuterTravail()
        {
            Console.WriteLine(LocalizationManager.Get("ExecuteJob_Title"));
            Console.WriteLine();

            Console.Write(LocalizationManager.Get("ExecuteJob_Identifier"));
            string identifier = Console.ReadLine() ?? "";

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("ExecuteJob_InProgress", identifier));
            System.Threading.Thread.Sleep(1000); // Simulation
            Console.WriteLine(LocalizationManager.Get("ExecuteJob_Success"));
        }

        private void SupprimerTravail()
        {
            Console.WriteLine(LocalizationManager.Get("DeleteJob_Title"));
            Console.WriteLine();

            Console.Write(LocalizationManager.Get("DeleteJob_JobName"));
            string nom = Console.ReadLine() ?? "";

            Console.WriteLine();
            Console.WriteLine(LocalizationManager.GetFormatted("DeleteJob_Success", nom));
        }

        private void AfficherTravaux()
        {
            Console.WriteLine(LocalizationManager.Get("ShowJobs_Title"));
            Console.WriteLine();

            Console.WriteLine(LocalizationManager.Get("ShowJobs_NoJobs"));
            Console.WriteLine();
            Console.WriteLine(LocalizationManager.Get("ShowJobs_Instructions"));
        }

        private void ChangerLangue()
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
    }
}
