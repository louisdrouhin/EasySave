using Avalonia.Controls;
using EasySave.Models;

namespace EasySave.GUI.Components;

public partial class StateCard : UserControl
{
    public StateCard()
    {
        InitializeComponent();
    }

    public StateCard(StateEntry entry) : this()
    {
        DataContext = entry;
    }
}
