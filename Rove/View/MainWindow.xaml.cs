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

namespace Rove.View
{
    public partial class MainWindow : Window
    {
        private static void CreateDefaultConfigFile()
        {
            var defaultConfig = ConfigSerializer.ConfigToText(OverallConfigSerialize.DefaultConfig);
            var file= typeof(MainWindow).Assembly.Location.Replace(".exe", "") + "Default.xml";
            File.WriteAllText(file, defaultConfig);
        }

        private static OverallConfig LoadConfig()
        {
            var file = typeof(MainWindow).Assembly.Location.Replace(".exe", "") + ".xml";
            if (!File.Exists(file))
            {
                return OverallConfigSerialize.DefaultConfig.ToOverallConfig();
            }

            var content = File.ReadAllText(file);
            return ConfigSerializer.TextToConfig(content).ToOverallConfig();
        }

        private DispatcherTimer Timer { get; } = new DispatcherTimer();

        private object TImerLock { get; } = new object();

        private bool IsDisposed { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            OverallConfig config = null;
            try
            {
                CreateDefaultConfigFile();
                config = LoadConfig();
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
                        CanClose = false
                    });
            }
        }
    }
}
