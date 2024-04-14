using SIPackages;

namespace SIQuester.ViewModel;

public sealed class PreviewViewModel
{
    public SIDocument Document { get; private set; }

    public SortedDictionary<string, int> Content { get; } = new();

    public int QuestionCount { get; private set; }

    public PreviewViewModel(SIDocument document)
    {
        Document = document;
        var questionCount = 0;

        foreach (var round in document.Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    questionCount++;

                    foreach (var contentItem in question.GetContent())
                    {
                        if (Content.TryGetValue(contentItem.Type, out var value))
                        {
                            Content[contentItem.Type] = value + 1;
                        }
                        else
                        {
                            Content[contentItem.Type] = 1;
                        }
                    }
                }
            }
        }

        QuestionCount = questionCount;
    }
}
