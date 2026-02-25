using Avalonia.Controls;

namespace EasySave.GUI.Components;

// Composant réutilisable pour afficher un job
// Affiche le nom, état, progression, et boutons d'action (play/pause/stop/delete)
public partial class JobCard : UserControl
{
    // Initialise le composant JobCard
    // Code-behind minimal : la logique est entièrement dans le ViewModel (MVVM)
    public JobCard()
    {
        InitializeComponent();
    }
}
