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
        private TomcatProcessControl Tomcat { get; }

        public ICommand Close { get; }

        public ICommand ShowHide { get; }

        public bool IsDisposed => Tomcat.IsDisposed;

        public int Id => Tomcat.Id;

        public TomcatProcessViewModel(TomcatProcessControl tomcat)
        {
            Tomcat = tomcat;
            Close = new LambdaCommand(() => Tomcat.Kill());
            ShowHide = new LambdaCommand(() =>
            {
                if (IsVisible)
                {
                    Tomcat.Hide();
                    IsVisible = false;
                }
                else
                {
                    Tomcat.Show();
                    IsVisible = true;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            Tomcat.Dispose();
        }

        public override int GetHashCode()
        {
            return Tomcat.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool IsVisible { get; set; } = false;

        public bool Equals(TomcatProcessViewModel other)
        {
            if (other == null)
            {
                return false;
            }

            return Tomcat.Equals(other.Tomcat);
        }
    }

    public sealed class TomcatProcessViewModelCollection : IDisposable
    {
        public ObservableCollection<TomcatProcessViewModel> Processes { get; } 
            = new ObservableCollection<TomcatProcessViewModel>();

        private List<int> CapturedProcessIds { get; } = new List<int>();

        public void Dispose()
        {
            foreach (var tomcat in Processes)
            {
                tomcat.Dispose();
            }
        }

        internal void Update()
        {
            var processes = TomcatProcessInfo.RunningTomcatProcesses;
            foreach (var process in processes)
            {
                if (!CapturedProcessIds.Contains(process.Id))
                {
                    CapturedProcessIds.Add(process.Id);
                    Processes.Add(new TomcatProcessViewModel(process.Control()));
                }
            }

            for (int i = 0; i < Processes.Count; i++)
            {
                var process = Processes[i];
                if (process.IsDisposed)
                {
                    CapturedProcessIds.Remove(process.Id);
                    Processes.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
