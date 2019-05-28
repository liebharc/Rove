using Rove.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Rove.View;
using System.Windows.Media;
using System.Text;
using System.Windows;
using FastColoredTextBoxNS;

namespace Rove.ViewModel
{
    public sealed class TomcatProcessViewModel : IDisposable, INotifyPropertyChanged
    {
        private class TrafficStat
        {
            public TrafficStat(DateTime time, int lines, int chars)
            {
                Time = time;
                Lines = lines;
                Chars = chars;
            }

            public DateTime Time { get; }
            public int Lines { get; }
            public int Chars { get; }
        }

        private class LogFileException : Exception
        {
            public LogFileException(string message) : base(message)
            {
            }
        }

        private BoundedExponentialPollingInterval SearchForLogFilePollingInterval { get; } = new BoundedExponentialPollingInterval();

        private TomcatProcessControl Tomcat { get; set; }

        private object TomcatLock { get; } = new object();

        private TailLogFile LogFile { get; set; }

        private OverallConfigChecked Config { get; }

        private ProcessConfigChecked ProcessConfig { get; }
        public CurrentRoveEnvironment CurrentEnvironment { get; }

        public ICommand Close { get; }

        public ICommand ShowHide { get; }

        public ICommand OpenLogFile { get; }

        public ICommand ClearErrorStats { get; }

        public bool IsDisposed => Tomcat == null || Tomcat.IsDisposed;

        public int Id => Tomcat == null ? -1 : Tomcat.Id;

        public bool IsVisible { get; private set; } = false;

        public SolidColorBrush Color { get; }

        public bool IsEnabled => Tomcat != null && !Tomcat.IsDisposed;

        public string Title => ProcessConfig.ProcessName;

        public event PropertyChangedEventHandler PropertyChanged;

        private StringBuilder LogContent { get; } = new StringBuilder();

        private FastColoredTextBoxNS.FastColoredTextBox LogViewer { get; set; }

        private Queue<TrafficStat> RecentTraffic { get; } = new Queue<TrafficStat>();

        private List<string> Backlog { get; } = new List<string>();

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

        private bool _updateEnabled = true;

        public bool UpdateEnabled
        {
            get
            {
                return _updateEnabled;
            }

            set
            {
                _updateEnabled = value;
                OnPropertyChanged(nameof(UpdateEnabled));
                if (value)
                {
                    ProcessBacklog();
                }
            }
        }

        public TimeSpan LogFileIdleRecheckDuration { get; }

        public TomcatProcessViewModel(OverallConfigChecked config, ProcessConfigChecked processConfig, CurrentRoveEnvironment currentEnvironment)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (processConfig == null)
            {
                throw new ArgumentNullException(nameof(processConfig));
            }

