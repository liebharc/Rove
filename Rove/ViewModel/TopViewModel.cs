﻿using Rove.Model;
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

        public TopViewModel(string currentEnvironment)
        {
            CurrentRoveEnvironment = new CurrentRoveEnvironment(currentEnvironment);
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
