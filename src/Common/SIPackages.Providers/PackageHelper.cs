using SIPackages.Core;
using System.Text;

namespace SIPackages.Providers;

/// <summary>
/// Generates random question packages.
/// </summary>
public static class PackageHelper
{
    /// <summary>
    /// Random package marker.
    /// </summary>
    public const string RandomIndicator = "{random}";

    public static Task<SIDocument> GenerateRandomPackageAsync(
        IPackagesProvider provider,
        string name,
        string author,
        string finalName,
        int roundsCount,
        int themesCount,
        int baseCost,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var doc = SIDocument.Create(name, author, stream);
        return GenerateCoreAsync(provider, roundsCount, themesCount, baseCost, doc, finalName, "", int.MaxValue, cancellationToken);
    }

    public static Task<SIDocument> GenerateRandomPackageAsync(
        IPackagesProvider provider,
        string folder,
        string name,
        string author,
        string finalName,
        string culture,
        int roundsCount = 3,
        int themesCount = 6,
        int baseCost = 100,
        int maxPackageCount = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        var doc = SIDocument.Create(name, author, folder);

        return GenerateCoreAsync(
            provider,
            roundsCount,
            themesCount,
            baseCost,
            doc,
            finalName,
            culture,
            maxPackageCount,
            cancellationToken);
    }

    private static async Task<SIDocument> GenerateCoreAsync(
        IPackagesProvider provider,
        int roundCount,
        int themeCount,
        int baseCost,
        SIDocument doc,
        string finalName,
        string culture,
        int maxPackageCount = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        var files = (await provider.GetPackagesAsync(culture, cancellationToken)).ToList();

        if (maxPackageCount < int.MaxValue)
        {
            files = GetRandomValues(files, maxPackageCount).ToList();
        }

        var packageComments = new StringBuilder(RandomIndicator); // Used in game reports

        for (var i = 0; i < roundCount; i++)
        {
            doc.Package.Rounds.Add(new Round { Type = RoundTypes.Standart, Name = (i + 1).ToString() });

            for (int j = 0; j < themeCount; j++)
            {
                if (files.Count == 0)
                {
                    break;
                }

                if (!await ExtractThemeAsync(
                    provider,
                    doc,
                    files,
                    i,
                    round => round.Type == RoundTypes.Standart && round.Themes.Count > 0,
                    packageComments,
                    baseCost,
                    culture,
                    cancellationToken))
                {
                    j--;
                    continue;
                }
            }

            if (files.Count == 0)
            {
                break;
            }
        }

        doc.Package.Rounds.Add(new Round { Type = RoundTypes.Final, Name = finalName });

        for (var j = 0; j < 7; j++)
        {
            if (files.Count == 0)
            {
                break;
            }

            if (!await ExtractThemeAsync(
                provider,
                doc,
                files,
                roundCount,
                round => round.Type == RoundTypes.Final && round.Themes.Count > 0,
                packageComments,
                0,
                culture,
                cancellationToken))
            {
                j--;
                continue;
            }
        }

        // В пакете могут быть и свои комментарии; допишем туда
        doc.Package.Info.Comments.Text += packageComments.ToString();

        return doc;
    }

    private static IEnumerable<T> GetRandomValues<T>(IEnumerable<T> array, int n) =>
        array.OrderBy(x => Random.Shared.Next()).Take(n);

