using Notions;
using OpenAI.Chat;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a package theme view model.
/// </summary>
public sealed class ThemeViewModel : ItemViewModel<Theme>
{
    /// <summary>
    /// Owner round view model.
    /// </summary>
    public RoundViewModel? OwnerRound { get; set; }

    public override IItemViewModel? Owner => OwnerRound;

    /// <summary>
    /// Theme questions.
    /// </summary>
    public ObservableCollection<QuestionViewModel> Questions { get; } = new();

    public override ICommand Add { get; protected set; }

    public override string AddHeader => Resources.AddQuestion;

    public override ICommand Remove { get; protected set; }

    public ICommand Clone { get; private set; }

    /// <summary>
    /// Adds new question.
    /// </summary>
    public ICommand AddQuestion { get; private set; }

    /// <summary>
    /// Adds new question having no data.
    /// </summary>
    public ICommand AddEmptyQuestion { get; private set; }

    /// <summary>
    /// Generates questions with the help of GPT.
    /// </summary>
    public ICommand GenerateQuestions { get; private set; }

    public ICommand SortQuestions { get; private set; }

    /// <summary>
    /// Shuffles questions and prices.
    /// </summary>
    public ICommand ShuffleQuestions { get; private set; }

    public ThemeViewModel(Theme theme)
        : base(theme)
    {
        foreach (var question in theme.Questions)
        {
            Questions.Add(new QuestionViewModel(question) { OwnerTheme = this });
        }

        Questions.CollectionChanged += Questions_CollectionChanged;

        Clone = new SimpleCommand(CloneTheme_Executed);
        Remove = new SimpleCommand(RemoveTheme_Executed);
        Add = AddQuestion = new SimpleCommand(AddQuestion_Executed);
        AddEmptyQuestion = new SimpleCommand(AddEmptyQuestion_Executed);
        GenerateQuestions = new SimpleCommand(GenerateQuestions_Executed);
        SortQuestions = new SimpleCommand(SortQuestions_Executed);
        ShuffleQuestions = new SimpleCommand(ShuffleQuestions_Executed);
    }

    public sealed class QuestionInfo
    {
        public string Text { get; set; } = string.Empty;

        public string[] AnswerOptions { get; set; } = Array.Empty<string>();

        public string Answer { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;
    }

    public sealed class ThemeInfo
    {
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public List<QuestionInfo> Questions { get; set; } = new List<QuestionInfo>();
    }

