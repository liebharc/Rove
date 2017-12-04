using Rove.Model;
using System;
using System.Collections.Generic;
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

        private OverallConfig Config { get; }

        private ProcessConfig ProcessConfig { get; }

        public ICommand Close { get; }

        public ICommand ShowHide { get; }

        public ICommand StartProcess { get; }

        public ICommand OpenLogFile { get; }

        public ICommand ClearErrorStats { get; }

        public bool IsDisposed => Tomcat.IsDisposed;

        public int Id => Tomcat.Id;

        public bool IsVisible { get; private set; } = false;

        public SolidColorBrush Color { get; }

        public bool IsEnabled => Tomcat != null && !Tomcat.IsDisposed;

        public string Title => ProcessConfig.ProcessName;

        public event PropertyChangedEventHandler PropertyChanged;

        private RichTextBox Logger { get; set; }

        private int LineCount { get; set; }

        private int _warnCount;
        public int WarnCount
        {
            get
            {
                return _warnCount;
            }

            set
            {
                _warnCount = value;
                OnPropertyChanged(nameof(WarnCount));
            }
        }

        private int _errorCount;
        public int ErrorCount
        {
            get
            {
                return _errorCount;
            }

            set
            {
                _errorCount = value;
                OnPropertyChanged(nameof(ErrorCount));
            }
        }

        private int _startupMessageCount;
        public int StartupMessageCount
        {
            get
            {
                return _startupMessageCount;
            }

            set
            {
                _startupMessageCount = value;
                OnPropertyChanged(nameof(StartupMessageCount));
            }
        }

        private string _firstError;
        public string FirstError
        {
            get
            {
                return _firstError;
            }

            set
            {
                _firstError = value;
                OnPropertyChanged(nameof(FirstError));
            }
        }

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
            ClearErrorStats = new LambdaCommand(() =>
            {
                ErrorCount = 0;
                WarnCount = 0;
                FirstError = string.Empty;
                StartupMessageCount = 0;
            });
            Config = config;
            ProcessConfig = processConfig;
            if (processConfig.Color != null)
            {
                Color = new SolidColorBrush(processConfig.Color);
            }
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
            if (!Logger.Dispatcher.CheckAccess())
            {
                Logger.Dispatcher.InvokeAsync(() => LogFile_NewMessagesArrived(lines));
                return;
            }
            
            foreach (var line in lines)
            {
                if (ProcessConfig.ErrorMessage.IsMatch(line))
                {
                    if (ErrorCount == 0)
                    {
                        FirstError = line;
                    }
                    ErrorCount++;
                    Write(line, LogColors.ErrorForeground);
                }
                else if (ProcessConfig.WarningMessage.IsMatch(line))
                {
                    WarnCount++;
                    Write(line, LogColors.WarnForeground);
                }
                else if (ProcessConfig.StartupMessage.IsMatch(line))
                {
                    StartupMessageCount++;
                    Write(line, LogColors.StartupForeground);
                }
                else
                {
                    Write(line, LogColors.InfoForeground);
                }
                    
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
}
