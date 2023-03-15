using SIQuester.Utilities;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace SIQuester;

/// <summary>
/// Represents text editor that allows to modify collections.
/// </summary>
[ContentProperty("Items")]
public sealed class TextList : TextBox
{
    /// <summary>
    /// Фрагмент текста редактора (элемент списка)
    /// </summary>
    private struct ItemInfo
    {
        /// <summary>
        /// Отображаемая длина
        /// </summary>
        internal int _length;

        /// <summary>
        /// Длина нередактируемой части у ссылки
        /// </summary>
        internal int _readOnlyLength;

        /// <summary>
        /// Может ли ссылка быть уточнена
        /// </summary>
        internal bool _canBeSpecified;

        public ItemInfo(int length, int readOnlyLength, bool canBeSpecified)
        {
            _length = length;
            _readOnlyLength = readOnlyLength;
            _canBeSpecified = canBeSpecified;
        }
    }

    private bool _blockChanges = false;
    
    private int _blockSelection = 0;

    private bool _blockNotificationsFlag = false;

    private readonly List<ItemInfo> _infos = new();

    private int _oldSelectionStart = -1, _oldSelectionLength = -1, _oldOffsetStart = -1;

    /// <summary>
    /// Разделитель, вставляемый между элементами
    /// </summary>
    [DefaultValue(", ")]
    public string ItemsSeparator { get; set; } = ", ";

    /// <summary>
    /// Набор элементов редактора
    /// </summary>
    public ICollectionView? Items { get; private set; }

    /// <summary>
    /// Источник элементов редактора
    /// </summary>
    public IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource", typeof(IList), typeof(TextList), new UIPropertyMetadata(null, OnItemsSourceChanged));