    private readonly BinaryData JsonSchema = BinaryData.FromBytes(
    Encoding.UTF8.GetBytes(
    @"{
        ""type"": ""object"",
        ""definitions"": {
            ""QuestionInfo"": {
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""Text"": {
                        ""type"": ""string""
                    },
                    ""AnswerOptions"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""string""
                        }
                    },
                    ""Answer"": {
                        ""type"": ""string""
                    },
                    ""Comment"": {
                        ""type"": ""string""
                    },
                    ""Source"": {
                        ""type"": ""string""
                    }
                },
                ""required"": [
                    ""Text"",
                    ""AnswerOptions"",
                    ""Answer"",
                    ""Comment"",
                    ""Source""
                ]
            }
        },
        ""properties"": {
            ""Name"": {
                ""type"": ""string""
            },
            ""Description"": {
                ""type"": ""string""
            },
            ""Questions"": {
                ""type"": ""array"",
                ""items"": {
                    ""$ref"": ""#/definitions/QuestionInfo""
                }
            }
        },
        ""required"": [
            ""Name"",
            ""Description"",
            ""Questions""
        ],
        ""additionalProperties"": false
    }"));

    private async void GenerateQuestions_Executed(object? arg)
    {
        if (OwnerRound == null)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(AppSettings.Default.GPTApiKey))
            {
                throw new Exception("API key not set");
            }

            var prompt = AppSettings.Default.GPTPrompt;

            if (string.IsNullOrEmpty(prompt))
            {
                prompt = Resources.DefaultGPTPrompt;
            }

            var themeNameSet = !string.IsNullOrEmpty(Model.Name);
            var topicName = themeNameSet ? $"\"{Model.Name}\"" : "";

            var userPrompt = $"Create topic {topicName} with 5 questions";

            List<ChatMessage> messages = new()
            {
                new SystemChatMessage(prompt),
                new UserChatMessage(userPrompt)
            };

            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "theme_questions",
                    jsonSchema: JsonSchema,
                    jsonSchemaIsStrict: true)
            };

            var client = new ChatClient(AppSettings.Default.GPTModel, AppSettings.Default.GPTApiKey);
            var completion = await client.CompleteChatAsync(messages, options);

            var data = completion.Value.Content[0].Text;
            var themeInfo = JsonSerializer.Deserialize<ThemeInfo>(data) ?? throw new Exception("Failed to parse GPT response");

            var document = (OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("document not found");
            using var change = document.OperationsManager.BeginComplexChange();

            for (var i = 0; i < Questions.Count; i++)
            {
                var question = Questions[i];

                if (question.Model.GetText().Length == 0 && (question.Right.FirstOrDefault()?.Length ?? 0) == 0)
                {
                    Questions.RemoveAt(i);
                    i--;
                }
            }

            if (!themeNameSet && !string.IsNullOrEmpty(themeInfo.Name))
            {
                Model.Name = themeInfo.Name;
            }

            if (!string.IsNullOrEmpty(themeInfo.Description))
            {
                Model.Info.Comments.Text = themeInfo.Description;
            }

            foreach (var questionInfo in themeInfo.Questions)
            {
                var price = DetectNextQuestionPrice(OwnerRound);

                var question = PackageItemsHelper.CreateQuestion(price, questionInfo.Text.ClearPoints(), questionInfo.Answer.ClearPoints());

                if (questionInfo.AnswerOptions.Length > 0)
                {
                    question.Parameters[QuestionParameterNames.AnswerType] = new StepParameter
                    {
                        Type = StepParameterTypes.Simple,
                        SimpleValue = StepParameterValues.SetAnswerTypeType_Select
                    };

                    StepParameters groupValue = new();

                    for (var i = 0; i < questionInfo.AnswerOptions.Length; i++)
                    {
                        groupValue.Add(
                            IndexLabelHelper.GetIndexLabel(i),
                            new()
                            {
                                Type = StepParameterTypes.Content,
                                ContentValue = new()
                                {
                                    new() { Type = ContentTypes.Text, Value = questionInfo.AnswerOptions[i].ClearPoints() }
                                }
                            });
                    }

                    question.Parameters[QuestionParameterNames.AnswerOptions] = new StepParameter
                    {
                        Type = StepParameterTypes.Group,
                        GroupValue = groupValue
                    };
                }

                if (!string.IsNullOrEmpty(questionInfo.Comment))
                {
                    question.Info.Comments.Text = questionInfo.Comment.ClearPoints();
                }

                if (!string.IsNullOrEmpty(questionInfo.Source))
                {
                    question.Info.Sources.Add(questionInfo.Source.ClearPoints());
                }

                var questionViewModel = new QuestionViewModel(question);
                Questions.Add(questionViewModel);
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowErrorMessage(exc.Message);
        }
    }

    private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                {
                    break;
                }

                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                {
                    if (Questions[i].OwnerTheme != null)
                    {
                        throw new Exception(Resources.ErrorInsertingBindedQuestion);
                    }

                    Questions[i].OwnerTheme = this;
                    Model.Questions.Insert(i, Questions[i].Model);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems == null)
                {
                    break;
                }

                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                {
                    if (Questions[i].OwnerTheme != null && Questions[i].OwnerTheme != this)
                    {
                        throw new Exception(Resources.ErrorInsertingBindedQuestion);
                    }

                    Questions[i].OwnerTheme = this;
                    Model.Questions[i] = Questions[i].Model;
                }
                break;

            case NotifyCollectionChangedAction.Move:
                var temp = Model.Questions[e.OldStartingIndex];
                Model.Questions.Insert(e.NewStartingIndex, temp);
                Model.Questions.RemoveAt(e.OldStartingIndex + (e.NewStartingIndex < e.OldStartingIndex ? 1 : 0));
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    break;
                }

                foreach (QuestionViewModel question in e.OldItems)
                {
                    question.OwnerTheme = null;
                    Model.Questions.RemoveAt(e.OldStartingIndex);

                    OwnerRound?.OwnerPackage?.Document?.ClearLinks(question);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Questions.Clear();

                foreach (var question in Questions)
                {
                    question.OwnerTheme = this;
                    Model.Questions.Add(question.Model);
                }
                break;
        }
    }

    private void CloneTheme_Executed(object? arg)
    {
        if (OwnerRound == null || OwnerRound.OwnerPackage == null)
        {
            return;
        }

        var newTheme = Model.Clone();
        var newThemeViewModel = new ThemeViewModel(newTheme);
        OwnerRound.Themes.Add(newThemeViewModel);
        OwnerRound.OwnerPackage.Document.Navigate.Execute(newThemeViewModel);
    }

    private void RemoveTheme_Executed(object? arg)
    {
        var ownerRound = OwnerRound;

        if (ownerRound == null)
        {
            return;
        }

        var ownerDocument = ownerRound.OwnerPackage?.Document;

        if (ownerDocument == null)
        {
            return;
        }

        using var change = ownerDocument.OperationsManager.BeginComplexChange();
        ownerRound.Themes.Remove(this);
        change.Commit();

        if (ownerDocument?.ActiveNode == this)
        {
            ownerDocument.ActiveNode = ownerRound;
        }
    }

    private void AddQuestion_Executed(object? arg)
    {
        try
        {
            var document = (OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("document not found");
            var price = DetectNextQuestionPrice(OwnerRound);

            var question = PackageItemsHelper.CreateQuestion(price);

            var questionViewModel = new QuestionViewModel(question);
            Questions.Add(questionViewModel);

            QDocument.ActivatedObject = questionViewModel.Parameters?.FirstOrDefault().Value.ContentValue?.FirstOrDefault();

            document.Navigate.Execute(questionViewModel);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private int DetectNextQuestionPrice(RoundViewModel round)
    {
        var validQuestions = Questions.Where(q => q.Model.Price != Question.InvalidPrice).ToList();
        var questionCount = validQuestions.Count;

        if (questionCount > 1)
        {
            var add = validQuestions[1].Model.Price - validQuestions[0].Model.Price;
            return Math.Max(0, validQuestions[questionCount - 1].Model.Price + add);
        }

        if (questionCount > 0)
        {
            return validQuestions[0].Model.Price * 2;
        }

        var roundIndex = round.OwnerPackage?.Rounds.IndexOf(round) ?? 0;

        return round.Model.Type == RoundTypes.Final ? 0 : AppSettings.Default.QuestionBase * (roundIndex + 1);
    }

    private void AddEmptyQuestion_Executed(object? arg)
    {
        var question = new Question { Price = -1 };
        var questionViewModel = new QuestionViewModel(question);
        Questions.Add(questionViewModel);
    }

    private void SortQuestions_Executed(object? arg)
    {
        try
        {
            var document = (OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("document not found");
            using var change = document.OperationsManager.BeginComplexChange();

            for (int i = 1; i < Questions.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (Questions[i].Model.Price < Questions[j].Model.Price)
                    {
                        Questions.Move(i, j);
                        break;
                    }
                }
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    private void ShuffleQuestions_Executed(object? arg)
    {
        try
        {
            var document = (OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("document not found");
            using var change = document.OperationsManager.BeginComplexChange();

            for (int i = 0; i < Questions.Count - 1; i++)
            {
                var j = i + Random.Shared.Next(Questions.Count - i);

                if (i == j)
                {
                    continue;
                }

                (Questions[j].Model.Price, Questions[i].Model.Price) = (Questions[i].Model.Price, Questions[j].Model.Price);
                (Questions[i], Questions[j]) = (Questions[j], Questions[i]);
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    protected override void UpdateCosts(CostSetter costSetter)
    {
        try
        {
            var document = (OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("document not found");
            using var change = document.OperationsManager.BeginComplexChange();

            UpdateCostsCore(costSetter);
            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.Inform(exc.Message, true);
        }
    }

    public void UpdateCostsCore(CostSetter costSetter)
    {
        for (var i = 0; i < Questions.Count; i++)
        {
            Questions[i].Model.Price = costSetter.BaseValue + costSetter.Increment * i;
        }
    }
}
