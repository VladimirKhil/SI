using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using Steamworks;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents a Steam Workshop item view model.
/// </summary>
public sealed class WorkshopItemViewModel : ModelViewBase
{
    private readonly WorkshopItem _model = new();

    /// <summary>
    /// Steam published file ID.
    /// </summary>
    public PublishedFileId_t PublishedFileId
    {
        get => _model.PublishedFileId;
        set
        {
            if (_model.PublishedFileId != value)
            {
                _model.PublishedFileId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Url));
            }
        }
    }

    /// <summary>
    /// Item title/name.
    /// </summary>
    public string Title
    {
        get => _model.Title;
        set
        {
            if (_model.Title != value)
            {
                _model.Title = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Item description.
    /// </summary>
    public string Description
    {
        get => _model.Description;
        set
        {
            if (_model.Description != value)
            {
                _model.Description = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Item tags.
    /// </summary>
    public string Tags
    {
        get => _model.Tags;
        set
        {
            if (_model.Tags != value)
            {
                _model.Tags = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Workshop item URL.
    /// </summary>
    public string Url => _model.Url;

    /// <summary>
    /// Command to open item in browser.
    /// </summary>
    public SimpleCommand OpenInBrowser { get; }

    public WorkshopItemViewModel(PublishedFileId_t publishedFileId)
    {
        PublishedFileId = publishedFileId;
        Title = "...";
        
        OpenInBrowser = new SimpleCommand(OpenInBrowser_Executed);
    }

    private void OpenInBrowser_Executed(object? arg)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors
        }
    }
}