    private static async Task<bool> ExtractThemeAsync(
        IPackagesProvider provider,
        SIDocument doc,
        List<string> files,
        int roundIndex,
        Func<Round, bool> predicate,
        StringBuilder packageComments,
        int baseCost,
        string culture,
        CancellationToken cancellationToken = default)
    {
        var fIndex = Random.Shared.Next(files.Count);
        var sourceDocument = await provider.GetPackageAsync(culture, files[fIndex], cancellationToken) ?? throw new PackageNotFoundException(files[fIndex]);

        using (sourceDocument)
        {
            var normal = sourceDocument.Package.Rounds.Where(predicate).ToList();
            var count = normal.Count;

            if (count == 0)
            {
                files.RemoveAt(fIndex);
                return false;
            }

            var rIndex = Random.Shared.Next(count);
            var r = normal[rIndex];
            var tIndex = Random.Shared.Next(r.Themes.Count);

            var theme = r.Themes[tIndex];

            // Исключим повторения имён тем
            if (doc.Package.Rounds.Any(round => round.Themes.Any(th => th.Name == theme.Name)))
            {
                files.RemoveAt(fIndex);
                return false;
            }

            // Нужно перенести в пакет необходимых авторов, источники, медиаконтент
            InheritAuthors(sourceDocument, r, theme);
            InheritSources(sourceDocument, r, theme);

            for (int i = 0; i < theme.Questions.Count; i++)
            {
                theme.Questions[i].Price = (roundIndex + 1) * (i + 1) * baseCost;

                InheritAuthors(sourceDocument, theme.Questions[i]);
                InheritSources(sourceDocument, theme.Questions[i]);
                await InheritContentAsync(doc, sourceDocument, theme.Questions[i], cancellationToken);
            }

            doc.Package.Rounds[roundIndex].Themes.Add(theme);
            packageComments.AppendFormat("{0}:{1}:{2};", files[fIndex], sourceDocument.Package.Rounds.IndexOf(r), tIndex);
            return true;
        }
    }

    private static void InheritAuthors(SIDocument sourceDocument, Question question)
    {
        var authors = question.Info.Authors;

        if (authors.Count > 0)
        {
            for (int i = 0; i < authors.Count; i++)
            {
                var link = sourceDocument.GetLink(question.Info.Authors, i, out string tail);

                if (link != null)
                {
                    question.Info.Authors[i] = link + tail;
                }
            }
        }
    }

    private static void InheritSources(SIDocument sourceDocument, Question question)
    {
        var sources = question.Info.Sources;

        if (sources.Count > 0)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                var link = sourceDocument.GetLink(question.Info.Sources, i, out string tail);

                if (link != null)
                {
                    question.Info.Sources[i] = link + tail;
                }
            }
        }
    }

    private static async Task InheritContentAsync(
        SIDocument doc,
        SIDocument sourceDocument,
        Question question,
        CancellationToken cancellationToken = default)
    {
        foreach (var contentItem in question.GetContent())
        {
            if (contentItem.Type == ContentTypes.Text || !contentItem.IsRef)
            {
                continue;
            }

            var link = sourceDocument.TryGetMedia(contentItem);

            if (link.HasValue && link.Value.HasStream)
            {
                var collection = doc.TryGetCollection(contentItem.Type);

                if (collection != null)
                {
                    using var stream = link.Value.Stream!;// TODO: think about conflict names resolution here
                    await collection.AddFileAsync(contentItem.Value, stream, cancellationToken);
                }
            }
        }
    }

    private static void InheritAuthors(SIDocument sourceDocument, Round round, Theme theme)
    {
        var authors = theme.Info.Authors;

        if (authors.Count == 0)
        {
            authors = round.Info.Authors;

            if (authors.Count == 0)
            {
                authors = sourceDocument.Package.Info.Authors;
            }

            if (authors.Count > 0)
            {
                var realAuthors = sourceDocument.GetRealAuthors(authors);
                theme.Info.Authors.Clear();

                foreach (var item in realAuthors)
                {
                    theme.Info.Authors.Add(item);
                }
            }
        }
        else
        {
            for (int i = 0; i < authors.Count; i++)
            {
                var link = sourceDocument.GetLink(theme.Info.Authors, i, out string tail);

                if (link != null)
                {
                    theme.Info.Authors[i] = link + tail;
                }
            }
        }
    }

    private static void InheritSources(SIDocument sourceDocument, Round round, Theme theme)
    {
        var sources = theme.Info.Sources;

        if (sources.Count == 0)
        {
            sources = round.Info.Sources;

            if (sources.Count == 0)
            {
                sources = sourceDocument.Package.Info.Sources;
            }

            if (sources.Count > 0)
            {
                var realSources = sourceDocument.GetRealSources(sources);
                theme.Info.Sources.Clear();

                foreach (var item in realSources)
                {
                    theme.Info.Sources.Add(item);
                }
            }
        }
        else
        {
            for (int i = 0; i < sources.Count; i++)
            {
                var link = sourceDocument.GetLink(theme.Info.Sources, i, out string tail);

                if (link != null)
                {
                    theme.Info.Sources[i] = link + tail;
                }
            }
        }
    }
}
