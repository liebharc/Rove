using Rove.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Rove.View;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Rove.ViewModel
{
    public sealed class TomcatProcessViewModel : IDisposable, INotifyPropertyChanged
    {
        private TomcatProcessControl Tomcat { get; set; }

        private TailLogFile LogFile { get; set; }

        public OverallConfig Config { get; }

        public ProcessConfig ProcessConfig { get; }

        public ICommand Close { get; }

        public ICommand ShowHide { get; }

        public ICommand StartProcess { get; }

        public ICommand OpenLogFile { get; }

        public bool IsDisposed => Tomcat.IsDisposed;

        public int Id => Tomcat.Id;

        public bool IsVisible { get; private set; } = false;

        public bool IsEnabled => Tomcat != null && !Tomcat.IsDisposed;

        public string Title => ProcessConfig.ProcessName;

        public event PropertyChangedEventHandler PropertyChanged;

        private RichTextBox Logger { get; set; }

        private int LineCount { get; set; }

        private bool _autoScroll = true;

        public bool AutoScroll
        {
            get
            {
                return _autoScroll;
            }

            set
            {
                _autoScroll = value;
                OnPropertyChanged(nameof(AutoScroll));
            }
        }

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

            StartProcess = new LambdaCommand(() => ProcessUtils.Run(processConfig.StartProcessScript).Report());
            Close = new LambdaCommand(() => Tomcat?.Kill());
            OpenLogFile = new LambdaCommand(() => ProcessUtils.Run("explorer.exe", QuoteDouble(LogFile.File.FullName)).Report());
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

        public void Dispose()
        {
            Tomcat?.Dispose();
            if (LogFile != null)
            {
                LogFile.Dispose();
                LogFile.NewMessagesArrived -= LogFile_NewMessagesArrived;
                LogFile = null;
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void Update(IEnumerable<TomcatProcessInfo> tomcats)
        {
            if (Tomcat == null)
            {
                Tomcat = tomcats
                    .FirstOrDefault(t => ProcessConfig.IsKnownProcess.IsMatch(t.CommandLine))
                    ?.Control();
                if (Tomcat != null)
                {
                    OnNewTomcat();
                    IsVisible = false;
                    OnTomcatChanged();
                }
            }
            else if (Tomcat.IsDisposed)
            {
                Tomcat = null;
                IsVisible = false;
                OnTomcatChanged();
            }
        }

        internal void Initialize(ProcessInfo panel)
        {
            Logger = panel.Log;
        }

        private void OnNewTomcat()
        {
            if (LogFile != null)
            {
                LogFile.Dispose();
                LogFile.NewMessagesArrived -= LogFile_NewMessagesArrived;
                LogFile = null;
            }

            if (Config.OnNewProcessScript != null)
            {
                Script.Run(Config.OnNewProcessScript, new[] { QuoteSingle(Tomcat.CommandLine) }).Check().Report();
            }

            if (ProcessConfig.OnProcessStartedScript != null)
            {
                Script.Run(ProcessConfig.OnProcessStartedScript, new[] { QuoteSingle(Tomcat.CommandLine) }).Check().Report();
            }

            var logResult = Script.Run(ProcessConfig.FindLogFileScript, new[] { QuoteSingle(Tomcat.CommandLine) });
            if (logResult.Check().IsError)
            {
                logResult.Check().Report();
            }
            else if (logResult.StdOut.Count == 0)
            {
                Result.Error(ProcessConfig.FindLogFileScript + " returned no result");
            }
            else if (logResult.StdOut.Count > 1)
            {
                Result.Error(ProcessConfig.FindLogFileScript + " returned too many results:\n" + string.Join("\n", logResult.StdOut));
            }
            else
            {
                var file = logResult.StdOut.First();
                try
                {
                    if (File.Exists(file))
                    {
                        LogFile = new TailLogFile(new FileInfo(file));
                        LogFile.NewMessagesArrived += LogFile_NewMessagesArrived;
                        LogFile.Start();
                    }
                    else
                    {
                        Result.Error(ProcessConfig.FindLogFileScript + " returned an invalid result: " + file);
                    }
                }
                catch (Exception ex)
                {
                    Result.Error(ProcessConfig.FindLogFileScript + " returned an invalid result: " + file + " error was " + ex.Message);
                }
            }
        }

        private void LogFile_NewMessagesArrived(IEnumerable<string> lines)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => LogFile_NewMessagesArrived(lines));
                return;
            }
            
            foreach (var line in lines)
            {
                // TODO count warnings and errors, display them and allow the user to clear them. Also check for startup message.s
                Write(line, LogColors.InfoForeground);
            }
        }


        private void Write(string logMessage, SolidColorBrush foreground)
        {
            if (Logger == null)
            {
                return;
            }

            var tr = new TextRange(Logger.Document.ContentEnd, Logger.Document.ContentEnd);
            tr.Text = logMessage + "\n";
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);
            
            if (Config.LogHistory > 0)
            {
                LineCount++;
                if (LineCount > Config.LogHistory)
                {
                    tr = new TextRange(Logger.Document.ContentStart, Logger.Document.ContentEnd);
                    tr.Text = tr.Text.Remove(0, tr.Text.IndexOf('\n'));
                    LineCount--;
                }
            }

            Logger.AppendText(tr.Text);

            if (AutoScroll)
            {
                Logger.ScrollToEnd();
            }
        }

        private static string QuoteSingle(string text)
        {
            return "'" + text + "'";
        }

        private static string QuoteDouble(string text)
        {
            return "\"" + text + "\"";
        }

        private void OnTomcatChanged()
        {
            OnPropertyChanged(nameof(Tomcat));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(IsVisible));
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
            var tomcats = TomcatProcessInfo.RunningTomcatProcesses;
            foreach (var process in Processes)
            {
                process.Update(tomcats);
            }
        }
    }
}
