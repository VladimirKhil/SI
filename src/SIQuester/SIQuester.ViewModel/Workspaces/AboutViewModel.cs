using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Input;
using System.Diagnostics;
using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Информация о программе
    /// </summary>
    public sealed class AboutViewModel: WorkspaceViewModel
    {
        public override string Header => Resources.About;

        /// <summary>
        /// Версия программы
        /// </summary>
        public string Version
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
        }

        /// <summary>
        /// Перейти на сайт программы
        /// </summary>
        public ICommand OpenSite { get; private set; }
        public ICommand OpenIcons { get; private set; }
        public ICommand OpenIcons2 { get; private set; }

        public AboutViewModel()
        {
            OpenSite = new SimpleCommand(arg => GoToUrl("http://vladimirkhil.com/si/siquester"));
            OpenIcons = new SimpleCommand(arg => GoToUrl("http://www.famfamfam.com/lab/icons/silk"));
            OpenIcons2 = new SimpleCommand(arg => GoToUrl("http://modernuiicons.com"));
        }

        private void GoToUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }
    }
}
