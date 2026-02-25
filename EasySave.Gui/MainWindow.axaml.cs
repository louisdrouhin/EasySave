using Avalonia.Controls;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI;

// Fenêtre principale de l'application
// Crée le MainWindowViewModel et configure le DataContext pour les bindings
public partial class MainWindow : Window
{
    // Initialise la fenêtre principale
    // Crée le ViewModel et établit la communication MVVM
    public MainWindow()
    {
        InitializeComponent();

        // Crée et configure le DataContext pour les bindings MVVM
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // Nettoie les ressources à la fermeture de la fenêtre
        Closed += (_, _) => viewModel.Dispose();
    }
}
