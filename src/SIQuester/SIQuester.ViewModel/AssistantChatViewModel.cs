using SIQuester.ViewModel.Properties;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// One message in the assistant chat.
/// </summary>
public sealed class ChatMessageViewModel : ModelViewBase
{
    public bool IsUser { get; }

    private string _text;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isError;

    public bool IsError
    {
        get => _isError;
        set
        {
            if (_isError != value)
            {
                _isError = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Text sent to the AI in the conversation history when it differs from <see cref="Text" />
    /// (the shown text may end with a UI note like "(stopped)").
    /// </summary>
    public string? HistoryText { get; set; }

    /// <summary>
    /// Not sent to the AI: a question that got no answer (missing key, error, stopped at once) or a UI-only note.
    /// </summary>
    public bool SkipInHistory { get; set; }

    public ChatMessageViewModel(bool isUser, string text)
    {
        IsUser = isUser;
        _text = text;
    }
}

/// <summary>
/// Claude assistant chat shown in the corner of the main window.
/// Every request includes the current package context (active theme and question).
/// </summary>
public sealed class AssistantChatViewModel : ModelViewBase
{
    private const string BasePrompt =
        "Ты — помощник автора вопросов в SIQuester, редакторе пакетов для интеллектуальной игры «Своя игра» (SIGame). " +
        "Помогай придумывать и улучшать вопросы, проверять факты и однозначность ответов, предлагать неверные ответы, " +
        "темы, комментарии и источники. Вопросы должны быть короткими, точными, с единственным однозначным ответом. " +
        "Если сомневаешься в факте — прямо скажи об этом. Отвечай кратко и по делу, на языке пользователя (обычно русский). " +
        "Пиши простым текстом без Markdown (без звёздочек и решёток); списки оформляй строками, начинающимися с «•». " +
        "Ниже — текущее содержимое пакета, которое видит автор; используй его, когда вопрос относится к нему.";

    private readonly Func<QDocument?> _getDocument;

    private CancellationTokenSource? _cancellation;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    private bool _isOpen;

    /// <summary>
    /// Is the chat panel open.
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                OnPropertyChanged();
            }
        }
    }

    private string _input = "";

    public string Input
    {
        get => _input;
        set
        {
            if (_input != value)
            {
                _input = value;
                OnPropertyChanged();
                Send.CanBeExecuted = CanSend;
            }
        }
    }

    private bool _isBusy;

