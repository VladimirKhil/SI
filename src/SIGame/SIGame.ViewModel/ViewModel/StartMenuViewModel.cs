﻿using SIGame.ViewModel.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIGame.ViewModel;

/// <summary>
/// Defines a start menu view model.
/// </summary>
public sealed class StartMenuViewModel : INotifyPropertyChanged
{
    private const string TwitchUri = @"https://www.twitch.tv/directory/category/sigame";
    private const string DiscordUri = @"https://discord.gg/pGXTE37gpv";
    private const string SImulatorUri = @"https://vladimirkhil.com/si/simulator";
    private const string YoomoneyUri = @"https://yoomoney.ru/to/410012283941753";
    private const string BoostyUri = @"https://boosty.to/vladimirkhil";
    private const string PatreonUri = @"https://patreon.com/vladimirkhil";
    private const string SIGameUri = @"https://vladimirkhil.com/si/game";
    private const string SteamUri = @"https://store.steampowered.com/app/3553500/SIGame";

    public UICommandCollection MainCommands { get; } = new();

    public HumanPlayerViewModel Human { get; }

    public ICommand SetProfile { get; }

    public ICommand NavigateToVK { get; private set; }

    public ICommand NavigateToTwitch { get; private set; }

    public ICommand NavigateToDiscord { get; private set; }

    public ICommand NavigateToSImulator { get; private set; }

    public ICommand NavigateToYoomoney { get; private set; }

    public ICommand NavigateToBoosty { get; private set; }

    public ICommand NavigateToPatreon { get; private set; }

    public ICommand LoadSIGame8 { get; private set; }

    public ICommand NavigateToSteam { get; }

    private ICommand? _update;

    public ICommand? Update
    {
        get => _update;
        set
        {
            _update = value;
            OnPropertyChanged();
        }
    }

    public ICommand CancelUpdate { get; set; }

    public Version UpdateVersion { get; set; } = new();

    public string UpdateVersionMessage => string.Format(Resources.UpdateVersionMessage, UpdateVersion);

    public StartMenuViewModel(HumanPlayerViewModel human, ICommand setProfile)
    {
        Human = human;
        SetProfile = setProfile;

        NavigateToVK = new SimpleCommand(NavigateToVK_Executed)
        { 
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name == "ru-RU"
        };

        NavigateToTwitch = new SimpleCommand(NavigateToTwitch_Executed);
        
        NavigateToDiscord = new SimpleCommand(NavigateToDiscord_Executed)
        {
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name != "ru-RU"
        };

        NavigateToSImulator = new SimpleCommand(NavigateToSImulator_Executed);
        
        NavigateToYoomoney = new SimpleCommand(NavigateToYoomoney_Executed)
        {
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name == "ru-RU"
        };

        NavigateToBoosty = new SimpleCommand(NavigateToBoosty_Executed)
        {
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name == "ru-RU"
        };

        NavigateToPatreon = new SimpleCommand(NavigateToPatreon_Executed);

        CancelUpdate = new SimpleCommand(obj => Update = null);
        LoadSIGame8 = new SimpleCommand(LoadSIGame8_Executed) { CanBeExecuted = Environment.OSVersion.Version.Major >= 10 };
        NavigateToSteam = new SimpleCommand(NavigateToSteam_Executed);
    }

    private void NavigateToSteam_Executed(object? arg)
    {
        try
        {
            Browser.Open(SteamUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void LoadSIGame8_Executed(object? arg)
    {
        try
        {
            Browser.Open(SIGameUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToSImulator_Executed(object? arg)
    {
        try
        {
            Browser.Open(SImulatorUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToVK_Executed(object? arg)
    {
        try
        {
            Browser.Open(Resources.GroupLink);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToTwitch_Executed(object? arg)
    {
        try
        {
            Browser.Open(TwitchUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", TwitchUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToDiscord_Executed(object? arg)
    {
        try
        {
            Browser.Open(DiscordUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", DiscordUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToYoomoney_Executed(object? arg)
    {
        try
        {
            Browser.Open(YoomoneyUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", YoomoneyUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToBoosty_Executed(object? arg)
    {
        try
        {
            Browser.Open(BoostyUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", BoostyUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToPatreon_Executed(object? arg)
    {
        try
        {
            Browser.Open(PatreonUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", PatreonUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
