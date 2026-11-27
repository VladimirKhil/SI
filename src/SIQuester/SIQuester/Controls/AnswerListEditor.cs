using SIQuester.ViewModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SIQuester;

/// <summary>
/// Edits a list of answers as separate fields (one per answer) with "remove" next to each field
/// and "add" next to the last one. Edits go straight to the answers collection, so undo and the action
/// toolbar keep working.
/// </summary>
/// <remarks>
/// Enter adds a new answer below the current one; Backspace in an empty field removes it.
/// </remarks>
public sealed class AnswerListEditor : StackPanel
{
    public static readonly DependencyProperty AnswersProperty = DependencyProperty.Register(
        nameof(Answers),
        typeof(AnswersViewModel),
        typeof(AnswerListEditor),
        new PropertyMetadata(null, OnAnswersChanged));

    /// <summary>
    /// Answers being edited.
    /// </summary>
    public AnswersViewModel? Answers
    {
        get => (AnswersViewModel?)GetValue(AnswersProperty);
        set => SetValue(AnswersProperty, value);
    }

    public static readonly DependencyProperty FieldStyleProperty = DependencyProperty.Register(
        nameof(FieldStyle), typeof(Style), typeof(AnswerListEditor), new PropertyMetadata(null, (d, _) => ((AnswerListEditor)d).Rebuild()));

    /// <summary>
    /// Style of a single answer field.
    /// </summary>
    public Style? FieldStyle
    {
        get => (Style?)GetValue(FieldStyleProperty);
        set => SetValue(FieldStyleProperty, value);
    }

    public static readonly DependencyProperty FieldBackgroundProperty = DependencyProperty.Register(
        nameof(FieldBackground), typeof(Brush), typeof(AnswerListEditor), new PropertyMetadata(null, (d, _) => ((AnswerListEditor)d).Rebuild()));

    /// <summary>
    /// Background of the answer fields.
    /// </summary>
    public Brush? FieldBackground
    {
        get => (Brush?)GetValue(FieldBackgroundProperty);
        set => SetValue(FieldBackgroundProperty, value);
    }

    private readonly List<TextBox> _fields = new();

    private bool _isUpdatingCollection;

    /// <summary>
    /// The field edited last: toolbar commands keep working with its selection after the focus moves to the toolbar.
    /// </summary>
    private TextBox? _lastField;

    public AnswerListEditor()
    {
        Orientation = Orientation.Vertical;
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Selected text of the edited answer: (answer index, selection start, selection length).
    /// Used by the answer commands that work with the selection (e.g. move the selected part to a new answer).
    /// </summary>
    internal Tuple<int, int, int>? GetSelectionInfo()
    {
        var field = _fields.FirstOrDefault(f => f.IsKeyboardFocusWithin) ?? _lastField;

        if (field == null || !_fields.Contains(field))
        {
            return null;
        }

        return Tuple.Create((int)field.Tag, field.SelectionStart, field.SelectionLength);
    }

    /// <summary>
    /// Focuses the first field when these answers have just been created (e.g. "Add wrong answers"),
    /// as the previous single text editor did through <see cref="ActivateManager" />.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Answers != null && ReferenceEquals(QDocument.ActivatedObject, Answers))
        {
            QDocument.ActivatedObject = null;
            FocusField(0);
        }
    }

    private static void OnAnswersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (AnswerListEditor)d;

        if (e.OldValue is AnswersViewModel oldAnswers)
        {
            oldAnswers.CollectionChanged -= editor.Answers_CollectionChanged;
        }

        if (e.NewValue is AnswersViewModel newAnswers)
        {
            newAnswers.CollectionChanged += editor.Answers_CollectionChanged;
        }

