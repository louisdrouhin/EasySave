using System;
using System.Windows.Input;

namespace EasySave.Gui.Commands;

// Simple implementation of an ICommand for MVVM bindings
// Encapsulates an action and an optional execution condition
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    // Creates a RelayCommand
    // @param execute - action to execute when the command is called
    // @param canExecute - optional predicate to determine if the command can execute
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    // Checks if the command can execute
    // @param parameter - parameter passed to the command
    // @returns true if the command can execute
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

    // Executes the command
    // @param parameter - parameter passed to the command
    public void Execute(object? parameter) => _execute(parameter);

    // Raises CanExecuteChanged event to refresh button state
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
