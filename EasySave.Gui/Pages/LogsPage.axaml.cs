using System;
using Avalonia.Controls;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI.Pages;

public partial class LogsPage : UserControl
{
    public LogsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Subscribe to LogAdded event for auto-scrolling
        if (DataContext is LogsPageViewModel vm)
        {
            vm.LogAdded += (s, e) =>
            {
                var listBox = this.FindControl<ListBox>("LogsListBox");
                if (listBox != null && listBox.ItemCount > 0)
                {
                    listBox.ScrollIntoView(listBox.ItemCount - 1);
                }
            };

            // Initial scroll when loaded
            this.Loaded += (s, e) =>
            {
                var listBox = this.FindControl<ListBox>("LogsListBox");
                if (listBox != null && listBox.ItemCount > 0)
                {
                    listBox.ScrollIntoView(listBox.ItemCount - 1);
                }
            };
        }
    }
}
