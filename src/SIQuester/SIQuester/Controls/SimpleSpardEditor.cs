using Lingware.Spard.Expressions;
using SIQuester.Model;
using SIQuester.ViewModel;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SIQuester;

public sealed class SimpleSpardEditor : RichTextBox
{
    private Sequence _rootExpression = null;
    private readonly Dictionary<Inline, Expression> _indexTable = new();
    private bool _updateFlag = false;

    private SpardTemplateViewModel _spardViewModel = null;
    private readonly Paragraph _paragraph = null;

    private readonly object _sync = new();

    private bool _selFlag = false;

    public string Spard
    {
        get { return (string)GetValue(SpardProperty); }
        set { SetValue(SpardProperty, value); }
    }

    public static readonly System.Windows.DependencyProperty SpardProperty =
        System.Windows.DependencyProperty.Register(
            "Spard",
            typeof(string),
            typeof(SimpleSpardEditor),
            new System.Windows.UIPropertyMetadata(null, OnSpardChanged));
    
    public static void OnSpardChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        try
        {
            var editor = (SimpleSpardEditor)d;
            if (!editor._updateFlag)
                editor.SetSpard((string)e.NewValue);
        }
        catch (Exception exc)
        {
            MainViewModel.ShowError(exc);
        }
    }

    private void SetSpard(string value)
    {
        if (_spardViewModel == null)
            return;

        var expr = ExpressionBuilder.Parse(value, false);

        _rootExpression = expr as Sequence;

        if (_rootExpression == null)
        {
            if (expr != null)
                _rootExpression = new Sequence(expr);
            else
                _rootExpression = new Sequence(Array.Empty<Expression>());
        }

        UpdateSpard();
    }

    public void BuildSpard()
    {
        _updateFlag = true;
        try
        {
            Spard = _rootExpression == null ? "" : _rootExpression.ToString();
        }
        finally
        {
            _updateFlag = false;
        }
    }

    public SimpleSpardEditor()
    {
        DataContextChanged += SimpleSpardEditor_DataContextChanged;
        Document = new FlowDocument();
        _paragraph = new Paragraph();
        Document.Blocks.Add(_paragraph);
        _paragraph.Inlines.Add(new Run());

        IsUndoEnabled = false;

        _rootExpression = new Sequence(Array.Empty<Expression>());
    }

    private void SimpleSpardEditor_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        _spardViewModel = DataContext as SpardTemplateViewModel;

        if (_spardViewModel == null && DataContext != null)
        {
            System.Windows.FrameworkElement parent = this;

            do
            {
                parent = VisualTreeHelper.GetParent(parent) as System.Windows.FrameworkElement;

                if (parent == null)
                    return;

                _spardViewModel = parent.DataContext as SpardTemplateViewModel;

                if (_spardViewModel != null)
                    break;

            } while (true);
        }

        if (_spardViewModel != null)
        {
            _spardViewModel.AliasInserted += SpardViewModel_AliasInserted;
            _spardViewModel.OptionalInserted += SpardViewModel_OptionalInserted;
        }
    }

    private void SpardViewModel_OptionalInserted() => InsertOptional(new Optional(new Sequence(Array.Empty<Expression>())));

    private Expression[] GetTopExpressions(Inline[] inlines)
    {
        var result = new List<Expression>();
        var depth = 0;

        for (int i = 0; i < inlines.Length; i++)
        {
            var expr = GetExpression(inlines[i]);

            if (depth == 0)
            {
                if (expr is Optional)
                {
                    depth++;
                }

                result.Add(expr);
            }
            else
            {
                if (expr is Optional)
                {
                    if (GetRunInContainer((InlineUIContainer)inlines[i]).Text == "(")
                        depth++;
                    else
                        depth--;
                }
            }
        }

        return result.ToArray();
    }

    private void InsertOptional(Optional optional)
    {
        if (Selection.IsEmpty)
        {
            InsertExpression(optional);
            return;
        }

        InsertExpression(Selection.Start, optional);
        InsertExpression(Selection.End, optional);

        var runs = GetAllSelectedInlines(out int leftIndex, out int rightIndex);
        var l = runs.Length;

        var parent = GetParentExpression(optional);
        var topExpressions = GetTopExpressions(runs.Where(run => _indexTable[run] != optional).ToArray());

        var newOperands = parent.Operands().Where(exp => !topExpressions.Contains(exp)).ToArray();

        // Удалим второй Optional из parent.Operands
        var index = Array.LastIndexOf(newOperands, optional);
        parent.SetOperands(newOperands.Where((exc, ind) => ind != index).ToArray());

        ((Sequence)optional.Operand).SetOperands(topExpressions);

        SpardChanged();
    }

    private Expression GetParentExpression(Expression expression)
    {
        var chain = GetExpressionChain(expression);
        if (chain.Length < 2)
            return _rootExpression;

        return chain[1];
    }

    private void CheckIfEmpty()
    {
        if (_indexTable.Count == 0)
        {
            _paragraph.Inlines.Clear();
            _paragraph.Inlines.Add(new Run());
            _rootExpression = new Sequence(Array.Empty<Expression>());
        }
    }

    private void SpardViewModel_AliasInserted(string aliasName)
    {
        var expr = aliasName == "Some" ? (Expression)new Set(aliasName) : new Instruction(new StringValue("m"), new Set(aliasName));
        InsertExpression(expr);
    }

    private void InsertExpression(Expression expression) => InsertExpression(Selection.Start, expression);

    private void InsertExpression(TextPointer pointer, Expression expression)
    {
        if (expression is not Optional)
        {
            RemoveSelection();
            Selection.Text = "";
        }

        var active = GetSelectedInline(pointer, out int leftIndex);
        var expr = GetExpression(active);

        InsertNewExpression(pointer, active, leftIndex, expr, expression);

        SpardChanged();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                InsertText(" ");
                e.Handled = true;
                break;

            case Key.Return:
                var set = new Set("Line");
                InsertExpression(set);
                var nextPos = Selection.End.GetPositionAtOffset(1, LogicalDirection.Forward);
                Selection.Select(nextPos, nextPos);
                e.Handled = true;
                return;

            case Key.Back:
                if (DeleteBackward())
                {
                    SpardChanged();
                    return;
                }
                break;

            case Key.Delete:
                if (DeleteForward())
                {
                    SpardChanged();
                    return;
                }
                break;

            case Key.Insert:
                e.Handled = true;
                return;

            case Key.X:
            case Key.C:
            case Key.V:
                if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                {
                    e.Handled = true;
                    return;
                }
                break;

            default:
                break;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        lock (_sync)
        {
            e.Handled = InsertText(e.Text);
            base.OnPreviewTextInput(e);
        }
    }

    protected override void OnSelectionChanged(System.Windows.RoutedEventArgs e)
    {
        if (!Selection.IsEmpty && !_selFlag)
        {
            var runs = GetAllSelectedInlines(out int leftIndex, out int rightIndex);

            var optionals = runs.Select(inline => GetExpression(inline)).OfType<Optional>();
            var singleOptionals = optionals.GroupBy(opt => opt).Where(g => g.Count() == 1).ToArray();

            if (singleOptionals.Length > 0)
            {
                var start = Selection.Start;
                var end = Selection.End;

                foreach (var item in singleOptionals)
                {
                    var cont = _indexTable.Where(es => es.Value == item.Key && !runs.Contains(es.Key)).First().Key;

                    if (start.CompareTo(cont.ContentStart) > 0)
                        start = cont.ContentStart;
                    if (end.CompareTo(cont.ContentEnd) < 0)
                        end = cont.ContentEnd;
                }

                _selFlag = true;

                try
                {
                    Selection.Select(start, end);
                    return;
                }
                finally
                {
                    _selFlag = false;
                }
            }
        }

        base.OnSelectionChanged(e);
    }

    protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        BuildSpard();
        base.OnPreviewLostKeyboardFocus(e);
    }

    /// <summary>
    /// Добавить текст
    /// </summary>
    /// <param name="text"></param>
    private bool InsertText(string text)
    {
        RemoveSelection();
        var active = GetSelectedInline(out int leftIndex);

        if (active != null && active.NextInline is Run && leftIndex == 1)
        {
            active = active.NextInline;
            leftIndex = 0;
        }
        
        var expr = GetExpression(active);

        bool result;

        if (active is InlineUIContainer || active is LineBreak)
        {
            var instruction = CreateInstruction(text);
            InsertNewExpression(active, leftIndex, expr, instruction);
            result = true;
        }
        else
        {
            if (expr is Instruction instruct && instruct.Argument is StringValue stringValueInner)
            {
                var stringValue = stringValueInner;
                var value = stringValue.Value;

                stringValue.Value = string.Concat(value.AsSpan(0, leftIndex), text, value.AsSpan(leftIndex));

                if (CaretPosition.LogicalDirection == LogicalDirection.Forward)
                    CaretPosition.InsertTextInRun(text);
                else
                {
                    CaretPosition.InsertTextInRun(text);
                    CaretPosition = CaretPosition.GetPositionAtOffset(text.Length, LogicalDirection.Forward);
                }

                result = true;
            }
            else
            {
                _indexTable.Clear();

                var instruction = CreateInstruction(text);
                InsertExpressionAtTheBeginning(instruction, _rootExpression);
                result = true;
            }
        }

        SpardChanged();
        return result;
    }

    private static Run GetRunInContainer(InlineUIContainer container) => (Run)((TextBlock)container.Child).Inlines.FirstInline;

    private void InsertNewExpression(Inline inline, int leftIndex, Expression previous, Expression expression) =>
        InsertNewExpression(Selection.Start, inline, leftIndex, previous, expression);

    private void InsertNewExpression(TextPointer pointer, Inline inline, int leftIndex, Expression current, Expression expression)
    {
        var prevInline = Selection.IsEmpty || pointer == Selection.End ? inline : inline.PreviousInline;

        if (leftIndex == 0 && (prevInline == null || prevInline.PreviousInline == null))
        {
            InsertExpressionAtTheBeginning(expression, _rootExpression);
            return;
        }

        var previous = leftIndex > 0 || Selection.IsEmpty || pointer == Selection.End ? current : GetExpression(inline.PreviousInline);

        if (previous is Optional && GetRunInContainer((InlineUIContainer)prevInline).Text == "(" && leftIndex == 1)
        {
            InsertExpressionAtTheBeginning(expression, (Sequence)previous.Operands().First());
        }
        else
        {
            var chain = GetExpressionChain(previous);
            var sequence = _rootExpression;

            if (chain.Length > 0)
            {
                previous = chain[^1];

                for (int i = 0; i < chain.Length; i++)
                {
                    if (chain[i] is Sequence seq)
                    {
                        sequence = seq;

                        if (i > 0)
                        {
                            previous = chain[i - 1];
                        }

                        break;
                    }
                }
            }

            InsertExpressionAfter(pointer, prevInline, previous, expression, sequence);
        }
    }

    private Expression GetExpression(Inline inline)
    {
        if (inline == null)
        {
            return null;
        }

        _indexTable.TryGetValue(inline, out Expression result);
        return result;
    }

    private Inline GetSelectedInline(out int leftIndex) => GetSelectedInline(Selection.Start, out leftIndex);

    /// <summary>
    /// Получить выделенный элемент
    /// Если выделение пусто:
    /// - если курсор находится в начале текста, вернуть первый элемент
    /// - иначе вернуть элемент, расположенный перед курсором
    /// Если выделение не пусто:
    /// - если курсор в начале выделения, вернуть элемент, расположенный после курсора
    /// - если курсор в конце выделения, вернуть элемент, расположенный перед курсором
    /// </summary>
    /// <param name="pointer"></param>
    /// <param name="leftIndex"></param>
    /// <returns></returns>
    private Inline GetSelectedInline(TextPointer pointer, out int leftIndex)
    {
        if (pointer.Parent is not Run active)
        {
            leftIndex = 1;
            System.Windows.DependencyObject element;

            if (Selection.IsEmpty || pointer == Selection.End)
            {
                element = pointer.GetAdjacentElement(LogicalDirection.Backward);

                if (element is Paragraph)
                {
                    element = pointer.GetAdjacentElement(LogicalDirection.Forward);
                    leftIndex = 0;
                }
            }
            else
            {
                element = pointer.GetAdjacentElement(LogicalDirection.Forward);
                leftIndex = 0;
            }

            return element as Inline;
        }
        else
        {
            leftIndex = active.ContentStart.GetOffsetToPosition(pointer);

            if (Selection.IsEmpty && leftIndex == 0 && active.PreviousInline != null)
            {
                leftIndex = 1;
                return active.PreviousInline;
            }
            else if (!Selection.IsEmpty && pointer == Selection.Start && leftIndex == active.Text.Length && active.NextInline != null)
            {
                leftIndex = 0;
                return active.NextInline;
            }
        }

        return active;
    }

    private Inline[] GetAllSelectedInlines(out int leftIndex, out int rightIndex)
    {
        var pointer = Selection.Start;
        var result = new List<Inline>();

        leftIndex = 0;
        var inline = GetSelectedInline(out int leftIndexLocal);

        if (inline != null)
        {
            while (pointer.CompareTo(Selection.End) < 0)
            {
                if (result.Count == 0)
                {
                    leftIndex = leftIndexLocal;
                }

                result.Add(inline);

                if (inline.NextInline == null)
                {
                    break;
                }

                inline = inline.NextInline;

                pointer = inline.ContentStart;
            }
        }

        if (inline == null || inline.PreviousInline != null && inline.PreviousInline is InlineUIContainer)
            rightIndex = 0;
        else if (inline.PreviousInline != null)
            rightIndex = inline.PreviousInline.ContentEnd.GetOffsetToPosition(Selection.End);
        else if (inline is InlineUIContainer)
            rightIndex = 0;
        else
            rightIndex = inline.ContentEnd.GetOffsetToPosition(Selection.End);

        return result.ToArray();
    }

    private Expression[] GetExpressionChain(Expression expression)
    {
        var result = new List<Expression>();
        FillChain(_rootExpression, expression, result);

        return result.ToArray();
    }

    private bool FillChain(Expression parent, Expression expression, List<Expression> chain)
    {
        foreach (var child in parent.Operands())
        {
            if (child == expression || FillChain(child, expression, chain))
            {
                chain.Add(child);
                return true;
            }
        }

        return false;
    }

    private void InsertExpressionAfter(TextPointer pointer, Inline inline, Expression before, Expression expr, Sequence root)
    {
        if (before == null)
        {
            _rootExpression.SetOperands(new Expression[] { expr });
            _paragraph.Inlines.Clear();
            _indexTable.Clear();
        }
        else
        {
            InsertPureExpression(before, expr, root);
        }

        var split = before != null && pointer.CompareTo(inline.ContentStart) > 0 && pointer.CompareTo(inline.ContentEnd) < 0;

        if (!InsertExpressionAtPoint(pointer, expr, out Inline toInsert))
        {
            return;
        }

        if (split)
        {
            ((StringValue)((Instruction)before).Argument).Value = ((Run)inline).Text;
            
            Inline inl = toInsert;
            Run newRun;
            do
            {
                inl = inl.NextInline;
                newRun = inl as Run;
            } while (inl != null && newRun == null);

            var newText = CreateInstruction(newRun.Text);
            InsertPureExpression(expr, newText, root);

            _indexTable[newRun] = newText;
        }
    }

    private void InsertExpressionAtTheBeginning(Expression expr, Sequence root)
    {
        InsertPureExpressionAtTheBeginning(expr, root);

        InsertExpressionAtPoint(expr, out Inline _);
    }

    private bool InsertExpressionAtPoint(Expression child, out Inline toInsert)
    {
        return InsertExpressionAtPoint(Selection.Start, child, out toInsert);
    }

    private bool InsertExpressionAtPoint(TextPointer pointer, Expression child, out Inline toInsert)
    {
        if (child is Instruction instruct)
        {
            if (instruct.Argument is StringValue value)
            {
                toInsert = new Run(value.Value, pointer);
            }
            else
            {
                GetSetDisplayData((Set)instruct.Argument, out string text, out SolidColorBrush color, out _);

                var run = new Run(text) { Background = color };
                toInsert = new InlineUIContainer(new TextBlock(run), pointer);
            }
        }
        else if (child is Set set) // <Line>
        {
            GetSetDisplayData(set, out string text, out SolidColorBrush color, out _);

            if (text == "\n")
                toInsert = new LineBreak(pointer);
            else
            {
                var run = new Run(text) { Background = color };
                toInsert = new InlineUIContainer(new TextBlock(run), pointer);
            }
        }
        else if (child is Optional)
        {
            if (Selection.IsEmpty)
            {
                var run = new Run("(") { Background = new SolidColorBrush(Colors.Chartreuse) };
                toInsert = new InlineUIContainer(new TextBlock(run), pointer);

                Selection.Select(toInsert.ElementEnd, toInsert.ElementEnd);

                _indexTable[toInsert] = child;

                run = new Run(")?") { Background = new SolidColorBrush(Colors.Chartreuse) };
                toInsert = new InlineUIContainer(new TextBlock(run), toInsert.ElementEnd);
            }
            else if (Selection.Start == pointer)
            {
                var run = new Run("(") { Background = new SolidColorBrush(Colors.Chartreuse) };
                toInsert = new InlineUIContainer(new TextBlock(run), pointer);

                _indexTable[toInsert] = child;
                return true;
            }
            else
            {
                var run = new Run(")?") { Background = new SolidColorBrush(Colors.Chartreuse) };
                toInsert = new InlineUIContainer(new TextBlock(run), pointer);

                _indexTable[toInsert] = child;
                return true;
            }
        }
        else
        {
            toInsert = null;
            return false;
        }

        Selection.Select(toInsert.ElementEnd, toInsert.ElementEnd);
        _indexTable[toInsert] = child;
        return true;
    }

    private void InsertPureExpression(Expression before, Expression expr, Sequence root)
    {
        var chain = GetExpressionChain(before);
        int index = 0;
        for (int i = 0; i < chain.Length; i++)
        {
            index = Array.LastIndexOf(root.OperandsArray, chain[i]) + 1; // LastIndex, т.к. в процессе построения временно возникает два Optional
            if (index > 0)
                break;
        }

        var newOperands = root.OperandsArray.Take(index).Concat(new Expression[] { expr }).Concat(root.OperandsArray.Skip(index));
        root.SetOperands(newOperands.ToArray());
    }

    private static void InsertPureExpressionAtTheBeginning(Expression expr, Sequence root)
    {
        var newOperands = new Expression[] { expr }.Concat(root.OperandsArray);
        root.SetOperands(newOperands.ToArray());
    }

    private bool DeleteForward()
    {
        if (Selection.IsEmpty)
        {
            var after = Selection.End.GetNextInsertionPosition(LogicalDirection.Forward);
            if (after == null)
            {
                return false;
            }

            Selection.Select(Selection.Start, after);
        }

        RemoveSelection();
        return true;
    }

    private bool DeleteBackward()
    {
        if (Selection.IsEmpty)
        {
            var before = Selection.Start.GetNextInsertionPosition(LogicalDirection.Backward);
            if (before == null)
            {
                return false;
            }

            Selection.Select(before, Selection.End);
        }

        RemoveSelection();
        return true;
    }

    /// <summary>
    /// Удалить выделенный участок
    /// </summary>
    private bool RemoveSelection()
    {
        if (Selection.IsEmpty) // Удалять нечего
        {
            return false;
        }

        // Run'ы удалим первыми, чтобы при удалении контейнеров они не слиплись
        var inlines = GetAllSelectedInlines(out int leftIndex, out int rightIndex);

        var exprs = new List<Expression>();
        bool toDelete;

        for (int i = 0; i < inlines.Length; i++)
        {
            toDelete = true;

            if (inlines[i] is Run run)
            {
                if (i == 0 && leftIndex > 0)
                {
                    if (i == inlines.Length - 1 && rightIndex < 0)
                        Cut(run, leftIndex, rightIndex);
                    else
                        Cut(run, leftIndex, 0);

                    toDelete = false;
                }
                else if (i == inlines.Length - 1 && rightIndex < 0)
                {
                    Cut(run, 0, rightIndex);
                    toDelete = false;
                }
            }

            if (toDelete)
            {
                exprs.Add(DeleteInline(inlines[i]));
            }
        }

        var expr = exprs.FirstOrDefault();
        if (expr == null)
        {
            return false;
        }

        var chain = GetExpressionChain(expr);
        var sequence = _rootExpression;

        if (chain.Length > 0)
        {
            expr = chain[^1];

            for (int i = 0; i < chain.Length; i++)
            {
                if (chain[i] is Sequence seq)
                {
                    sequence = seq;

                    if (i > 0)
                    {
                        expr = chain[i - 1];
                    }

                    break;
                }
            }
        }

        // Склеивание двух строк
        var newOps = sequence.OperandsArray.Where(ex => !exprs.Contains(ex));
        var first = inlines[0];
        var last = inlines[^1];

        if (first.PreviousInline is Run && last.NextInline is Run run2)
        {
            var prev = GetExpression(first.PreviousInline);

            ((StringValue)((Instruction)prev).Argument).Value += run2.Text;

            newOps = newOps.Where(ex => ex != GetExpression(last.NextInline)).ToArray();
            _indexTable.Remove(last.NextInline);
        }

        sequence.SetOperands(newOps.ToArray());
        CheckIfEmpty();

        return true;
    }

    private void Cut(Run run, int leftIndex, int rightIndex)
    {
        var instruction = (Instruction)GetExpression(run);
        var expr = (StringValue)instruction.Argument;
        var value = expr.Value;

        expr.Value = string.Concat(value.AsSpan(0, leftIndex), value.AsSpan(value.Length + rightIndex, -rightIndex));
    }

    private Expression DeleteInline(Inline inline)
    {
        var expr = GetExpression(inline); // Это должно быть перед удалением из indexTable
        _indexTable.Remove(inline);

        return expr;
    }

    private void UpdateSpard()
    {
        if (System.Windows.Threading.Dispatcher.CurrentDispatcher != Dispatcher)
        {
            Dispatcher.Invoke(new Action(UpdateSpard));
            return;
        }

        _paragraph.Inlines.Clear();
        InitAndSetText();
    }

    private static Instruction CreateInstruction(string text = "") => new(new StringValue("ignoresp"), new StringValue(text));

    private void InitAndSetText()
    {
        _indexTable.Clear();
        Init(_rootExpression, null);

        SpardChanged();
    }

    private void SpardChanged()
    {
        BuildSpard();
#if DEBUG
        ToolTip = Spard;
#endif
    }

    private void Init(Expression expression, Expression parent)
    {
        if (expression is StringValue stringValue)
        {
            if (parent is not Instruction) // Мало ли
            {
                var instruct = new Instruction(new StringValue("ignoresp"), stringValue);

                var operands = parent.Operands().ToArray();
                var index = Array.IndexOf(operands, stringValue);
                operands[index] = instruct;

                parent.SetOperands(operands);

                parent = instruct;
            }

            AppendTextToEnd(parent, stringValue.Value);
            return;
        }

        if (expression is Set set)
        {
            GetSetDisplayData(set, out string text, out SolidColorBrush color, out bool isReadOnly);

            var run = AppendTextToEnd(isReadOnly ? parent : expression, text, true);
            if (run != null)
                run.Background = color;

            return;
        }

        if (expression is Optional opt)
        {
            var run = AppendTextToEnd(expression, "(", true);
            run.Background = new SolidColorBrush(Colors.Chartreuse);

            if (opt.Operand != null)
                Init(opt.Operand, expression);

            run = AppendTextToEnd(expression, ")?", true);
            run.Background = new SolidColorBrush(Colors.Chartreuse);
            return;
        }

        if (expression is Sequence sequence)
        {
            foreach (var item in sequence.Operands().ToArray())
            {
                Init(item, expression);
            }

            return;
        }

        if (expression is Instruction instruction && instruction.Argument != null)
        {
            Init(instruction.Argument, expression);
        }
    }

    private void GetSetDisplayData(Set set, out string text, out SolidColorBrush color, out bool isReadOnly)
    {
        var name = ((StringValue)set.Operand.Operands().First()).Value;
        _spardViewModel.Aliases.TryGetValue(name, out var alias);
        text = alias == null ? (name == "Line" ? "\n" : name) : alias.VisibleName;
        color = new SolidColorBrush(alias == null ? Colors.MediumSlateBlue : (Color)ColorConverter.ConvertFromString(alias.Color));
        isReadOnly = name != "Line" && name != "Some";
    }

    private Run AppendTextToEnd(Expression expression, string text, bool readOnly = false)
    {
        if (text == "\n")
        {
            var item = new LineBreak();
            _paragraph.Inlines.Add(item);
            _indexTable[item] = expression;

            return null;
        }

        var run = new Run(text);

        if (readOnly)
        {
            var container = new InlineUIContainer(new TextBlock(run));
            _paragraph.Inlines.Add(container);
            _indexTable[container] = expression;
        }
        else
        {
            _paragraph.Inlines.Add(run);
            _indexTable[run] = expression;
        }

        return run;
    }
}
