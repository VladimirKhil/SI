using SIPackages;
using SIQuester.Contracts;
using SIQuester.Helpers;
using SIQuester.Model;
using SIQuester.ViewModel;
using System.Text;
using System.Windows;
using System.Xml;

namespace SIQuester.Implementation;

internal static class DragManager
{
    private const string FormatMarker = "1";

    internal static void DoDrag(
        FrameworkElement host,
        QDocument active,
        InfoOwner item,
        InfoOwnerData itemData,
        Action? beforeDrag = null,
        Action? afterDrag = null)
    {
        ArgumentNullException.ThrowIfNull(host, nameof(host));
        ArgumentNullException.ThrowIfNull(active, nameof(active));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(itemData, nameof(itemData));

        var dataObject = new DataObject(itemData);

        if (host.DataContext is RoundViewModel roundViewModel)
        {
            var packageViewModel = roundViewModel.OwnerPackage;

            if (packageViewModel == null)
            {
                throw new ArgumentException(nameof(packageViewModel));
            }

            roundViewModel.IsDragged = true;

            int index = packageViewModel.Rounds.IndexOf(roundViewModel);

            using var change = active.OperationsManager.BeginComplexChange();

            try
            {
                var sb = new StringBuilder();

                using (var writer = XmlWriter.Create(sb))
                {
                    roundViewModel.Model.WriteXml(writer);
                }

                dataObject.SetData(WellKnownDragFormats.Round, FormatMarker);
                dataObject.SetData(DataFormats.Serializable, sb);

                var result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);

                if (result == DragDropEffects.Move)
                {
                    if (packageViewModel.Rounds[index] != roundViewModel)
                    {
                        index++;
                    }

                    packageViewModel.Rounds.RemoveAt(index);
                    change.Commit();
                }
            }
            finally
            {
                roundViewModel.IsDragged = false;
            }
        }
        else
        {
            if (host.DataContext is ThemeViewModel themeViewModel)
            {
                roundViewModel = themeViewModel.OwnerRound;

                if (roundViewModel == null)
                {
                    throw new ArgumentException(nameof(roundViewModel));
                }

                themeViewModel.IsDragged = true;

                int index = roundViewModel.Themes.IndexOf(themeViewModel);

                using var change = active.OperationsManager.BeginComplexChange();

                try
                {
                    var sb = new StringBuilder();

                    using (var writer = XmlWriter.Create(sb))
                    {
                        themeViewModel.Model.WriteXml(writer);
                    }

                    dataObject.SetData(WellKnownDragFormats.Theme, FormatMarker);
                    dataObject.SetData(DataFormats.Serializable, sb);

                    var result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);

                    if (result == DragDropEffects.Move)
                    {
                        if (roundViewModel.Themes[index] != themeViewModel)
                        {
                            index++;
                        }

                        roundViewModel.Themes.RemoveAt(index);
                    }

                    change.Commit();
                }
                finally
                {
                    themeViewModel.IsDragged = false;
                }
            }
            else
            {
                var questionViewModel = host.DataContext as QuestionViewModel;
                themeViewModel = questionViewModel.OwnerTheme;

                if (themeViewModel == null)
                {
                    throw new ArgumentException(nameof(themeViewModel));
                }

                questionViewModel.IsDragged = true;

                var index = themeViewModel.Questions.IndexOf(questionViewModel);
                using var change = active.OperationsManager.BeginComplexChange();

                try
                {
                    var sb = new StringBuilder();

                    using (var writer = XmlWriter.Create(sb))
                    {
                        questionViewModel.Model.WriteXml(writer);
                    }

                    dataObject.SetData(WellKnownDragFormats.Question, FormatMarker);
                    dataObject.SetData(DataFormats.Serializable, sb);

                    beforeDrag?.Invoke();

                    DragDropEffects result;

                    try
                    {
                        result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);
                    }
                    catch (InvalidOperationException)
                    {
                        result = DragDropEffects.None;
                    }
                    finally
                    {
                        host.Opacity = 1.0;

                        afterDrag?.Invoke();
                    }

                    if (result == DragDropEffects.Move)
                    {
                        if (themeViewModel.Questions[index] != questionViewModel)
                        {
                            index++;
                        }

                        var currentPrices = themeViewModel.CapturePrices();

                        themeViewModel.Questions.RemoveAt(index);

                        if (AppSettings.Default.ChangePriceOnMove)
                        {
                            themeViewModel.ResetPrices(currentPrices);
                        }
                    }

                    change.Commit();
                }
                finally
                {
                    questionViewModel.IsDragged = false;
                }
            }
        }
    }
}
