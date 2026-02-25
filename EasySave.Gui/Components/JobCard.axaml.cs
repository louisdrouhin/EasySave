using Avalonia.Controls;

namespace EasySave.GUI.Components;

// Reusable component to display a job
// Displays name, state, progress, and action buttons (play/pause/stop/delete)
public partial class JobCard : UserControl
{
    // Initializes the JobCard component
    // Minimal code-behind: logic is entirely in the ViewModel (MVVM)
    public JobCard()
    {
        InitializeComponent();
    }
}
