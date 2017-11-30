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

            MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
