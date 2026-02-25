using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel pour une application métier dans la liste
// Représente une application à surveiller (ex: CryptoSoft)
public class AppItemViewModel : ViewModelBase
{
    // Texte à afficher pour l'application (ex: "CryptoSoft")
    public string DisplayText { get; }

    // Commande pour supprimer cette application de la liste
    public ICommand RemoveCommand { get; }

    // Crée un ViewModel pour une application métier
    // @param appName - nom de l'application à afficher
    // @param removeCallback - callback appelée quand l'utilisateur supprime l'application
    public AppItemViewModel(string appName, Action<AppItemViewModel> removeCallback)
    {
        DisplayText = appName;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
