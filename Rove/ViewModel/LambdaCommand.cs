using System;
using System.Windows.Input;

namespace Rove.ViewModel
{
    public class LambdaCommand : ICommand
    {
#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

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
