using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using EasySave.Core;
using EasySave.Gui.ViewModels;

namespace EasySave.GUI.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        this.Loaded += (s, e) => ApplyStyles();
    }

    private void ApplyStyles()
    {
        // Style the custom numeric TextBox
        if (this.FindControl<TextBox>("LargeFileSizeTextBox") is TextBox textBox)
        {
            textBox.Background = Brushes.White;
            textBox.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            textBox.CaretBrush = Brushes.Transparent;
            textBox.BorderThickness = new Thickness(0);
        }

        // Style the custom numeric buttons
        if (this.FindControl<Button>("IncrementBtn") is Button incrementBtn)
        {
            incrementBtn.Background = Brushes.White;
            incrementBtn.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            incrementBtn.BorderThickness = new Thickness(0);
        }

        if (this.FindControl<Button>("DecrementBtn") is Button decrementBtn)
        {
            decrementBtn.Background = Brushes.White;
            decrementBtn.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            decrementBtn.BorderThickness = new Thickness(0);
        }

        // Find all NumericUpDown controls with input-field class (for any remaining)
        FindAndStyleNumericUpDowns(this);
    }

    private void FindAndStyleNumericUpDowns(Control parent)
    {
        if (parent is NumericUpDown numericUpDown && numericUpDown.Classes.Contains("input-field"))
        {
            StyleNumericUpDown(numericUpDown);
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control control)
                {
                    FindAndStyleNumericUpDowns(control);
                }
            }
        }
        else if (parent is ItemsControl itemsControl)
        {
            foreach (var child in itemsControl.ItemsSource as System.Collections.IEnumerable ?? new object[0])
            {
                if (child is Control control)
                {
                    FindAndStyleNumericUpDowns(control);
                }
            }
        }
    }

    private void StyleNumericUpDown(NumericUpDown numericUpDown)
    {
        numericUpDown.ApplyTemplate();

        // Style the TextBox inside
        var textBox = numericUpDown.FindControl<TextBox>("PART_TextBox");
        if (textBox != null)
        {
            textBox.Background = Brushes.White;
            textBox.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            textBox.CaretBrush = Brushes.Transparent;
            textBox.BorderThickness = new Thickness(0);
        }

        // Style the RepeatButtons
        var incrementBtn = numericUpDown.FindControl<RepeatButton>("PART_IncrementButton");
        if (incrementBtn != null)
        {
            incrementBtn.Background = Brushes.White;
            incrementBtn.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            incrementBtn.BorderThickness = new Thickness(0);
        }

        var decrementBtn = numericUpDown.FindControl<RepeatButton>("PART_DecrementButton");
        if (decrementBtn != null)
        {
            decrementBtn.Background = Brushes.White;
            decrementBtn.Foreground = new SolidColorBrush(Color.Parse("#000000"));
            decrementBtn.BorderThickness = new Thickness(0);
        }
    }

    private void OnIncrementLargeFileSize(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is SettingsPageViewModel vm && long.TryParse(vm.LargeFileSizeLimitKb.ToString(), out long value))
        {
            vm.LargeFileSizeLimitKb = value + 1;
        }
    }

    private void OnDecrementLargeFileSize(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is SettingsPageViewModel vm && long.TryParse(vm.LargeFileSizeLimitKb.ToString(), out long value))
        {
            vm.LargeFileSizeLimitKb = value - 1;
        }
    }

    private void OnLargeFileSizePointerEntered(object? sender, PointerEventArgs e)
    {
        if (this.FindControl<Border>("LargeFileSizeBorder") is Border border)
        {
            border.BorderBrush = new SolidColorBrush(Color.Parse("#F97316"));
        }
    }

    private void OnLargeFileSizePointerExited(object? sender, PointerEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox?.IsFocused != true)
        {
            if (this.FindControl<Border>("LargeFileSizeBorder") is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
            }
        }
    }

    private void OnLargeFileSizeGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (this.FindControl<Border>("LargeFileSizeBorder") is Border border)
        {
            border.BorderBrush = new SolidColorBrush(Color.Parse("#EA580C"));
        }
    }

    private void OnLargeFileSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Border>("LargeFileSizeBorder") is Border border)
        {
            border.BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Blur the current focused control
            if (sender is Control control)
            {
                var focusedControl = TopLevel.GetTopLevel(control)?.FocusManager?.GetFocusedElement();
                if (focusedControl is Control fc)
                {
                    fc.Focus(NavigationMethod.Pointer);
                }
            }
            // Focus on the page to blur any text input
            this.Focus(NavigationMethod.Pointer);
            e.Handled = true;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // DataContext is set by the MainWindowViewModel or parent
        // No additional setup needed - all bindings work automatically
    }
}
