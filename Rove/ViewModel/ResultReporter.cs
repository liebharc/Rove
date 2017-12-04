using Rove.Model;
using System.Windows;

namespace Rove.ViewModel
{
    public static class ResultReporter
    {
        public static void Report(this Result result)
        {
            if (result.IsOk)
            {
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => Report(result));
                return;
            }

            MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
