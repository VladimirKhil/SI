using SIGame.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System;
using SIGame.Helpers;

namespace SIGame;

/// <summary>
/// Handles global game exceptions.
/// </summary>
internal sealed class ExceptionHandler
{
    private readonly IErrorManager _errorManager;

    public ExceptionHandler(IErrorManager errorManager)
    {
        EnsureThat.EnsureArg.IsNotNull(errorManager);

        _errorManager = errorManager;
    }

    internal bool Handle(Exception exception)
    {
        try
        {
            EnsureThat.EnsureArg.IsNotNull(exception);

            var inner = exception;

            while (inner.InnerException != null)
            {
                inner = inner.InnerException;
            }

            if (inner is OutOfMemoryException)
            {
                MessageBox.Show(
                    Properties.Resources.Error_IncifficientResourcesForExecution,
                    AppConstants.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (exception is System.Windows.Markup.XamlParseException
                && inner is BadImageFormatException
                && inner.Message.Contains("WebView2Behavior"))
            {
                var error = string.Format(Properties.Resources.WebViewWrongFormat, Environment.Is64BitProcess);

                MessageBox.Show(
                    $"{error}: {inner.Message}",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return true;
            }

            if (inner is System.Windows.Markup.XamlParseException
                || inner is System.Xaml.XamlParseException
                || inner is NotImplementedException
                || inner is TypeInitializationException
                || inner is FileFormatException
                || inner is SEHException)
            {
                MessageBox.Show(
                    $"{Properties.Resources.Error_RuntimeBroken}: {inner.Message}",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (inner is FileNotFoundException)
            {
                MessageBox.Show(
                    $"{Properties.Resources.Error_FilesBroken}: {inner.Message}. {Properties.Resources.TryReinstallApp}.",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (inner is COMException)
            {
                MessageBox.Show(
                    $"{Properties.Resources.Error_DirectXBroken}: {inner.Message}.",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (inner is FileLoadException
                || inner is IOException
                || inner is ArgumentOutOfRangeException && inner.Message.Contains("capacity"))
            {
                MessageBox.Show(inner.Message, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var message = exception.ToString();

            if (message.Contains("System.Windows.Automation")
                || message.Contains("UIAutomationCore.dll")
                || message.Contains("UIAutomationTypes"))
            {
                MessageBox.Show(
                    Properties.Resources.Error_WindowsAutomationBroken,
                    AppConstants.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (message.Contains("ApplyTaskbarItemInfo")
                || message.Contains("GetValueFromTemplatedParent")
                || message.Contains("IsBadSplitPosition")
                || message.Contains("IKeyboardInputProvider.AcquireFocus")
                || message.Contains("ReleaseOnChannel")
                || message.Contains("ManifestSignedXml2.GetIdElement"))
            {
                MessageBox.Show(
                    Properties.Resources.Error_OSBroken,
                    AppConstants.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (message.Contains("ComputeTypographyAvailabilities") || message.Contains("FontList.get_Item"))
            {
                MessageBox.Show(
                    Properties.Resources.Error_Typography,
                    AppConstants.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (message.Contains("WpfXamlLoader.TransformNodes"))
            {
                MessageBox.Show(
                    Properties.Resources.AppBroken,
                    AppConstants.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            _errorManager.SendErrorReport(exception);

            return true;
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                exc.ToString(),
                AppConstants.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return false;
        }
    }
}