            CurrentEnvironment = currentEnvironment;
            LogFileIdleRecheckDuration = processConfig.LogFileIdleRecheckDuration;
            Close = new LambdaCommand(() => Tomcat?.Kill());
            OpenLogFile = new LambdaCommand(() => 
                {
                    if (LogFile != null && LogFile.File != null)
                    {
                        ProcessUtils.Run("explorer.exe", ProcessUtils.DefaultWorkingDir, QuoteDouble(LogFile.File.FullName)).Report();
                    }
                }
            );
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
                ClearErrorStatistics();
            });
            Config = config;
            ProcessConfig = processConfig;
            if (processConfig.Color != null)
            {
                Color = new SolidColorBrush(processConfig.Color);
            }
        }

        private void ClearErrorStatistics()
        {
            ErrorCount = 0;
            WarnCount = 0;
            FirstError = string.Empty;
            StartupMessageCount = 0;
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
            lock (TomcatLock)
            {
                try
                {
                    TryToAttachToTomcatIfIdle(tomcats);
                    Tomcat?.Update();
                }
                catch (LogFileException ex)
                {
                    Result.Error(ex.Message).Report();
                    Tomcat?.Dispose();
                    IsVisible = false;
                    Tomcat = null;
                }
            }
        }

        private void TryToAttachToTomcatIfIdle(IEnumerable<TomcatProcessInfo> tomcats)
        {
            if (Tomcat == null)
            {
                Tomcat = tomcats
                    .FirstOrDefault(t => ProcessConfig.IsKnownProcess.IsMatch(t.CommandLine))
                    ?.Control();
                if (Tomcat != null)
                {
                    OnNewTomcat();
                    StartToReadLogFile(DetermineLogFilePath(), true);
                    IsVisible = false;
                    OnTomcatChanged();
                }
            }
            else if (Tomcat.IsDisposed)
            {
                Tomcat = null;
				LogFile?.Dispose();
				LogFile = null;
                IsVisible = false;
                if (LogFile != null)
                {
                    DisposeLogFile();
                }

                OnTomcatChanged();
            }
            else if (LogFile == null)
            {
                StartToReadLogFile(DetermineLogFilePath(), true);
            }
            else if (LogFile != null && LogFile.IsIdle(LogFileIdleRecheckDuration))
            {
                LogFile.RememberIdleCheck();
                var logFile = DetermineLogFilePath();
                if (!String.IsNullOrEmpty(logFile) && File.Exists(logFile) && (LogFile== null || !new FileInfo(logFile).FullName.Equals(LogFile.File.FullName)))
                {
                    DisposeLogFile();
                    StartToReadLogFile(logFile, false);
                }
            }
        }

        internal void Initialize(ProcessInfo panel)
        {
            LogViewer = panel.Log.Child as FastColoredTextBoxNS.FastColoredTextBox;
            LogViewer.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if (ProcessConfig.ErrorMessage.IsMatch(e.ChangedRange.Text))
            {
                e.ChangedRange.SetStyle(LogColors.ErrorStyle);
                ErrorCount++;
                if (ErrorCount == 0)
                {
                    FirstError = e.ChangedRange.Text;
                }
            }
            else if (ProcessConfig.WarningMessage.IsMatch(e.ChangedRange.Text))
            {
                e.ChangedRange.SetStyle(LogColors.WarnStyle);
                WarnCount++;
            }
            else if (ProcessConfig.StartupMessage.IsMatch(e.ChangedRange.Text))
            {
                e.ChangedRange.SetStyle(LogColors.StartupStyle);
                StartupMessageCount++;
            }
        }

        private void OnNewTomcat()
        {
            if (LogFile != null)
            {
                DisposeLogFile();
            }

            SearchForLogFilePollingInterval.Reset();

            if (Config.SetRoveEnvScript != null)
            {
                var result = Script.Run(Config.SetRoveEnvScript, CurrentEnvironment, new[] { QuoteSingle(Tomcat.CommandLine) });
                SetRoveEnv(result);
            }

            if (Config.OnNewProcessScript != null)
            {
                Script.Run(Config.OnNewProcessScript, CurrentEnvironment, new[] { QuoteSingle(Tomcat.CommandLine) }).Check().Report();
            }

            if (ProcessConfig.OnProcessStartedScript != null)
            {
                Script.Run(ProcessConfig.OnProcessStartedScript, CurrentEnvironment, new[] { QuoteSingle(Tomcat.CommandLine) }).Check().Report();
            }
        }

        private void SetRoveEnv(Script.ScriptResult result)
        {
            result.Check().Report();
            if (result.StdOut.Count == 1)
            {
                var newEnv = result.StdOut.First();
                Logger.WriteInfo("SetRoveEnvScript set env to " + newEnv);
                CurrentEnvironment.Selection = newEnv;
            }
            else if (result.StdOut.Any())
            {
                Logger.WriteInfo("SetRoveEnvScript was chattier than expected: " + string.Join("\n", result.StdOut));
            }
        }

        private void DisposeLogFile()
        {
            LogFile.Dispose();
            LogFile.NewMessagesArrived -= LogFile_NewMessagesArrived;
            LogFile = null;
        }

        private void StartToReadLogFile(string file, bool isNewSession)
        {
            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    if (File.Exists(file))
                    {
                        LogFile = new TailLogFile(new FileInfo(file), isNewSession);
                        LogFile.NewMessagesArrived += LogFile_NewMessagesArrived;
                        LogFile.Start();
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => DisplayMessageInLogWindow("Waiting for " + file + " to become available")));
                    }
                }
                catch (Exception ex)
                {
                    throw new LogFileException(ProcessConfig.FindLogFileScript + " returned an invalid result: " + file + " error was " + ex.Message);
                }
            }
        }

        private string DetermineLogFilePath()
        {
            if (!SearchForLogFilePollingInterval.IsTimeForPolling())
            {
                return string.Empty;
            }

            var logResult = Script.Run(ProcessConfig.FindLogFileScript, CurrentEnvironment, new[] { QuoteSingle(Tomcat.CommandLine) });
            if (logResult.Check().IsError)
            {
                logResult.Check().Report();
                return string.Empty;
            }
            else if (logResult.StdOut.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(() => DisplayMessageInLogWindow(ProcessConfig.FindLogFileScript + " didn't return a result yet"));
                return string.Empty;
            }
            else if (logResult.StdOut.Count > 1)
            {
                throw new LogFileException(ProcessConfig.FindLogFileScript + " returned too many results:\n" + string.Join("\n", logResult.StdOut));
            }

            var file = logResult.StdOut.First();
            return file;
        }

        private void LogFile_NewMessagesArrived(bool isNewTailSession, int charCount, List<string> lines)
        {
            if (!UpdateEnabled)
            {
                Backlog.AddRange(lines);
                if (Config.LogHistory > 0 && Backlog.Count > Config.LogHistory)
                {
                    Backlog.RemoveRange(0, Backlog.Count - Config.LogHistory);
                }
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() => WriteLines(isNewTailSession, charCount, lines)));
        }

        private void ProcessBacklog()
        {
            WriteLines(false, Backlog.Select(b => b.Length).Sum() , Backlog);
            Backlog.Clear();
        }

        private void WriteLines(bool isNewTailSession, int charCount, List<string> lines)
        {
            if (isNewTailSession)
            {
                ClearLogWindow();
                ClearErrorStatistics();
            }

            var now = DateTime.Now;
            const int SPEED_WINDOW = 3;
            RecentTraffic.Enqueue(new TrafficStat(now, lines.Count, charCount));
            while ((now - RecentTraffic.First().Time) > TimeSpan.FromSeconds(SPEED_WINDOW))
            {
                RecentTraffic.Dequeue();
            }

            double avgVelocity = RecentTraffic.Select(t => t.Chars).Sum() / SPEED_WINDOW;
            if (RecentTraffic.Count > 2 && avgVelocity > Config.UpdateLimit)
            {
                DisplayMessageInLogWindow("Too many new log messages for this application. Consider to open the log file with a dedicated log viewer tool. Received " + avgVelocity + " chars in the last second");
                return;
            }


            int startLine = 0;
            if (Config.LogHistory > 0 && lines.Count > Config.LogHistory)
            {
                LogViewer.Clear();
                startLine = lines.Count - Config.LogHistory;
            }

            for (int i = startLine; i < lines.Count; i++)
            {
                LogViewer.AppendText(lines[i] + "\n");
            }

            if (Config.LogHistory > 0)
            {
                if (LogViewer.LinesCount > Config.LogHistory)
                {
                    LogViewer.RemoveLines(Enumerable.Range(0, LogViewer.LinesCount - Config.LogHistory).ToList());
                }
            }

            GoEndWhileKeepingHorizontalPosition(LogViewer);
        }

        private void GoEndWhileKeepingHorizontalPosition(FastColoredTextBox viewer)
        {
            var value = viewer.HorizontalScroll.Value;
            viewer.GoEnd();
            viewer.HorizontalScroll.Value = value;
            viewer.UpdateScrollbars();
        }

        private void DisplayMessageInLogWindow(string message)
        {
            ClearLogWindow();
            LogViewer.Text = message + "\n";
        }

        private void ClearLogWindow()
        {
            LogViewer.Text = string.Empty;
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
