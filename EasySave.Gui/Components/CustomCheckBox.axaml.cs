using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;
using System;

namespace EasySave.GUI.Components;

public partial class CustomCheckBox : UserControl
{
    // Register IsChecked as a StyledProperty for binding support
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<CustomCheckBox, bool>(nameof(IsChecked), false, defaultBindingMode: BindingMode.TwoWay);

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event EventHandler<bool>? CheckedChanged;

    public CustomCheckBox()
    {
        InitializeComponent();

        var checkButton = this.FindControl<Button>("CheckButton");
        if (checkButton != null)
        {
            checkButton.Click += (s, e) => Toggle();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            UpdateVisuals();
            CheckedChanged?.Invoke(this, (bool)change.NewValue!);
        }
    }

    public void Toggle()
    {
        IsChecked = !IsChecked;
    }

    private void UpdateVisuals()
    {
        var checkBoxBorder = this.FindControl<Border>("CheckBox");
        var checkMark = this.FindControl<Path>("CheckMark");

        if (checkBoxBorder != null)
        {
            if (IsChecked)
            {
                checkBoxBorder.Background = new SolidColorBrush(Color.Parse("#F97316"));
                checkBoxBorder.BorderBrush = new SolidColorBrush(Color.Parse("#F97316"));
            }
            else
            {
                checkBoxBorder.Background = Brushes.White;
                checkBoxBorder.BorderBrush = new SolidColorBrush(Color.Parse("#15171D"));
            }
        }

        if (checkMark != null)
        {
            checkMark.IsVisible = IsChecked;
        }
    }
}
