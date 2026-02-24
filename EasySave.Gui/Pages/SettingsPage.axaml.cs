using System;
using Avalonia.Controls;
using EasySave.Core;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // DataContext is set by the MainWindowViewModel or parent
        // No additional setup needed - all bindings work automatically
    }
}
