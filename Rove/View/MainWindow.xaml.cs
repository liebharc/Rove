﻿using Rove.ViewModel;
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
using System.Linq;

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
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
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
                StoreCurrentLayout();
                StoreCurrentAutoScrollValues();
                WriteConfigFile(LastConfig, GetLogName());
                IsDisposed = true;
            }
        }

        private void CreateAPanelForEachProcess(IList<TomcatProcessViewModel> processes)
        {
            var initialPane = LayoutDocumentPaneFindLayoutDocumentPanes(Layout.Layout.Children).First();
            List<LayoutDocument> documents = new List<LayoutDocument>();
            foreach (var process in processes)
            {
                var panel = new ProcessInfo { DataContext = process };
                process.Initialize(panel);
                var document = new LayoutDocument
                {
                    Title = process.Title,
                    Content = panel,
                    CanClose = false,
                    ContentId = process.Title
                };

                initialPane.Children.Add(document);
                documents.Add(document);
            }

            RestoreLayout();
            HandleChildrenWhichAreNotPartOfTheRestoredLayout(documents);
        }

        private List<LayoutDocumentPane> LayoutDocumentPaneFindLayoutDocumentPanes(IEnumerable<ILayoutElement> elements)
        {
            List<LayoutDocumentPane> result = new List<LayoutDocumentPane>();
            foreach (var element in elements)
            {
                var pane = element as LayoutDocumentPane;
                if (pane != null)
                {
                    result.Add(pane);
                    continue;
                }

                var panel = element as LayoutPanel;
                if (panel != null)
                {
                    result.AddRange(LayoutDocumentPaneFindLayoutDocumentPanes(panel.Children));
                    continue;
                }

                var group = element as LayoutDocumentPaneGroup;
                if (group != null)
                {
                    result.AddRange(LayoutDocumentPaneFindLayoutDocumentPanes(group.Children));
                    continue;
                }
            }

            return result;
        }

        private void HandleChildrenWhichAreNotPartOfTheRestoredLayout(List<LayoutDocument> documents)
        {
            var panes = LayoutDocumentPaneFindLayoutDocumentPanes(Layout.Layout.Children);
            var available =
                panes
                .SelectMany(c => c.Children.OfType<LayoutDocument>());
            var orphans = documents.Where(d => available.All(a => a.ContentId != d.ContentId)).ToList();
            foreach (var orphan in orphans)
            {
                var pane = panes.First();
                pane.Children.Add(orphan);
            }
        }

        private void StoreCurrentLayout()
        {
            try
            {
                using (StringWriter writer = new StringWriter())
                {
                    XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(Layout);
                    xmlLayout.Serialize(writer);
                    LastConfig.DisplayLayout = writer.ToString();
                }
            }
            catch (Exception ex)
            {
                Result.Error("Failed to save display configuration: " + ex.Message).Report();
            }
        }

        private void StoreCurrentAutoScrollValues()
        {
            var model = DataContext as TomcatProcessViewModelCollection;
            if (model == null || model.Processes.Count != LastConfig.ProcessConfigs.Count)
            {
                return;
            }

            for (int i = 0; i < model.Processes.Count; i++)
            {
                LastConfig.ProcessConfigs[i].AutoScroll = model.Processes[i].AutoScroll;
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
