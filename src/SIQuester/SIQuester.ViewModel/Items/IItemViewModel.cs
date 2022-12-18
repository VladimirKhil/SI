using SIPackages;
using System.Windows.Input;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a package item view model.
/// </summary>
public interface IItemViewModel
{
    bool IsSelected { get; set; }

    bool IsExpanded { get; set; }

    bool IsDragged { get; set; }

    InfoOwner GetModel();

    /// <summary>
    /// View model that owns current view model.
    /// </summary>
    IItemViewModel? Owner { get; }

    InfoViewModel Info { get; }

    ICommand Add { get; }

    ICommand Remove { get; }
}
