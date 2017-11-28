using System;
using System.Windows.Input;

namespace Rove.ViewModel
{
    public class LambdaCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action Action { get; }

        public LambdaCommand(Action action)
        {
            Action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Action();
        }
    }
}
