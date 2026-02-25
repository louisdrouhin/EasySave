using System;
using System.Windows.Input;
using EasySave.Gui.Commands;

namespace EasySave.Gui.ViewModels;

// ViewModel pour un élément d'extension dans les listes
// Représente une extension de fichier (ex: .docx, .xlsx)
public class ExtensionItemViewModel : ViewModelBase
{
    // Texte à afficher pour l'extension (ex: ".docx")
    public string DisplayText { get; }

    // Commande pour supprimer cette extension de la liste
    public ICommand RemoveCommand { get; }

    // Crée un ViewModel pour une extension
    // @param ext - extension à afficher
    // @param removeCallback - callback appelée quand l'utilisateur supprime l'extension
    public ExtensionItemViewModel(string ext, Action<ExtensionItemViewModel> removeCallback)
    {
        DisplayText = ext;
        RemoveCommand = new RelayCommand(_ => removeCallback(this));
    }
}
