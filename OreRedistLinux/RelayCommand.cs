using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// WPF re-queried CanExecute on its own (CommandManager.RequerySuggested). Avalonia does not.
    /// Commands here register weakly and are re-queried after a command ran and after
    /// the user clicked/typed (see DialogWindow). Not on property changes: some CanExecute
    /// handlers (ConfirmCommand) change properties themselves and would loop forever.
    /// </summary>
    public class RelayCommand : ICommand
    {
        static readonly List<WeakReference<RelayCommand>> _all = new();
        static bool _requeryPending;

        Action<object> _execute;
        Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            if (canExecute != null)
            {
                lock (_all)
                {
                    if (_all.Count > 512) _all.RemoveAll(w => !w.TryGetTarget(out _));
                    _all.Add(new WeakReference<RelayCommand>(this));
                }
            }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter)
        {
            _execute?.Invoke(parameter);
            RequestRequery(); // a command usually changes what other commands may do
        }

        public static void RequestRequery()
        {
            lock (_all)
            {
                if (_requeryPending) return;
                _requeryPending = true;
            }
            Dispatcher.UIThread.Post(() =>
            {
                List<RelayCommand> commands = new();
                lock (_all)
                {
                    _requeryPending = false;
                    _all.RemoveAll(w => !w.TryGetTarget(out _));
                    foreach (var w in _all)
                        if (w.TryGetTarget(out var c)) commands.Add(c);
                }
                foreach (var c in commands)
                    c.CanExecuteChanged?.Invoke(c, EventArgs.Empty);
            }, DispatcherPriority.Background);
        }
    }
}
