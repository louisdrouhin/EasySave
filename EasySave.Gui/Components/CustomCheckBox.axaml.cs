using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;

namespace EasySave.GUI.Components;

public partial class CustomCheckBox : UserControl
{
    private bool _isChecked = false;

    public event EventHandler<bool>? CheckedChanged;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetChecked(value);
    }

    public CustomCheckBox()
    {
        InitializeComponent();

        var checkButton = this.FindControl<Button>("CheckButton");
        if (checkButton != null)
        {
            checkButton.Click += (s, e) => Toggle();
        }
    }

    public void Toggle()
    {
        SetChecked(!_isChecked);
    }

    public void SetChecked(bool value)
    {
        _isChecked = value;
        UpdateVisuals();
        CheckedChanged?.Invoke(this, value);
    }

    private void UpdateVisuals()
    {
        var checkBoxBorder = this.FindControl<Border>("CheckBox");
        var checkMark = this.FindControl<Path>("CheckMark");

        if (checkBoxBorder != null)
        {
            if (_isChecked)
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
            checkMark.IsVisible = _isChecked;
        }
    }
}
