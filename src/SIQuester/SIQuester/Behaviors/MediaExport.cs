using SIQuester.ViewModel;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.PlatformSpecific;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VmResources = SIQuester.ViewModel.Properties.Resources;

namespace SIQuester.Behaviors;

/// <summary>
/// Context menu "Unload media…" on a picture, sound or video of a question and on the package logo:
/// saves the file from the package to a place the user picks (the same dialog as the toolbar media export).
/// The media comes from the element's DataContext (<see cref="ContentItemViewModel" />, or <see cref="PackageViewModel" /> for the logo).
/// Usage: <c>lb:MediaExport.IsEnabled="True"</c>.
/// </summary>
public static class MediaExport
{
    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(MediaExport), new PropertyMetadata(false, OnIsEnabledChanged));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not true)
        {
            return;
        }

        var save = new MenuItem { Header = Properties.Resources.ExportMediaCommand };

        if (element.TryFindResource("ExportMediaImage") is ImageSource icon)
        {
            save.Icon = new Image { Source = icon, Width = 16, Height = 16 };
        }

        save.Click += (_, _) => Save(element.DataContext);

        var menu = new ContextMenu();
        menu.Items.Add(save);

        // Only when there is a file in the package to save (not for a web link or an empty logo)
        menu.Opened += (_, _) => save.IsEnabled = TryGetMedia(element.DataContext, out _, out _);

        element.ContextMenu = menu;
    }

    private static bool TryGetMedia(object? context, out IMedia? media, out string fileName)
    {
        media = null;
        fileName = "";

        switch (context)
        {
            case ContentItemViewModel item when item.Model.IsRef:
                media = item.MediaSource;
                fileName = item.Model.Value;
                break;

            case PackageViewModel package when package.Logo != null:
                media = package.Logo;
                fileName = package.Model.Logo?.TrimStart('@') ?? "";
                break;
        }

        return media != null && (media.GetStream != null || File.Exists(media.Uri));
    }

    private static void Save(object? context)
    {
        if (!TryGetMedia(context, out var media, out var name) || media == null)
        {
            return;
        }

        string? fileName = Path.GetFileName(name);

        if (!PlatformManager.Instance.ShowSaveUI(VmResources.ExportMedia, "", null, ref fileName))
        {
            return;
        }

        try
        {
            if (media.GetStream != null)
            {
                using var source = media.GetStream().Stream;
                using var target = File.Create(fileName);
                source.CopyTo(target);
            }
            else
            {
                File.Copy(media.Uri, fileName, overwrite: true);
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage($"{VmResources.ExportMediaError}: {exc.Message}");
        }
    }
}
