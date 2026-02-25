using Avalonia.Controls;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI;

// Main application window
// Creates the MainWindowViewModel and configures the DataContext for bindings
public partial class MainWindow : Window
{
    // Initializes the main window
    // Creates the ViewModel and establishes MVVM communication
    public MainWindow()
    {
        InitializeComponent();

        // Creates and configures the DataContext for MVVM bindings
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // Cleans up resources when the window closes
        Closed += (_, _) => viewModel.Dispose();
    }
}
