using AppService.Client;
using AppService.Client.Models;
using Microsoft.Extensions.Options;
using SICore;
using SIGame.Contracts;
using SIGame.Properties;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Contract;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace SIGame.Implementation;

/// <inheritdoc cref="IErrorManager" />
internal sealed class ErrorManager : IErrorManager
{
    private const string AppCode = "SI";
    private static bool ErrorHappened = false;
    private static readonly object ErrorSync = new();

    private readonly IAppServiceClient _appServiceClient;
    private readonly ISIStatisticsServiceClient _siStatisticsServiceClient;

    private readonly AppState _appState;

    private readonly bool _useAppService;

    public ErrorManager(
        IAppServiceClient appServiceClient,
        ISIStatisticsServiceClient gameResultServiceClient,
        AppState appState,
        IOptions<AppServiceClientOptions> appServiceClientOptions)
    {
        _appServiceClient = appServiceClient;
        _siStatisticsServiceClient = gameResultServiceClient;

        _appState = appState;

        _useAppService = appServiceClientOptions.Value.ServiceUri != null;
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
        if (!_useAppService)
        {
            MessageBox.Show(errorMessage);
            return;
        }

        try
        {
            var errorReport = new SIReport
            {
                Comment = "",
                Report = errorMessage,
                Title = Resources.FatalErrorMessage,
                Subtitle = Resources.ErrorDescribe
            };

            var reportView = new GameReportView { DataContext = errorReport };

            var mainWindow = Application.Current.MainWindow;

            errorReport.SendReport = new CustomCommand(async arg =>
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var message = errorMessage + Environment.NewLine + errorReport.Comment;

                try
                {
                    var result = await _appServiceClient.SendErrorReportAsync(AppCode, message, version, DateTime.Now);

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
                                Error = message,
                                Time = DateTime.Now,
                                Version = version.ToString()
                            });
                    }
                }

                if (mainWindow != null)
                {
                    mainWindow.Close();
                }

                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
            });

            errorReport.SendNoReport = new CustomCommand(arg =>
            {
                if (mainWindow != null)
                    mainWindow.Close();

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
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                try
                {
                    await _appServiceClient.SendErrorReportAsync(AppCode, errorMessage, version, DateTime.Now);
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

            if (_useAppService)
            {
                while (CommonSettings.Default.DelayedErrorsNew.Count > 0)
                {
                    var report = CommonSettings.Default.DelayedErrorsNew[0];

                    await _appServiceClient.SendErrorReportAsync(AppCode, report.Error, Version.Parse(report.Version), report.Time);
                    CommonSettings.Default.DelayedErrorsNew.RemoveAt(0);
                }
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
