using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel;
using System.Text;
using System.Xml;

namespace SIQuester.Model;

/// <summary>
/// Contains package object data used in copy-paste and drag-and-drop operations.
/// </summary>
[Serializable]
public sealed class InfoOwnerData
{
    public enum Level { Package, Round, Theme, Question };

    public Level ItemLevel { get; set; }

    public string ItemData { get; set; }

    public AuthorInfo[] Authors { get; set; }

    public SourceInfo[] Sources { get; set; }

    public Dictionary<string, string> Images { get; set; } = new();

    public Dictionary<string, string> Audio { get; set; } = new();

    public Dictionary<string, string> Video { get; set; } = new();

    public Dictionary<string, string> Html { get; set; } = new();

    public InfoOwnerData(QDocument document, IItemViewModel item)
    {
        var model = item.GetModel();

        var sb = new StringBuilder();

        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        {
            model.WriteXml(writer);
        }

        ItemData = sb.ToString();

        ItemLevel =
            model is Package ? Level.Package :
            model is Round ? Level.Round :
            model is Theme ? Level.Theme : Level.Question;

        GetFullData(document, item);
    }

    public InfoOwner GetItem()
    {
        InfoOwner item = ItemLevel switch
        {
            Level.Package => new Package(),
            Level.Round => new Round(),
            Level.Theme => new Theme(),
            _ => new Question(),
        };

        using (var sr = new StringReader(ItemData))
        {
            using var reader = XmlReader.Create(sr);
            reader.Read();
            item.ReadXml(reader);
        }

        return item;
    }

    /// <summary>
    /// Gets full object data including attached objects.
    /// </summary>
    /// <param name="documentViewModel">Document which contains the object.</param>
    /// <param name="item">Object having necessary data.</param>
    private void GetFullData(QDocument documentViewModel, IItemViewModel item)
    {
        var model = item.GetModel();
        var document = documentViewModel.Document;

        var length = model.Info.Authors.Count;

        var authors = new HashSet<AuthorInfo>();

        for (int i = 0; i < length; i++)
        {
            var docAuthor = document.GetLink(model.Info.Authors, i);

            if (docAuthor != null)
            {
                authors.Add(docAuthor);
            }
        }

        Authors = authors.ToArray();

        length = model.Info.Sources.Count;

        var sources = new HashSet<SourceInfo>();

        for (int i = 0; i < length; i++)
        {
            var docSource = document.GetLink(model.Info.Sources, i);

            if (docSource != null)
            {
                sources.Add(docSource);
            }
        }

        Sources = sources.ToArray();

        GetMedia(documentViewModel, model);
    }

    private void GetMedia(QDocument documentViewModel, InfoOwner model)
    {
        if (model is Question question)
        {
            GetQuestion(documentViewModel, question);
        }

        if (model is Theme theme)
        {
            GetTheme(documentViewModel, theme);
        }

        if (model is Round round)
        {
            GetRound(documentViewModel, round);
        }
    }

    private void GetRound(QDocument documentViewModel, Round round)
    {
        foreach (var theme in round.Themes)
        {
            GetTheme(documentViewModel, theme);
        }
    }

    private void GetTheme(QDocument documentViewModel, Theme theme)
    {
        foreach (var question in theme.Questions)
        {
            GetQuestion(documentViewModel, question);
        }
    }

    private void GetQuestion(QDocument documentViewModel, Question question)
    {
        foreach (var contentItem in question.GetContent())
        {
            if (!contentItem.IsRef)
            {
                continue;
            }

            var collection = documentViewModel.TryGetCollectionByMediaType(contentItem.Type);

            if (collection == null)
            {
                continue;
            }

            var targetCollection = contentItem.Type switch
            {
                AtomTypes.Image => Images,
                AtomTypes.Audio => Audio,
                AtomTypes.AudioNew => Audio,
                AtomTypes.Video => Video,
                AtomTypes.Html => Html,
                _ => null,
            };

            if (targetCollection == null)
            {
                continue;
            }

            var link = contentItem.Value;

            if (!targetCollection.ContainsKey(link))
            {
                var preparedMedia = collection.Wrap(link);
                targetCollection.Add(link, preparedMedia.Uri);
            }
        }

        foreach (var atom in question.Scenario)
        {
            if (!atom.IsLink)
            {
                continue;
            }

            var collection = documentViewModel.TryGetCollectionByMediaType(atom.Type);

            if (collection == null)
            {
                continue;
            }

            var targetCollection = atom.Type switch
            {
                AtomTypes.Image => Images,
                AtomTypes.Audio => Audio,
                AtomTypes.AudioNew => Audio,
                AtomTypes.Video => Video,
                _ => null,
            };

            var link = atom.Text[1..];

            if (!targetCollection.ContainsKey(link))
            {
                var preparedMedia = collection.Wrap(link);
                targetCollection.Add(link, preparedMedia.Uri);
            }
        }
    }
}
