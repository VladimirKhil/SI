﻿using NUnit.Framework;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using Utils.Timers;

namespace SImulator.ViewModel.Tests;

internal sealed class TestPlatformManager : PlatformManager
{
    public override ButtonManagerFactory ButtonManagerFactory { get; } = new TestButtonManagerFactory();

    public override string AskSelectColor()
    {
        throw new NotImplementedException();
    }

    public override Task<string?> AskSelectFileAsync(string header)
    {
        throw new NotImplementedException();
    }

    public override string AskSelectLogsFolder()
    {
        throw new NotImplementedException();
    }

    public override Task<IPackageSource?> AskSelectPackageAsync(string arg)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> AskStopGameAsync()
    {
        throw new NotImplementedException();
    }

    public override void ClearMedia()
    {
        throw new NotImplementedException();
    }

    public override Task CloseMainViewAsync()
    {
        throw new NotImplementedException();
    }

    public override void ClosePlayersView()
    {
        
    }

    public override IAnimatableTimer CreateAnimatableTimer() => new TestAnimatableTimer();

    public override IGameLogger CreateGameLogger(string? folder) => new TestLogger();

    public override Task CreateMainViewAsync(object dataContext, IDisplayDescriptor screen) => Task.CompletedTask;

    public override void CreatePlayersView(object dataContext)
    {
        throw new NotImplementedException();
    }

    public override string[] GetComPorts()
    {
        throw new NotImplementedException();
    }

    public override int GetKeyNumber(GameKey key)
    {
        throw new NotImplementedException();
    }

    public override string[] GetLocalComputers()
    {
        throw new NotImplementedException();
    }

    public override IDisplayDescriptor[] GetScreens() => new IDisplayDescriptor[] { new TestScreen() };

    public override void InitSettings(AppSettings defaultSettings)
    {
        throw new NotImplementedException();
    }

    public override bool IsEscapeKey(GameKey key)
    {
        throw new NotImplementedException();
    }

    public override void NavigateToSite()
    {
        throw new NotImplementedException();
    }

    public override void PlaySound(string name, Action? onFinish)
    {
        
    }

    public override void ShowMessage(string text, bool error = true) => Assert.Fail(text);
}
