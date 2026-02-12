using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EasySave.GUI.Dialogs;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;

        var titleBlock = this.FindControl<TextBlock>("TitleBlock");
        if (titleBlock != null)
        {
            titleBlock.Text = title;
        }

        var messageBlock = this.FindControl<TextBlock>("MessageBlock");
        if (messageBlock != null)
        {
            messageBlock.Text = message;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var okButton = this.FindControl<Button>("OkButton");
        if (okButton != null)
        {
            okButton.Click += OkButton_Click;
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
