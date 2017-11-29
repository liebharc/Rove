using Rove.ViewModel;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using Rove.Model;
using System.IO;

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
            OverallConfig config;
            try
            {
                CreateDefaultConfigFile();
                config = LoadConfig();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Error with configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            TomcatProcessViewModelCollection viewModel = new TomcatProcessViewModelCollection();
            DataContext = viewModel;
            viewModel.Processes.CollectionChanged += Processes_CollectionChanged;
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

        private void Processes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // TODO replace grid with more suitable GUI control
            var newItems = ToGenericList(e.NewItems);
            var oldItems = ToGenericList(e.OldItems);
            var added = newItems.Except(oldItems);
            var removed = oldItems.Except(newItems);
            if (removed.Any())
            {
                for (int i = 0; i < Grid.Children.Count; i++)
                {
                    var child = Grid.Children[i] as UserControl;
                    if (child != null && child.DataContext != null && removed.Contains(child.DataContext))
                    {
                        Grid.Children.RemoveAt(i);
                        i--;
                    }
                }
            }

            foreach (var addition in added)
            {
                Grid.Children.Add(new ProcessInfo { DataContext = addition });
            }

            const int RowSize = 3;
            int rows = (Grid.Children.Count + RowSize - 1) / RowSize;
            int columns = Math.Min(3, Grid.Children.Count);
            SetGridRows(rows);
            SetGridCols(columns);
            for (int i = 0; i < Grid.Children.Count; i++)
            {
                var child = Grid.Children[i] as UserControl;
                Grid.SetRow(child, i / RowSize);
                Grid.SetColumn(child, i % RowSize);
            }
        }

        private void SetGridRows(int rows)
        {
            while (Grid.RowDefinitions.Count < rows)
            {
                Grid.RowDefinitions.Add(new RowDefinition());
            }
            while (Grid.RowDefinitions.Count > rows)
            {
                Grid.RowDefinitions.RemoveAt(0);
            }
        }

        private void SetGridCols(int cols)
        {
            while (Grid.ColumnDefinitions.Count < cols)
            {
                Grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            while (Grid.ColumnDefinitions.Count > cols)
            {
                Grid.ColumnDefinitions.RemoveAt(0);
            }
        }

        private static IEnumerable<TomcatProcessViewModel> ToGenericList(System.Collections.IList list)
        {
            if (list == null)
            {
                return new List<TomcatProcessViewModel>();
            }
            return list.Cast<TomcatProcessViewModel>();
        }
    }
}