    /// <summary>
    /// Claude is answering.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
                Send.CanBeExecuted = CanSend;
                Stop.CanBeExecuted = value;
            }
        }
    }

    public SimpleCommand Send { get; }

    public SimpleCommand Stop { get; }

    public ICommand Clear { get; }

    public ICommand Toggle { get; }

    private bool CanSend => !_isBusy && !string.IsNullOrWhiteSpace(_input);

    public AssistantChatViewModel(Func<QDocument?> getDocument)
    {
        _getDocument = getDocument;

        Send = new SimpleCommand(async _ => await SendAsync()) { CanBeExecuted = false };
        Stop = new SimpleCommand(_ => _cancellation?.Cancel()) { CanBeExecuted = false };
        Clear = new SimpleCommand(_ => { if (!_isBusy) Messages.Clear(); });
        Toggle = new SimpleCommand(_ => IsOpen = !IsOpen);

        AppSettings.Default.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppSettings.AiProvider) or nameof(AppSettings.GPTModel) or nameof(AppSettings.GeminiModel) or null or "")
            {
                OnPropertyChanged(nameof(AssistantName));
                OnPropertyChanged(nameof(AssistantSubtitle));
            }
        };
    }

    /// <summary>
    /// Name in the chat header and on the floating button.
    /// </summary>
    public string AssistantName => AppSettings.Default.AiProvider switch
    {
        AiProvider.ChatGPT => "ChatGPT",
        AiProvider.Gemini => "Gemini",
        _ => "Claude"
    };

    /// <summary>
    /// Model and context hint under the chat title.
    /// </summary>
    public string AssistantSubtitle =>
        AppSettings.Default.AiProvider switch
        {
            AiProvider.ChatGPT => GptAssistant.ModelName,
            AiProvider.Gemini => GeminiAssistant.ModelName,
            _ => "Opus 5.5"
        } + Resources.ChatSubtitleTail;

    private async Task SendAsync()
    {
        var text = Input.Trim();

        if (text.Length == 0 || _isBusy)
        {
            return;
        }

        Input = "";
        var question = new ChatMessageViewModel(true, text);
        Messages.Add(question);

        var provider = AppSettings.Default.AiProvider;

        var (isConfigured, noKeyMessage) = provider switch
        {
            AiProvider.ChatGPT => (GptAssistant.IsConfigured, Resources.ChatNoKeyGpt),
            AiProvider.Gemini => (GeminiAssistant.IsConfigured, Resources.ChatNoKeyGemini),
            _ => (ClaudeAssistant.IsConfigured, Resources.ChatNoKey)
        };

        if (!isConfigured)
        {
            question.SkipInHistory = true; // unanswered: must not be sent again with the next question
            Messages.Add(new ChatMessageViewModel(false, noKeyMessage) { IsError = true });
            return;
        }

        // Conversation for the API: answered turns only, without error and UI notes
        var history = Messages
            .Where(m => !m.IsError && !m.SkipInHistory)
            .Select(m => (m.IsUser, Text: m.HistoryText ?? m.Text))
            .Where(m => m.Text.Length > 0)
            .ToList();

        var answer = new ChatMessageViewModel(false, "");
        Messages.Add(answer);

        IsBusy = true;
        _cancellation = new CancellationTokenSource();

        try
        {
            void OnText(string piece) => answer.Text += piece;

            var stopReason = provider switch
            {
                AiProvider.ChatGPT => await GptAssistant.StreamChatAsync(BuildSystemPrompt(), history, OnText, _cancellation.Token),
                AiProvider.Gemini => await GeminiAssistant.StreamChatAsync(BuildSystemPrompt(), history, OnText, _cancellation.Token),
                _ => await ClaudeAssistant.StreamChatAsync(BuildSystemPrompt(), history, OnText, _cancellation.Token)
            };

            // Claude and Gemini: refusal / max_tokens; ChatGPT: content_filter / length
            var reason = stopReason?.Replace("_", "").ToLowerInvariant();

            if (reason is "refusal" or "contentfilter")
            {
                answer.Text = Resources.ChatRefused;
                answer.IsError = true;
                question.SkipInHistory = true;
            }
            else if (reason is "maxtokens" or "length")
            {
                answer.HistoryText = answer.Text; // the AI gets its own text without the UI note
                answer.Text += "\n\n" + Resources.ChatTruncated;
            }
        }
        catch (OperationCanceledException)
        {
            if (answer.Text.Length > 0)
            {
                answer.HistoryText = answer.Text; // the part that arrived before stopping
                answer.Text += "\n\n" + Resources.ChatStopped;
            }
            else
            {
                // Stopped before any text: only a note is shown, the question stays unanswered
                answer.Text = Resources.ChatStopped;
                answer.SkipInHistory = true;
                question.SkipInHistory = true;
            }
        }
        catch (Exception exc)
        {
            answer.Text = Resources.ChatErrorPrefix + exc.Message;
            answer.IsError = true;
            question.SkipInHistory = true;
        }
        finally
        {
            if (answer.Text.Length == 0)
            {
                Messages.Remove(answer);
            }

            _cancellation.Dispose();
            _cancellation = null;
            IsBusy = false;
        }
    }

    /// <summary>
    /// Builds the system prompt with the package context: structure overview,
    /// the active theme with its questions and the selected question.
    /// </summary>
    private string BuildSystemPrompt()
    {
        var prompt = new StringBuilder(BasePrompt);
        var document = _getDocument();

        if (document == null)
        {
            prompt.AppendLine().AppendLine().Append("Сейчас пакет не открыт.");
            return prompt.ToString();
        }

        var package = document.Package;
        prompt.AppendLine().AppendLine().AppendLine($"<package name=\"{package.Model.Name}\">");

        foreach (var round in package.Rounds)
        {
            prompt.AppendLine($"Раунд «{round.Model.Name}»: темы — {string.Join(", ", round.Themes.Select(t => $"«{t.Model.Name}»"))}");
        }

        prompt.AppendLine("</package>");

        var node = document.ActiveNode;
        var activeQuestion = node as QuestionViewModel;
        var activeTheme = activeQuestion?.OwnerTheme ?? node as ThemeViewModel;

        if (activeTheme != null)
        {
            prompt.AppendLine().AppendLine($"<current_theme name=\"{activeTheme.Model.Name}\">");

            foreach (var question in activeTheme.Questions)
            {
                AppendQuestion(prompt, question);
            }

            prompt.AppendLine("</current_theme>");
        }

        if (activeQuestion != null)
        {
            prompt.AppendLine().AppendLine("<selected_question>");
            AppendQuestion(prompt, activeQuestion);
            prompt.AppendLine("</selected_question>");
            prompt.Append("Если автор говорит «этот вопрос» — речь о selected_question.");
        }

        return prompt.ToString();
    }

    private static void AppendQuestion(StringBuilder prompt, QuestionViewModel question)
    {
        prompt.Append($"[{question.Model.Price}] {question.Model.GetText()}");
        prompt.Append($" | Ответ: {string.Join("; ", question.Right)}");

        if (question.Wrong.Count > 0)
        {
            prompt.Append($" | Неверные: {string.Join("; ", question.Wrong)}");
        }

        var comment = question.Info.Comments.Text;

        if (comment.Length > 0)
        {
            prompt.Append($" | Комментарий: {comment}");
        }

        prompt.AppendLine();
    }
}
