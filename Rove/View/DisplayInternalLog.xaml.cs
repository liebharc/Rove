using Rove.Model;
using System;
using System.Windows;

namespace Rove.View
{
    public partial class DisplayInternalLog : IDisposable
    {
        private FastColoredTextBoxNS.FastColoredTextBox LogViewer { get; set; }

        public DisplayInternalLog()
        {
            InitializeComponent();

            LogViewer = Log.Child as FastColoredTextBoxNS.FastColoredTextBox;

            foreach (var line in Logger.RecentMessages)
            {
                LogViewer.Text += line + "\n";
            }

            Logger.NewMessage += Logger_NewMessage;
        }

        private void Logger_NewMessage(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { LogViewer.Text += message + "\n"; }));
        }

        public void Dispose()
        {
            Logger.NewMessage -= Logger_NewMessage;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }
    }
}
