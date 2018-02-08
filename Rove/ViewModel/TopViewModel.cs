using Rove.View;
using System.Windows.Input;

namespace Rove.ViewModel
{
    public class TopViewModel
    {
        public ICommand Help { get; }

        public TopViewModel()
        {
            Help = new LambdaCommand(() => 
            {
                var logWindow = new DisplayInternalLog();
                logWindow.Show();
            });
        }
    }
}
