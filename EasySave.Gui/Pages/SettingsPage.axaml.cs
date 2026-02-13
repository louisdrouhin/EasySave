using Avalonia.Controls;
using EasySave.Core.Localization;

namespace EasySave.GUI.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();

        var titleText = this.FindControl<TextBlock>("TitleText");
        if (titleText != null) titleText.Text = LocalizationManager.Get("SettingsPage_Title");
    }
}
