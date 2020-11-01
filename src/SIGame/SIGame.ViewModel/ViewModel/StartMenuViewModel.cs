using SICore;
using SIGame.ViewModel.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public sealed class StartMenuViewModel
    {
        public UICommandCollection MainCommands { get; } = new UICommandCollection();

        public HumanPlayerViewModel Human { get; set; }

        public ICommand SetProfile { get; set; }

        public ICommand NavigateToVK { get; private set; }

        public StartMenuViewModel()
        {
            NavigateToVK = new CustomCommand(NavigateToVK_Executed);
        }

        private void NavigateToVK_Executed(object arg)
        {
            try
            {
                Process.Start(Resources.GroupLink);
            }
            catch (Exception exc)
            {
                PlatformSpecific.PlatformManager.Instance.ShowMessage(string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message), PlatformSpecific.MessageType.Error);
            }
        }
    }
}
