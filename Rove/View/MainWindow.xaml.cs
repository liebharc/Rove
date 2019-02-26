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

    public partial class MainWindow
    {
        private static void CreateDefaultConfigFile()
        {
            WriteConfigFile(OverallConfig.DefaultConfig, GetAuxFileBaseName() + "Default.xml");
        }

        private static void WriteConfigFile<T>(T config, string file)
        {
            var defaultConfig = ConfigSerializer.ConfigToText(config);
            File.WriteAllText(file, defaultConfig);
        }

        private static string GetAuxFileBaseName()
        {
            return typeof(MainWindow).Assembly.Location.Replace(".exe", "");
        }

        private static string GetOverallConfigFileName()
        {
            return GetAuxFileBaseName() + ".xml";
        }

        private static string GetUserConfigFileName()
        {
            return GetAuxFileBaseName() + "User.xml";
        }

        private static OverallConfig LoadConfig()
        {
            var file = GetOverallConfigFileName();
            if (!File.Exists(file))
            {
                return OverallConfig.DefaultConfig;
            }

            var content = File.ReadAllText(file);
            return ConfigSerializer.TextToConfig<OverallConfig>(content);
        }

        private static UserConfig LoadDisplayConfig()
        {
            var file = GetUserConfigFileName();
            if (!File.Exists(file))
            {
                return UserConfig.DefaultConfig;
            }

            var content = File.ReadAllText(file);
            return ConfigSerializer.TextToConfig<UserConfig>(content);
        }

        private object UpdateThreadLock { get; } = new object();

        private Thread UpdateThread { get; }

        private volatile bool _isTimerActive = true;

        private bool IsDisposed { get; set; } = false;

        private CurrentRoveEnvironment CurrentRoveEnvironment { get; }

        public MainWindow()
        {
            InitializeComponent();
            OverallConfigChecked config = null;
            UserConfig user = UserConfig.DefaultConfig;
            try
            {
                CreateDefaultConfigFile();
                var overall = LoadConfig();
                user = LoadDisplayConfig();
                config = overall.ToOverallConfig(user);
            }
            catch (Exception ex)
            {
                Result.Error(ex.Message).Report();
                Environment.Exit(0);
            }

            var topModel = new TopViewModel(user.CurrentRoveEnvironment);
            TopBar.DataContext = topModel;
            TomcatProcessViewModelCollection viewModel = new TomcatProcessViewModelCollection(config, topModel.CurrentRoveEnvironment);
            CurrentRoveEnvironment = topModel.CurrentRoveEnvironment;
            CreateAPanelForEachProcess(viewModel.Processes, user);

            DataContext = viewModel;
            Closed += MainWindow_Closed;

            UpdateThread = new Thread((_) => Update(viewModel)) { IsBackground = true, Name = "Update Thread" };
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
                (DataContext as IDisposable)?.Dispose();
                _isTimerActive = false;

                UpdateThread.Join();
                var config = new UserConfig
                {
                    DisplayLayout = StoreCurrentLayout(),
                    ProcessConfigs = StoreCurrentAutoScrollValues(),
                    CurrentRoveEnvironment = CurrentRoveEnvironment.Selection
                };
                WriteConfigFile(config, GetUserConfigFileName());
                IsDisposed = true;
            }
        }

        private void CreateAPanelForEachProcess(IList<TomcatProcessViewModel> processes, UserConfig user)
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

            RestoreLayout(user);
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

        private string StoreCurrentLayout()
        {
            try
            {
                using (StringWriter writer = new StringWriter())
                {
                    XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(Layout);
                    xmlLayout.Serialize(writer);
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                Result.Error("Failed to save display configuration: " + ex.Message).Report();
                return string.Empty;
            }
        }

        private List<ProcessUserConfig> StoreCurrentAutoScrollValues()
        {
            var model = DataContext as TomcatProcessViewModelCollection;
            if (model == null)
            {
                return new List<ProcessUserConfig>();
            }

            return model.Processes
                .Select(p => new ProcessUserConfig { ProcessName = p.Title })
                .ToList();
        }

        private void RestoreLayout(UserConfig userConfig)
        {
            if (!string.IsNullOrEmpty(userConfig.DisplayLayout))
            {
                try
                {
                    using (StringReader reader = new StringReader(userConfig.DisplayLayout))
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