        editor.Rebuild();
    }

    private void Answers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdatingCollection)
        {
            return; // our own text edit: the field already shows it
        }

        if (e.Action == NotifyCollectionChangedAction.Replace && Answers != null && e.NewStartingIndex < _fields.Count)
        {
            // Undo/redo or an edit from elsewhere: update the text in place (keeps focus)
            var field = _fields[e.NewStartingIndex];
            var value = Answers[e.NewStartingIndex];

            if (field.Text != value)
            {
                field.Text = value;
            }

            return;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        var focusedIndex = _fields.FindIndex(f => f.IsKeyboardFocusWithin);

        Children.Clear();
        _fields.Clear();

        var answers = Answers;

        if (answers == null)
        {
            return;
        }

        for (var i = 0; i < answers.Count; i++)
        {
            Children.Add(CreateRow(answers, i, i == answers.Count - 1));
        }

        if (focusedIndex > -1)
        {
            FocusField(Math.Min(focusedIndex, _fields.Count - 1));
        }
    }

    private UIElement CreateRow(AnswersViewModel answers, int index, bool isLast)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 2), LastChildFill = true };

        // "+" only on the last row, but every row keeps its place so the fields line up
        var add = CreateButton("", Properties.Resources.AddAnswer, () => AddAnswer(answers));
        add.Visibility = isLast ? Visibility.Visible : Visibility.Hidden;
        DockPanel.SetDock(add, Dock.Right);
        row.Children.Add(add);

        var remove = CreateButton("", Properties.Resources.RemoveAnswer, () => RemoveAnswer(answers, index));
        DockPanel.SetDock(remove, Dock.Right);
        row.Children.Add(remove);

        var field = new TextBox
        {
            Text = answers[index],
            Style = FieldStyle,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(3),
            MinWidth = 75,
            Tag = index,
        };

        if (FieldBackground != null)
        {
            field.Background = FieldBackground;
        }

        field.TextChanged += Field_TextChanged;
        field.GotKeyboardFocus += (_, _) =>
        {
            _lastField = field;
            answers.CurrentPosition = (int)field.Tag;
        };
        field.PreviewKeyDown += Field_PreviewKeyDown;
        CommandManager.AddPreviewCanExecuteHandler(field, Field_PreviewCanExecute);
        CommandManager.AddPreviewExecutedHandler(field, Field_PreviewExecuted);

        _fields.Add(field);
        row.Children.Add(field);

        return row;
    }

    private Button CreateButton(string glyph, string toolTip, Action action)
    {
        var button = new Button
        {
            Content = glyph,
            ToolTip = toolTip,
            Focusable = false,
        };

        if (TryFindResource("AnswerRowButton") is Style style)
        {
            button.Style = style;
        }

        button.Click += (_, _) => action();
        return button;
    }

    private void Field_TextChanged(object sender, TextChangedEventArgs e)
    {
        var field = (TextBox)sender;
        var answers = Answers;
        var index = (int)field.Tag;

        if (answers == null || index >= answers.Count || answers[index] == field.Text)
        {
            return;
        }

        _isUpdatingCollection = true;

        try
        {
            answers[index] = field.Text;
        }
        finally
        {
            _isUpdatingCollection = false;
        }
    }

    /// <summary>
    /// Whether the clipboard holds a picture or files and no text.
    /// </summary>
    private static bool ClipboardHasOnlyMedia()
    {
        try
        {
            return !Clipboard.ContainsText() && (Clipboard.ContainsImage() || Clipboard.ContainsFileDropList());
        }
        catch
        {
            return false; // clipboard is busy: let the text field handle it
        }
    }

    /// <summary>
    /// A text field says "nothing to paste" for a picture, so Ctrl+V would go nowhere.
    /// For right answers allow it: the window puts the media into the answer (as PasteBehavior did for the single editor).
    /// </summary>
    private void Field_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Paste && Answers?.IsRight == true && ClipboardHasOnlyMedia())
        {
            e.CanExecute = true;
            e.Handled = true;
        }
    }

    private void Field_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command != ApplicationCommands.Paste
            || Answers?.IsRight != true
            || !ClipboardHasOnlyMedia()
            || Application.Current.MainWindow is not IInputElement window)
        {
            return;
        }

        e.Handled = true;
        ApplicationCommands.Paste.Execute(null, window);
    }

    private void Field_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var field = (TextBox)sender;
        var answers = Answers;

        if (answers == null)
        {
            return;
        }

        var index = (int)field.Tag;

        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            AddAnswer(answers, index + 1);
        }
        else if (e.Key == Key.Back && field.Text.Length == 0 && answers.Count > 1)
        {
            e.Handled = true;
            RemoveAnswer(answers, index);
            FocusField(Math.Max(0, index - 1), atEnd: true);
        }
    }

    private void AddAnswer(AnswersViewModel answers, int? position = null)
    {
        var index = Math.Clamp(position ?? answers.Count, 0, answers.Count);
        answers.Insert(index, "");
        FocusField(index);
    }

    private static void RemoveAnswer(AnswersViewModel answers, int index)
    {
        if (index >= answers.Count)
        {
            return;
        }

        // Goes through the view model command: keeps its rules (e.g. at least one right answer) and undo grouping
        answers.CurrentPosition = index;

        if (answers.RemoveItem.CanExecute(null))
        {
            answers.RemoveItem.Execute(null);
        }
    }

    private void FocusField(int index, bool atEnd = false)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (index < 0 || index >= _fields.Count)
            {
                return;
            }

            var field = _fields[index];
            field.Focus();

            if (atEnd)
            {
                field.CaretIndex = field.Text.Length;
            }
        }, System.Windows.Threading.DispatcherPriority.Input);
    }
}
