using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using SImulator.Properties;
using SImulator.ViewModel;
using SIUI.ViewModel;
using System.ComponentModel;

namespace SImulator.Views;

/// <summary>
/// Shows the moderator view of the current game stage (round table, question, final etc.).
/// </summary>
internal sealed class StageView : ContentControl
{
    private GameViewModel? _game;
    private TableInfoViewModel? _localInfo;

    public StageView() => DataContextChanged += (_, _) => Attach(DataContext as GameViewModel);

    private void Attach(GameViewModel? game)
    {
        if (_localInfo != null)
        {
            _localInfo.PropertyChanged -= LocalInfo_PropertyChanged;
        }

        _game = game;
        _localInfo = game?.LocalInfo;

        if (_localInfo != null)
        {
            _localInfo.PropertyChanged += LocalInfo_PropertyChanged;
        }

        Rebuild();
    }

    private void LocalInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableInfoViewModel.TStage))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(Rebuild);
        }
    }

    private void Rebuild()
    {
        if (_game == null || _localInfo == null)
        {
            Content = null;
            return;
        }

        Content = _localInfo.TStage switch
        {
            TableStage.Sign => Hint(Properties.Resources.HintIntro),
            TableStage.GameThemes => Hint(Properties.Resources.HintGameThemes),
            TableStage.Round => BoundHint(_game, nameof(GameViewModel.ActiveRoundName)),
            TableStage.RoundTable => RoundTable(_localInfo),
            TableStage.Final => FinalTable(_localInfo),
            TableStage.Special => Special(_game),
            TableStage.Question => Question(_game),
            TableStage.QuestionPrice or TableStage.Theme => BoundHint(_localInfo, nameof(TableInfoViewModel.Text)),
            _ => Hint(Properties.Resources.HintBeforeStart),
        };
    }

    private static TextBlock Hint(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(30),
    };

    private static TextBlock BoundHint(object source, string path)
    {
        var text = Hint("");
        text.Bind(TextBlock.TextProperty, new Binding(path) { Source = source });
        return text;
    }

    private static Control RoundTable(TableInfoViewModel info)
    {
        var grid = new UniformGrid { Columns = 1 };

        foreach (var theme in info.RoundInfo)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,5*") };
            row.Children.Add(new TextBlock
            {
                Text = theme.Name,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Margin = new Thickness(2),
            });

            var questions = new UniformGrid { Rows = 1 };
            Grid.SetColumn(questions, 1);

            foreach (var question in theme.Questions)
            {
                var button = new Button
                {
                    Command = info.SelectQuestion,
                    CommandParameter = question,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1),
                };

                void update()
                {
                    var available = question.Price != QuestionInfoViewModel.InvalidPrice;
                    button.Content = available ? question.Price.ToString() : "";
                    button.IsEnabled = available;
                }

                update();
                question.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(update);
                questions.Children.Add(button);
            }

            row.Children.Add(questions);
            grid.Children.Add(row);
        }

        return grid;
    }

    private static Control FinalTable(TableInfoViewModel info)
    {
        var panel = new StackPanel { Margin = new Thickness(5) };

        foreach (var theme in info.RoundInfo)
        {
            var button = new Button
            {
                Command = info.SelectTheme,
                CommandParameter = theme,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Height = 28,
                Margin = new Thickness(0, 1),
            };

            void update()
            {
                var available = !string.IsNullOrEmpty(theme.Name);
                button.Content = available ? theme.Name : "";
                button.IsEnabled = available;
            }

            update();
            theme.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(update);
            panel.Children.Add(button);
        }

        return new ScrollViewer { Content = panel };
    }

    private static Control Special(GameViewModel game)
    {
        var panel = new StackPanel();
        var type = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };
        type.Bind(TextBlock.TextProperty, new Binding(nameof(GameViewModel.ActiveQuestionTypeName)) { Source = game });
        panel.Children.Add(type);

        foreach (var parameter in game.ActiveQuestionParameters)
        {
            panel.Children.Add(new TextBlock { Text = $"{parameter.Key}: {parameter.Value}" });
        }

        return panel;
    }

    private static Control Question(GameViewModel game)
    {
        var panel = new StackPanel { Margin = new Thickness(5), Spacing = 4 };

        var themeComments = new TextBlock { TextWrapping = TextWrapping.Wrap, FontStyle = FontStyle.Italic };
        themeComments.Bind(TextBlock.TextProperty, new Binding(nameof(GameViewModel.ThemeComments)) { Source = game });
        themeComments.Bind(IsVisibleProperty, new Binding(nameof(GameViewModel.ThemeComments)) { Source = game, Converter = Converters.NotEmpty });
        panel.Children.Add(themeComments);

        panel.Children.Add(new TextBlock { Text = Properties.Resources.Question, Foreground = Brushes.Gray });

        var content = new ItemsControl();
        content.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(GameViewModel.ContentItems)) { Source = game });
        content.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<SIPackages.ContentItem>((item, _) =>
            new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = item == null ? "" : item.Type == SIPackages.Core.ContentTypes.Text ? item.Value : $"[{item.Type}] {item.Value}",
            });
        panel.Children.Add(content);

        panel.Children.Add(new TextBlock { Text = Properties.Resources.Answer, Foreground = Brushes.Gray, Margin = new Thickness(0, 8, 0, 0) });

        var answers = new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold };
        answers.Bind(TextBlock.TextProperty, new Binding(nameof(GameViewModel.QuestionAnswers)) { Source = game, Converter = Converters.JoinLines });
        panel.Children.Add(answers);

        return new ScrollViewer { Content = panel };
    }
}
