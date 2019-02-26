using Rove.Model;
using Rove.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Rove.ViewModel
{
    public sealed class TopViewModel : INotifyPropertyChanged
    {
        public ICommand Help { get; }

        public CurrentRoveEnvironment CurrentRoveEnvironment { get; }

        public IEnumerable<string> Environments => CurrentRoveEnvironment.AvailbleEnvironments;

        public Visibility EnvironmentVisibility => Environments.Any() ? Visibility.Visible : Visibility.Collapsed;

        public string CurrentEnvironment
        {
            get
            {
                return CurrentRoveEnvironment.Selection;
            }

            set
            {
                CurrentRoveEnvironment.Selection = value;
            }
        }

        public TopViewModel(RoveEnvironments environments, string currentEnvironment)
        {
            CurrentRoveEnvironment = new CurrentRoveEnvironment(environments, currentEnvironment);
            CurrentRoveEnvironment.PropertyChanged += CurrentRoveEnvironment_PropertyChanged;
            Help = new LambdaCommand(() => 
            {
                var logWindow = new DisplayInternalLog();
                logWindow.Show();
            });
        }

        private void CurrentRoveEnvironment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentRoveEnvironment.Selection))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEnvironment)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
