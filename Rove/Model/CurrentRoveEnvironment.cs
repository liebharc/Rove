using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Rove.Model
{
    public sealed class CurrentRoveEnvironment : INotifyPropertyChanged
    {
        public CurrentRoveEnvironment(RoveEnvironments environments, string currentEnvironment)
        {
            Environments = environments;
            Selection = currentEnvironment;
        }

        private string _selection;

        public string Selection {
            get { return _selection; }
            set
            {
                if (!string.IsNullOrEmpty(value) && IsValid(value))
                {
                    _selection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selection)));
                }
            }
        }

        private bool IsValid(string selection)
        {
            return Environments.AvailbleEnvironments.Contains(selection);
        }

        public IEnumerable<string> AvailbleEnvironments => Environments.AvailbleEnvironments;

        private RoveEnvironments Environments { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
