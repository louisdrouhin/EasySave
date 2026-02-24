using Avalonia.Controls;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Create and set the DataContext to MainWindowViewModel
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // Clean up resources when window closes
        Closed += (_, _) => viewModel.Dispose();
    }
}
