using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Rove.Model
{
    public sealed class CurrentRoveEnvironment : INotifyPropertyChanged
    {
        public CurrentRoveEnvironment(string currentEnvironment)
        {
            Selection = currentEnvironment;
        }

        private string _selection;

        public string Selection {
            get { return _selection; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _selection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selection)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string MapSelected(string value)
        {
            return value.Replace("$RoveEnv", Selection);
        }
    }
}
