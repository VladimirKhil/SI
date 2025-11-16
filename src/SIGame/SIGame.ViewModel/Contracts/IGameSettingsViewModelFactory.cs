using SI.GameServer.Contract;
using SIUI.ViewModel;

namespace SIGame.ViewModel.Contracts;

public interface IGameSettingsViewModelFactory
{
    GameSettingsViewModel Create(SettingsViewModel settingsViewModel, SIStorageInfo[] libraries, bool isNetworkGame = false, long? maxPackageSize = null);
}
