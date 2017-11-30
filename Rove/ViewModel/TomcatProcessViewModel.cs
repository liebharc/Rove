using Rove.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Rove.ViewModel
{
    public sealed class TomcatProcessViewModel : IDisposable, INotifyPropertyChanged
    {
        private TomcatProcessControl Tomcat { get; set; }

        public OverallConfig Config { get; }

        public ProcessConfig ProcessConfig { get; }

        public ICommand Close { get; }

        public ICommand ShowHide { get; }

        public ICommand StartProcess { get; }

        public bool IsDisposed => Tomcat.IsDisposed;

        public int Id => Tomcat.Id;

        public bool IsVisible { get; private set; } = false;

        public bool IsEnabled => Tomcat != null && !Tomcat.IsDisposed;

        public string Title => ProcessConfig.ProcessName;

        public TomcatProcessViewModel(OverallConfig config, ProcessConfig processConfig)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (processConfig == null)
            {
                throw new ArgumentNullException(nameof(processConfig));
            }

            StartProcess = new LambdaCommand(() => Script.Run(processConfig.StartProcessScript));
            Close = new LambdaCommand(() => Tomcat?.Kill());
            ShowHide = new LambdaCommand(() =>
            {
                if (IsVisible)
                {
                    Tomcat?.Hide();
                    IsVisible = false;
                }
                else
                {
                    Tomcat?.Show();
                    IsVisible = true;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            });
            Config = config;
            ProcessConfig = processConfig;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            Tomcat?.Dispose();
        }

        internal void Update()
        {
            if (Tomcat == null || Tomcat.IsDisposed)
            {
                Tomcat = null;
            }
        }
    }

    public sealed class TomcatProcessViewModelCollection : IDisposable
    {
        public ObservableCollection<TomcatProcessViewModel> Processes { get; } 
            = new ObservableCollection<TomcatProcessViewModel>();

        public TomcatProcessViewModelCollection(OverallConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            foreach (var process in config.ProcessConfigs)
            {
                Processes.Add(new TomcatProcessViewModel(config, process));
            }
        }

        public void Dispose()
        {
            foreach (var tomcat in Processes)
            {
                tomcat.Dispose();
            }
        }

        internal void Update()
        {
            foreach (var process in Processes)
            {
                process.Update();
            }
        }
    }
}
