using Rove.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rove.ViewModel
{
    public sealed class TomcatProcessViewModelCollection : IDisposable
    {
        public List<TomcatProcessViewModel> Processes { get; } = new List<TomcatProcessViewModel>();

        private SeenProcessList SeenProcessList { get; } = new SeenProcessList();

        public TomcatProcessViewModelCollection(OverallConfigChecked config, CurrentRoveEnvironment currentEnvironment)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            foreach (var process in config.ProcessConfigs)
            {
                Processes.Add(new TomcatProcessViewModel(config, process, currentEnvironment));
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
            var tomcats = TomcatProcessInfo.NewRunningTomcatProcesses(SeenProcessList);
            Parallel.ForEach(Processes, p => p.Update(tomcats));
        }
    }
}
