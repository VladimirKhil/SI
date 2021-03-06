using SIQuester.Model;
using System;
using System.Linq;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class SettingsViewModel: WorkspaceViewModel
    {
        public ICommand Reset { get; private set; }

        public override string Header => "Настройки";

        public string[] Fonts => System.Windows.Media.Fonts.SystemFontFamilies.Select(ff => ff.Source).OrderBy(f => f).ToArray();

        public bool SpellCheckingEnabled => Environment.OSVersion.Version > new Version(6, 2);

        public SettingsViewModel()
        {
            Reset = new SimpleCommand(Reset_Executed);
        }

        private void Reset_Executed(object arg) => AppSettings.Default.Reset();
    }
}
