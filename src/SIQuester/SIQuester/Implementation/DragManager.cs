﻿using SIQuester.Contracts;
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
        InfoOwnerData itemData,
        Action? beforeDrag = null,
        Action? afterDrag = null)
    {
        var dataObject = new DataObject(itemData);

        try
        {
            if (host.DataContext is RoundViewModel roundViewModel)
            {
                HandleRoundDrag(host, active, roundViewModel, dataObject);
            }
            else if (host.DataContext is ThemeViewModel themeViewModel)
            {
                HandleThemeDrag(host, active, themeViewModel, dataObject);
            }
            else if (host.DataContext is QuestionViewModel questionViewModel)
            {
                HandleQuestionDrag(host, active, questionViewModel, dataObject, beforeDrag, afterDrag);
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, AppSettings.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void HandleRoundDrag(FrameworkElement host, QDocument active, RoundViewModel roundViewModel, DataObject dataObject)
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

    private static void HandleThemeDrag(FrameworkElement host, QDocument active, ThemeViewModel themeViewModel, DataObject dataObject)
    {
        var roundViewModel = themeViewModel.OwnerRound;

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

    private static void HandleQuestionDrag(
        FrameworkElement host,
        QDocument active,
        QuestionViewModel questionViewModel,
        DataObject dataObject,
        Action? beforeDrag,
        Action? afterDrag)
    {
        var themeViewModel = questionViewModel.OwnerTheme;

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
