using SIPackages;
using SIPackages.Core;
using System.Collections.Specialized;
using System.Globalization;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class PointAnswerViewModel : ModelViewBase
{
    private readonly QuestionViewModel _question;
    private string _answer;
    private double _deviation;
    private ContentItemsViewModel? _subscribedContent;

    public string Answer
    {
        get => _answer;
        set
        {
            if (_answer != value)
            {
                var oldValue = _answer;
                _answer = value;
                OnPropertyChanged<string>(oldValue);
                UpdateRightAnswer();
            }
        }
    }

    public double Deviation
    {
        get => _deviation;
        set
        {
            if (Math.Abs(_deviation - value) > double.Epsilon)
            {
                var oldValue = _deviation;
                _deviation = value;
                OnPropertyChanged(oldValue);
                UpdateDeviation();
            }
        }
    }

    public SimpleCommand SelectPoint { get; }

    public event Action<ContentItem>? SelectPointRequest;

    public PointAnswerViewModel(QuestionViewModel question)
    {
        _question = question;
        SelectPoint = new SimpleCommand(SelectPoint_Executed);
        
        if (_question.Right.Count > 0)
        {
            _answer = _question.Right[0];
        }
        else
        {
            _answer = "";
        }

        if (_question.Parameters.TryGetValue(QuestionParameterNames.AnswerDeviation, out var deviationParameter)
            && double.TryParse(deviationParameter.Model.SimpleValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var dev))
        {
            _deviation = dev;
        }

        _question.Parameters.CollectionChanged += Parameters_CollectionChanged;
        SubscribeToContent();
        UpdateCanSelectPoint();
    }

    private void Parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SubscribeToContent();
        UpdateCanSelectPoint();
    }

    private void SubscribeToContent()
    {
        if (_question.Parameters.TryGetValue(QuestionParameterNames.Question, out var qParam)
            && qParam.ContentValue != null)
        {
            if (_subscribedContent != qParam.ContentValue)
            {
                if (_subscribedContent != null)
                {
                    _subscribedContent.CollectionChanged -= Content_CollectionChanged;
                }

                _subscribedContent = qParam.ContentValue;
                _subscribedContent.CollectionChanged += Content_CollectionChanged;
            }
        }
        else
        {
            if (_subscribedContent != null)
            {
                _subscribedContent.CollectionChanged -= Content_CollectionChanged;
                _subscribedContent = null;
            }
        }
    }

    private void Content_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCanSelectPoint();

    private void UpdateCanSelectPoint()
    {
        var canExecute = false;

        if (_question.Parameters.TryGetValue(QuestionParameterNames.Question, out var qParam)
            && qParam.ContentValue != null
            && qParam.ContentValue.Count > 0)
        {
            var last = qParam.ContentValue.Last();
            if (last.Model.Type == ContentTypes.Image)
            {
                canExecute = true;
            }
        }

        SelectPoint.CanBeExecuted = canExecute;
    }

    private void UpdateRightAnswer()
    {
        if (_question.Right.Count == 0)
        {
            _question.Right.Add(_answer);
        }
        else
        {
            _question.Right[0] = _answer;
        }
    }

    private void UpdateDeviation()
    {
        if (_question.Parameters.TryGetValue(QuestionParameterNames.AnswerDeviation, out var deviationParameter))
        {
            deviationParameter.Model.SimpleValue = _deviation.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            var newParameter = new StepParameterViewModel(_question, new StepParameter
            {
                Type = StepParameterTypes.Simple,
                SimpleValue = _deviation.ToString(CultureInfo.InvariantCulture)
            });
            _question.Parameters.AddParameter(QuestionParameterNames.AnswerDeviation, newParameter);
        }
    }

    private void SelectPoint_Executed(object? arg)
    {
        if (_question.Parameters.TryGetValue(QuestionParameterNames.Question, out var qParam)
            && qParam.ContentValue != null
            && qParam.ContentValue.Count > 0)
        {
            var last = qParam.ContentValue.Last();
            if (last.Model.Type == ContentTypes.Image)
            {
                SelectPointRequest?.Invoke(last.Model);
            }
        }
    }

    public StreamInfo? GetImageStream(string uri)
    {
        var document = _question.OwnerTheme?.OwnerRound?.OwnerPackage?.Document;
        if (document == null) return null;

        var name = uri;
        if (name.StartsWith("@")) name = name[1..];
        
        var items = name.Split(new[] { '/', '\\' }, 2);
        if (items.Length > 1) name = items[1];

        return document.Document.Images.GetFile(name);
    }

    protected override void Dispose(bool disposing)
    {
        _question.Parameters.CollectionChanged -= Parameters_CollectionChanged;
        if (_subscribedContent != null)
        {
            _subscribedContent.CollectionChanged -= Content_CollectionChanged;
            _subscribedContent = null;
        }
    }
}
