using Microsoft.Extensions.Logging;
using SI.GameServer.Contract;
using SICore.Contracts;
using SIGame.ViewModel.Contracts;
using SIStorage.Service.Client;
using SIUI.ViewModel;

namespace SIGame.ViewModel.Services;

internal sealed class GameSettingsViewModelFactory : IGameSettingsViewModelFactory
{
    private readonly UserSettings _userSettings;
    private readonly CommonSettings _commonSettings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISIStorageClientFactory _siStorageClientFactory;
    private readonly IGameHost _gameHost;

    public GameSettingsViewModelFactory(
        UserSettings userSettings,
        CommonSettings commonSettings,
        ILoggerFactory loggerFactory,
        ISIStorageClientFactory siStorageClientFactory,
        IGameHost gameHost)
    {
        _userSettings = userSettings;
        _commonSettings = commonSettings;
        _loggerFactory = loggerFactory;
        _siStorageClientFactory = siStorageClientFactory;
        _gameHost = gameHost;
    }

    public GameSettingsViewModel Create(SettingsViewModel settingsViewModel, SIStorageInfo[] libraries, bool isNetworkGame = false, long? maxPackageSize = null)
    {
        var gameSettings = new GameSettingsViewModel(
            _userSettings.GameSettings,
            _commonSettings,
            _userSettings,
            settingsViewModel,
            _siStorageClientFactory,
            libraries,
            _gameHost,
            _loggerFactory,
            isNetworkGame,
            maxPackageSize);

        return gameSettings;
    }
}
