using Rove.ViewModel;
using System.Windows;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using Rove.Model;
using System.IO;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;
using System.Windows.Data;
using System.Globalization;
using System.Threading;

namespace Rove.View
{
    public class IsRunningToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isRunning = value as bool?;
            return isRunning == true ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window
    {
        private static void CreateDefaultConfigFile()
        {
            WriteConfigFile(OverallConfig.DefaultConfig, GetLogBaseName() + "Default.xml");
        }

        private static void WriteConfigFile(OverallConfig config, string file)
        {
            var defaultConfig = ConfigSerializer.ConfigToText(config);
            File.WriteAllText(file, defaultConfig);
        }

        private static string GetLogBaseName()
        {
            return typeof(MainWindow).Assembly.Location.Replace(".exe", "");
        }

        private static string GetLogName()
        {
            return GetLogBaseName() + ".xml";
        }

        private static OverallConfig LoadConfig()
        {
            var file = GetLogName();
            if (!File.Exists(file))
            {
                return OverallConfig.DefaultConfig;
            }

            var content = File.ReadAllText(file);
            return ConfigSerializer.TextToConfig(content);
        }

        private object UpdateThreadLock { get; } = new object();

        private Thread UpdateThread { get; }

        private volatile bool _isTimerActive = true;

        private bool IsDisposed { get; set; } = false;

        private OverallConfig LastConfig { get; }

        public MainWindow()
        {
            InitializeComponent();
            OverallConfigChecked config = null;
            try
            {
                CreateDefaultConfigFile();
                LastConfig = LoadConfig();
                config = LastConfig.ToOverallConfig();
            }
            catch (Exception ex)
            {
                Result.Error(ex.Message).Report();
                Environment.Exit(0);
            }
            TomcatProcessViewModelCollection viewModel = new TomcatProcessViewModelCollection(config);
            CreateAPanelForEachProcess(viewModel.Processes);

            DataContext = viewModel;
            Closed += MainWindow_Closed;

            UpdateThread = new Thread((_) => Update(viewModel));
            UpdateThread.Start();
        }

        private void Update(TomcatProcessViewModelCollection viewModel)
        {
            while (_isTimerActive)
            {
                lock (UpdateThreadLock)
                {
                    viewModel.Update();
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (!IsDisposed)
            {
                lock (UpdateThreadLock)
                {
                    (DataContext as IDisposable)?.Dispose();
                    _isTimerActive = false;
                }

                UpdateThread.Join();
                SaveLayout();
                IsDisposed = true;
            }
        }

        private void CreateAPanelForEachProcess(IList<TomcatProcessViewModel> processes)
        {
            foreach (var process in processes)
            {
                var panel = new ProcessInfo { DataContext = process };
                process.Initialize(panel);
                Root.Children.Add(
                    new LayoutDocument
                    {
                        Title = process.Title,
                        Content = panel,
                        CanClose = false,
                        ContentId = process.Title
                    });
            }

            RestoreLayout();
        }

        private void SaveLayout()
        {
            try
            {
                using (StringWriter writer = new StringWriter())
                {
                    XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(Layout);
                    xmlLayout.Serialize(writer);
                    LastConfig.DisplayLayout = writer.ToString();
                    WriteConfigFile(LastConfig, GetLogName());
                }
            }
            catch (Exception ex)
            {
                Result.Error("Failed to save display configuration: " + ex.Message).Report();
            }
        }

        private void RestoreLayout()
        {
            if (!string.IsNullOrEmpty(LastConfig.DisplayLayout))
            {
                try
                {
                    using (StringReader reader = new StringReader(LastConfig.DisplayLayout))
                    {
                        XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(Layout);
                        xmlLayout.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    Result.Error("Failed to load display configuration: " + ex.Message).Report();
                }
            }
        }
    }
}
