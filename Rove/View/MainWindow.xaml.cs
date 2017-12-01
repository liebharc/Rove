using Rove.ViewModel;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using Rove.Model;
using System.IO;
using Xceed.Wpf.AvalonDock.Layout;
using System.Xml;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace Rove.View
{
    public partial class MainWindow : Window
    {
        private static void CreateDefaultConfigFile()
        {
            WriteConfigFile(OverallConfigSerialize.DefaultConfig, GetLogBaseName() + "Default.xml");
        }

        private static void WriteConfigFile(OverallConfigSerialize config, string file)
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

        private static OverallConfigSerialize LoadConfig()
        {
            var file = GetLogName();
            if (!File.Exists(file))
            {
                return OverallConfigSerialize.DefaultConfig;
            }

            var content = File.ReadAllText(file);
            return ConfigSerializer.TextToConfig(content);
        }

        private DispatcherTimer Timer { get; } = new DispatcherTimer();

        private object TImerLock { get; } = new object();

        private bool IsDisposed { get; set; } = false;

        private OverallConfigSerialize LastConfig { get; }

        public MainWindow()
        {
            InitializeComponent();
            OverallConfig config = null;
            try
            {
                CreateDefaultConfigFile();
                LastConfig = LoadConfig();
                config = LastConfig.ToOverallConfig();
            }
            catch (ArgumentException ex)
            {
                Result.Error("Error with configuration: " + ex.Message).Report();
                Environment.Exit(0);
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

            Timer.Interval = TimeSpan.FromSeconds(0.1);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lock (TImerLock)
            {
                if (IsDisposed)
                {
                    return;
                }

                (DataContext as TomcatProcessViewModelCollection)?.Update();
            }
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            lock (TImerLock)
            {
                IsDisposed = true;
                Timer.Stop();
                (DataContext as TomcatProcessViewModelCollection)?.Dispose(); (DataContext as TomcatProcessViewModelCollection)?.Update();
            }

            SaveLayout();
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