    public static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TextList)d;
        var oldValue = (IList)e.OldValue;
        var newValue = (IList)e.NewValue;

        if (e.NewValue == null && !BindingOperations.IsDataBound(d, ItemsSourceProperty))
        {
            control.Items = null;
        }
        else
        {
            control.Items = CollectionViewSource.GetDefaultView(control.ItemsSource);
            control.CurrentItem = control.ItemsSource.Cast<object>().FirstOrDefault();
            control.CurrentPosition = control.CurrentItem != null ? 0 : -1;

            if (control.IsSynchronizedWithCurrentItem == true)
            {
                control.Items.MoveCurrentToPosition(0);
            }
        }

        control.OnItemsSourceChanged(oldValue, newValue);
    }

    private void OnItemsSourceChanged(IList oldValue, IList newValue)
    {
        SetText();

        if (oldValue is INotifyCollectionChanged notifier)
        {
            notifier.CollectionChanged -= Notifier_CollectionChanged;
        }

        notifier = (INotifyCollectionChanged)newValue;

        if (notifier != null)
        {
            notifier.CollectionChanged += Notifier_CollectionChanged;
        }
    }

    private void Notifier_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_blockNotificationsFlag)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                OnItemsAdded(e);
                break;

            case NotifyCollectionChangedAction.Move:
                // Не обрабатывается
                break;

            case NotifyCollectionChangedAction.Remove:
                OnItemsRemoved(e);
                break;

            case NotifyCollectionChangedAction.Replace:
                OnItemsReplaced(e);
                break;

            case NotifyCollectionChangedAction.Reset:
                SetText();
                break;

            default:
                break;
        }

        Check();
    }

    public object? CurrentItem
    {
        get => GetValue(CurrentItemProperty);
        set => SetValue(CurrentItemProperty, value);
    }

    public static readonly DependencyProperty CurrentItemProperty =
        DependencyProperty.Register("CurrentItem", typeof(object), typeof(TextList), new PropertyMetadata(null));

    public int CurrentPosition
    {
        get => (int)GetValue(CurrentPositionProperty);
        set => SetValue(CurrentPositionProperty, value);
    }

    public static readonly DependencyProperty CurrentPositionProperty =
        DependencyProperty.Register("CurrentPosition", typeof(int), typeof(TextList), new PropertyMetadata(-1));

    private void OnItemsReplaced(NotifyCollectionChangedEventArgs e)
    {
        _blockSelection++;

        try
        {
            OnItemsRemoved(e);
        }
        finally
        {
            _blockSelection--;
        }

        OnItemsAdded(e);
    }

    private void OnItemsRemoved(NotifyCollectionChangedEventArgs e)
    {
        _blockChanges = true;

        try
        {
            _blockSelection++;

            try
            {
                var start = ConvertLocalOffsetToGlobalOffset(e.OldStartingIndex);
                var end = ConvertLocalOffsetToGlobalOffset(e.OldStartingIndex + e.OldItems.Count);

                if (e.OldStartingIndex == 0)
                {
                    end += ItemsSeparator.Length;
                }

                Select(start, end - start);
                _infos.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
            }
            finally
            {
                _blockSelection--;
            }

            SelectedText = "";
        }
        finally
        {
            _blockChanges = false;
        }

        if (IsFocused)
        {
            if (SelectionStart + ItemsSeparator.Length < Text.Length && SelectionStart > 0)
            {
                Select(SelectionStart + ItemsSeparator.Length, 0);
            }

            Keyboard.Focus(this);
        }
        else
        {
            Select(0, 0);
        }
    }

    private void Check()
    {
        if (ItemsSource.Count == 1 && _infos.Count == 1 && _infos[0]._readOnlyLength == -1)
        {
            if (Text != ItemsSource[0].ToString())
            {
                MessageBox.Show("Ошибка редактирования! Обратитесь к разработчику.");
                SetText();
            }
        }
    }

    private void OnItemsAdded(NotifyCollectionChangedEventArgs e)
    {
        _blockChanges = true;
        _blockSelection++;

        try
        {
            Select(ConvertLocalOffsetToGlobalOffset(e.NewStartingIndex), 0);
            var text = new StringBuilder();
            var index = e.NewStartingIndex;

            foreach (var item in e.NewItems)
            {
                if (index > 0)
                {
                    text.Append(ItemsSeparator);
                }

                var toAdd = CheckIsLink(index, item, out bool isLink, out bool canBeSpecified, out string tail);
                text.Append(toAdd);
                _infos.Insert(index++, new ItemInfo(toAdd.Length, isLink ? toAdd.Length : -1, canBeSpecified));
            }

            if (e.NewStartingIndex == 0 && index < _infos.Count)
            {
                text.Append(ItemsSeparator);
            }

            SelectedText = text.ToString();
        }
        finally
        {
            _blockChanges = false;
            _blockSelection--;
        }

        if (IsFocused)
        {
            Select(e.NewStartingIndex > 0 ? SelectionStart + ItemsSeparator.Length : SelectionStart, 0);
            Keyboard.Focus(this);
        }
        else
        {
            Select(0, 0);
        }
    }

    private string CheckIsLink(int index, object item, out bool isLink, out bool canBeSpecified, out string tail)
    {
        var toAdd = item.ToString();

        if (LinkManager != null)
        {
            var link = LinkManager.GetLinkText(ItemsSource, index, out canBeSpecified, out tail);

            if (link != null)
            {
                toAdd = "(" + link + ")";
                isLink = true;
            }
            else
            {
                isLink = false;
            }
        }
        else
        {
            tail = null;
            isLink = false;
            canBeSpecified = false;
        }

        return toAdd;
    }

    public bool? IsSynchronizedWithCurrentItem
    {
        get => (bool?)GetValue(IsSynchronizedWithCurrentItemProperty);
        set { SetValue(IsSynchronizedWithCurrentItemProperty, value); }
    }

    public static readonly DependencyProperty IsSynchronizedWithCurrentItemProperty =
        DependencyProperty.Register("IsSynchronizedWithCurrentItem", typeof(bool?), typeof(TextList), new UIPropertyMetadata(false));
    
    /// <summary>
    /// Менеджер,обеспечивающий возможность управления ссылками
    /// </summary>
    public ILinkManager LinkManager
    {
        get => (ILinkManager)GetValue(LinkManagerProperty);
        set { SetValue(LinkManagerProperty, value); }
    }

    public static readonly DependencyProperty LinkManagerProperty =
        DependencyProperty.Register("LinkManager", typeof(ILinkManager), typeof(TextList), new UIPropertyMetadata(null));
    
    public TextList()
    {
        DataObject.AddCopyingHandler(this, OnCopy);
        DataObject.AddPastingHandler(this, OnPaste);
        IsUndoEnabled = false;
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        var offset = ConvertGlobalOffsetToLocalOffset(SelectionStart, out int index);

        if (ItemsSource.Count == 0 || 
            _infos[index]._readOnlyLength > -1 &&
            (!_infos[index]._canBeSpecified || offset < _infos[index]._readOnlyLength))
        {
            e.CancelCommand();
        }
    }

    private void OnCopy(object sender, DataObjectCopyingEventArgs e)
    {
        var offset = ConvertGlobalOffsetToLocalOffset(SelectionStart, out int index);

        if (ItemsSource.Count == 0 ||
            _infos[index]._readOnlyLength > -1 &&
            (!_infos[index]._canBeSpecified || offset < _infos[index]._readOnlyLength))
        {
            e.CancelCommand();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var offset = ConvertGlobalOffsetToLocalOffset(SelectionStart, out int index);

        if (ItemsSource.Count == 0
            || SelectionLength == 0 && (e.Key == Key.Back && offset == 0
                || e.Key == Key.Delete && offset == _infos[index]._length)
            || _infos[index]._readOnlyLength > -1 && (!_infos[index]._canBeSpecified
                || offset < _infos[index]._readOnlyLength
                || offset == _infos[index]._readOnlyLength && e.Key == Key.Back && SelectionLength == 0)
            || e.Key == Key.Insert)
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (_blockChanges)
        {
            return;
        }

        int index = 0;

        foreach (var change in e.Changes)
        {
            var offset = ConvertGlobalOffsetToLocalOffset(change.Offset, out index);

            var origin = ItemsSource[index].ToString();
            _blockNotificationsFlag = true;

            if (offset + change.RemovedLength > origin.Length)
            {
                SetText();
                return;
            }

            try
            {
                if (_infos[index]._readOnlyLength > -1 && _infos[index]._canBeSpecified)
                {
                    var delta = offset - _infos[index]._readOnlyLength; // Offset in link clarification
                    var hashIndex = origin.IndexOf('#');
                    string newText = Text.Substring(change.Offset, change.AddedLength);

                    if (hashIndex > -1)
                    {
                        offset = hashIndex + delta + 1;
                    }
                    else
                    {
                        newText = "#" + newText;
                        offset = origin.Length;
                    }

                    ItemsSource[index] = string.Concat(origin.AsSpan(0, offset), newText, origin.AsSpan(offset + change.RemovedLength));
                    _infos[index] = new ItemInfo(ItemsSource[index].ToString().Length, _infos[index]._readOnlyLength, _infos[index]._canBeSpecified);
                }
                else
                {
                    ItemsSource[index] = string.Concat(origin.AsSpan(0, offset), Text.AsSpan(change.Offset, change.AddedLength), origin.AsSpan(offset + change.RemovedLength));
                    _infos[index] = new ItemInfo(ItemsSource[index].ToString().Length, _infos[index]._readOnlyLength, _infos[index]._canBeSpecified);
                }
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("origin: {0}, offset: {1}, cOffset: {2}, cAdded: {3}, cRemoved: {4}, text: {5}", origin, offset, change.Offset, change.AddedLength, change.RemovedLength, Text), exc);
            }
            finally
            {
                _blockNotificationsFlag = false;
            }
        }

        Check();
    }

    /// <summary>
    /// Converts global offset in text editor into local offset in current item.
    /// </summary>
    /// <param name="offset">Global offset value.</param>
    /// <param name="index">Current item index.</param>
    /// <returns>Current item offset.</returns>
    private int ConvertGlobalOffsetToLocalOffset(int offset, out int index)
    {
        if (ItemsSeparator == null)
        {
            throw new InvalidOperationException("ItemsSeparator == null");
        }

        if (ItemsSource == null)
        {
            throw new InvalidOperationException("ItemsSource == null");
        }

        index = 0;
        var length = 0;

        while (index < _infos.Count)
        {
            var add = _infos[index]._length + ItemsSeparator.Length;

            if (length + add <= offset)
            {
                length += add;
            }
            else
            {
                break;
            }

            index++;
        }

        var result = offset - length;

        if (index < 0 || index >= _infos.Count)
        {
            throw new InvalidOperationException(
                $"_infos = {string.Join(";", _infos.Select(info => info._length))}, offset = {offset}, index = {index}, result = {result}");
        }

        if (index < 0 || index >= ItemsSource.Count)
        {
            throw new InvalidOperationException(
                $"_infos = {string.Join(";", _infos.Select(info => info._length))}, ItemsSource.Count = {ItemsSource.Count}, offset = {offset}, index = {index}, result = {result}");
        }

        return result;
    }

    /// <summary>
    /// Преобразовать смещение элемента в глобальное смещение
    /// </summary>
    /// <param name="index"></param>
    private int ConvertLocalOffsetToGlobalOffset(int index)
    {
        var result = 0;

        for (var i = 0; i < index; i++)
        {
            result += _infos[i]._length;

            if (i + 1 < index)
            {
                result += ItemsSeparator.Length;
            }
        }

        return result;
    }

    protected override void OnSelectionChanged(RoutedEventArgs e)
    {
        base.OnSelectionChanged(e);

        // В данной версии просто запретим выделять более 1 элемента
        if (_blockSelection > 0 || ItemsSource == null || _infos.Count == 0)
        {
            return;
        }

        var offsetStart = ConvertGlobalOffsetToLocalOffset(SelectionStart, out int indexStart);
        var offsetEnd = ConvertGlobalOffsetToLocalOffset(SelectionStart + SelectionLength, out int indexEnd);

        if (indexStart >= _infos.Count)
        {
            return;
        }

        if (indexStart != indexEnd || ItemsSource != null && ItemsSource.Count > 0 && offsetEnd > _infos[indexStart]._length)
        {
            _blockSelection++;

            try
            {
                if (SelectionLength == 0)
                {
                    if (SelectionStart + 1 == _oldSelectionStart)
                    {
                        if (SelectionStart > 0)
                        {
                            SelectionStart--;
                        }
                    }
                    else
                    {
                        SelectionStart++;
                    }

                    _oldSelectionStart = SelectionStart;
                    _oldSelectionLength = SelectionLength;
                    _oldOffsetStart = ConvertGlobalOffsetToLocalOffset(SelectionStart, out indexStart);
                }
                else if (SelectionStart < _oldSelectionStart)
                {
                    // Выделение идёт справа налево
                    SelectionStart = Math.Max(0, _oldSelectionStart - _oldOffsetStart);
                    SelectionLength = _oldSelectionLength + _oldOffsetStart;

                    _oldSelectionStart = SelectionStart;
                    _oldSelectionLength = SelectionLength;

                    ConvertGlobalOffsetToLocalOffset(SelectionStart, out indexStart);
                }
                else if (SelectionStart == _oldSelectionStart)
                {
                    // Выделение идёт слева направо
                    SelectionLength = Math.Max(0, _infos[indexStart]._length - _oldOffsetStart);
                }
                else
                {
                    SelectionLength = 0;

                    _oldSelectionStart = SelectionStart;
                    _oldSelectionLength = SelectionLength;
                    _oldOffsetStart = offsetStart;
                }
            }
            finally
            {
                _blockSelection--;
                e.Handled = true;
            }
        }
        else
        {
            _oldSelectionStart = SelectionStart;
            _oldSelectionLength = SelectionLength;
            _oldOffsetStart = offsetStart;
        }

        if (ItemsSource != null && indexStart < ItemsSource.Count)
        {
            CurrentItem = ItemsSource[indexStart];
            CurrentPosition = indexStart;
        }

        if (IsSynchronizedWithCurrentItem == true && Items != null)
        {
            Items.MoveCurrentToPosition(indexStart);
        }
    }

    /// <summary>
    /// Задать изначальный текст в редакторе
    /// </summary>
    private void SetText()
    {
        _blockChanges = true;

        try
        {
            if (Items == null)
            {
                Text = "";
                return;
            }

            var text = new StringBuilder();
            _infos.Clear();
            int index = 0;
            var isFirst = true;

            foreach (var item in Items.Cast<string>())
            {
                if (!isFirst)
                {
                    text.Append(ItemsSeparator);
                }

                var toAdd = CheckIsLink(index, item, out bool isLink, out bool canBeExtended, out string tail);
                text.Append(toAdd);

                var length = toAdd.Length;

                if (!string.IsNullOrEmpty(tail))
                {
                    text.Append(tail);
                    length += tail.Length;
                }

                _infos.Add(new ItemInfo(length, isLink ? toAdd.Length : -1, canBeExtended));
                index++;

                isFirst = false;
            }

            Text = text.ToString();
        }
        finally
        {
            _blockChanges = false;
        }
    }

    internal Tuple<int, int, int> GetSelectionInfo()
    {
        var start = SelectionStart;
        var index = 0;

        while (index < _infos.Count && start > _infos[index]._length)
        {
            start -= _infos[index]._length + 2;
            index++;
        }

        return Tuple.Create(index, start, SelectionLength);
    }
}
