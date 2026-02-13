using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EasySave.Core.Localization;
using System.Threading.Tasks;

namespace EasySave.GUI.Dialogs;

public partial class PasswordDialog : Window
{
    private string _password = "";

    public PasswordDialog()
    {
        InitializeComponent();

        Title = LocalizationManager.Get("PasswordDialog_Title");

        var labelBlock = this.FindControl<TextBlock>("LabelBlock");
        if (labelBlock != null) labelBlock.Text = LocalizationManager.Get("PasswordDialog_Label");

        var passwordInput = this.FindControl<TextBox>("PasswordInput");
        if (passwordInput != null) passwordInput.Watermark = LocalizationManager.Get("PasswordDialog_Placeholder");

        var okButton = this.FindControl<Button>("OkButton");
        if (okButton != null) okButton.Content = LocalizationManager.Get("ErrorDialog_Button_OK"); 

        var cancelButton = this.FindControl<Button>("CancelButton");
        if (cancelButton != null) cancelButton.Content = LocalizationManager.Get("CreateJobDialog_Button_Cancel"); 
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var okButton = this.FindControl<Button>("OkButton");
        if (okButton != null)
        {
            okButton.Click += OkButton_Click;
        }

        var cancelButton = this.FindControl<Button>("CancelButton");
        if (cancelButton != null)
        {
            cancelButton.Click += CancelButton_Click;
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var passwordInput = this.FindControl<TextBox>("PasswordInput");
        if (passwordInput != null)
        {
            _password = passwordInput.Text ?? "";
        }
        Close(_password);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
