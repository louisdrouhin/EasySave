using Avalonia.Controls;
using EasySave.Models;

namespace EasySave.GUI.Components;

public partial class JobCard : UserControl
{
    public JobCard()
    {
        InitializeComponent();
    }

    public JobCard(Job job) : this()
    {
        DataContext = job;
    }
}
