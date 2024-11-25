using AppRegistryService.Contract;
using AppRegistryService.Contract.Models;
using AppRegistryService.Contract.Requests;
using SIGame.Contracts;
using SIGame.Properties;
using SIGame.ViewModel;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Contract;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Utils.Commands;

namespace SIGame.Implementation;

/// <inheritdoc cref="IErrorManager" />
internal sealed class ErrorManager : IErrorManager
{
    private static bool ErrorHappened = false;
    private static readonly object ErrorSync = new();

    private readonly IAppRegistryServiceClient _appRegistryServiceClient;
    private readonly ISIStatisticsServiceClient _siStatisticsServiceClient;

    private readonly AppState _appState;

    public ErrorManager(
        IAppRegistryServiceClient appRegistryServiceClient,
        ISIStatisticsServiceClient gameResultServiceClient,
        AppState appState)
    {
        _appRegistryServiceClient = appRegistryServiceClient;
        _siStatisticsServiceClient = gameResultServiceClient;

        _appState = appState;
    }

    public bool SendErrorReport(Exception e)
    {
        Monitor.Enter(ErrorSync);

        try
        {
            if (ErrorHappened)
            {
                return false;
            }

            ErrorHappened = true;
        }
        finally
        {
            Monitor.Exit(ErrorSync);
        }

        var exc = e;

        var result = new StringBuilder();

        while (exc != null)
        {
            if (result.Length > 0)
            {
                result.AppendLine().AppendLine("======").AppendLine();
            }

            result.AppendLine(exc.ToStringDemystified());
            exc = exc.InnerException;
        }

        if (exc is InvalidProgramException)
        {
            MessageBox.Show(
                string.Format(Resources.ErrorNET, exc.Message),
                CommonSettings.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return true;
        }

        var errorMessage = result.ToString();
        var sync = new object();
        Application.Current.Dispatcher.Invoke(SendErrorMessage, errorMessage, sync);
        return true;
    }

    private async void SendErrorMessage(string errorMessage, object sync)
    {
        try
        {
            var errorReport = new SIReport
            {
                Comment = "",
                Report = errorMessage,
                Title = Resources.FatalErrorMessage,
                Subtitle = Resources.ErrorDescribe
            };

            var reportView = new GameReportView { DataContext = errorReport, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };

            var mainWindow = Application.Current.MainWindow;

            errorReport.SendReport = new SimpleCommand(async arg =>
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

                try
                {
                    var result = await _appRegistryServiceClient.Apps.SendAppErrorReportAsync(
                        App.AppId,
                        new AppErrorRequest(version, Environment.OSVersion.Version, RuntimeInformation.OSArchitecture)
                        {
                            ErrorMessage = errorMessage,
                            ErrorTime = DateTimeOffset.UtcNow,
                            UserNotes = errorReport.Comment
                        });

                    switch (result)
                    {
                        case ErrorStatus.Fixed:
                            MessageBox.Show(Resources.ErrorFixed, CommonSettings.AppName);
                            break;

                        case ErrorStatus.CannotReproduce:
                            MessageBox.Show(Resources.ErrorNotReproduced, CommonSettings.AppName);
                            break;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Resources.ReportDelayed);

                    if (CommonSettings.Default.DelayedErrorsNew.Count < 10)
                    {
                        CommonSettings.Default.DelayedErrorsNew.Add(
                            new ErrorInfo
                            {
                                Error = errorMessage,
                                Time = DateTime.Now,
                                Version = version.ToString(),
                                UserNotes = errorReport.Comment
                            });
                    }
                }

                mainWindow?.Close();

                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            });

            errorReport.SendNoReport = new SimpleCommand(arg =>
            {
                mainWindow?.Close();
                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            });

            if (mainWindow != null && mainWindow.IsVisible)
            {
                mainWindow.Content = reportView;
                mainWindow.Background = (Brush)Application.Current.Resources["WindowBackground"];
            }
            else if (MessageBox.Show(Resources.FatalErrorMessage, CommonSettings.AppName, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                errorReport.SendReport.Execute(null);
            }
            else
            {
                errorReport.SendNoReport.Execute(null);
            }
        }
        catch (Exception)
        {
            if (MessageBox.Show(Resources.FatalErrorMessage, CommonSettings.AppName, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
                
                try
                {
                    await _appRegistryServiceClient.Apps.SendAppErrorReportAsync(
                        App.AppId,
                        new AppErrorRequest(version, Environment.OSVersion.Version, RuntimeInformation.OSArchitecture)
                        {
                            ErrorMessage = errorMessage,
                            ErrorTime = DateTimeOffset.UtcNow,
                        });
                }
                catch (Exception)
                {
                    MessageBox.Show(Resources.ReportDelayed);

                    if (CommonSettings.Default.DelayedErrorsNew.Count < 10)
                    {
                        CommonSettings.Default.DelayedErrorsNew.Add(
                            new ErrorInfo
                            {
                                Version = version.ToString(),
                                Time = DateTime.Now,
                                Error = errorMessage
                            });
                    }
                }

                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            }
        }
    }

    public async void SendDelayedReports()
    {
        try
        {
            if (CommonSettings.Default.DelayedErrorsNew.Count == 0 && _appState.DelayedReports.Count == 0)
            {
                return;
            }

            while (CommonSettings.Default.DelayedErrorsNew.Count > 0)
            {
                var report = CommonSettings.Default.DelayedErrorsNew[0];

                if (report.Version != null && report.Error != null)
                {
                    await _appRegistryServiceClient.Apps.SendAppErrorReportAsync(
                        App.AppId,
                        new AppErrorRequest(Version.Parse(report.Version), Environment.OSVersion.Version, RuntimeInformation.OSArchitecture)
                        {
                            ErrorMessage = report.Error,
                            ErrorTime = report.Time,
                            UserNotes = report.UserNotes
                        });
                }

                CommonSettings.Default.DelayedErrorsNew.RemoveAt(0);
            }

            while (_appState.DelayedReports.Count > 0)
            {
                await _siStatisticsServiceClient.SendGameReportAsync(_appState.DelayedReports[0]);
                _appState.DelayedReports.RemoveAt(0);
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("SendDelayedReports error " + exc.ToStringDemystified());
        }
    }
}
