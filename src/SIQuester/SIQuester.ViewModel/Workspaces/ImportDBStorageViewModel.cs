using Microsoft.Extensions.Logging;
using Notions;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.Properties;
using System.Text;
using System.Xml;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents view model for importing chgk database packages.
/// </summary>
public sealed class ImportDBStorageViewModel : WorkspaceViewModel
{
    private const string RootNodeName = "SVOYAK";

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public override string Header => Resources.DBStorage;

    private DBNode[]? _tours = null;

    public DBNode[]? Tours
    {
        get
        {
            if (_tours == null)
            {
                LoadTours();
            }

            return _tours;
        }
    }

    private bool _isProgress = false;

    public bool IsProgress
    {
        get => _isProgress;
        set
        {
            if (_isProgress != value)
            {
                _isProgress = value;
                OnPropertyChanged();
            }
        }
    }

    private async void LoadTours()
    {
        IsProgress = true;

        try
        {
            _tours = await LoadNodesAsync(RootNodeName, _cancellationTokenSource.Token);
            OnPropertyChanged(nameof(Tours));
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            IsProgress = false;
        }
    }

    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly AppOptions _appOptions;
    private readonly IChgkDbClient _chgkDbClient;
    private readonly ILoggerFactory _loggerFactory;

    public ImportDBStorageViewModel(
        StorageContextViewModel storageContextViewModel,
        IChgkDbClient chgkDbClient,
        AppOptions appOptions,
        ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _appOptions = appOptions;
        _chgkDbClient = chgkDbClient;
        _loggerFactory = loggerFactory;
    }

    public async Task SelectNodeAsync(DBNode item)
    {
        async Task<QDocument> loader(CancellationToken cancellationToken)
        {
            var siDoc = await SelectAsync(item, cancellationToken);
            return new QDocument(siDoc, _storageContextViewModel, _loggerFactory) { FileName = siDoc.Package.Name, Changed = true };
        };

        var loaderViewModel = new DocumentLoaderViewModel(item.Name);
        OnNewItem(loaderViewModel);

        try
        {
            await loaderViewModel.LoadAsync(loader);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _cancellationTokenSource.Cancel();
        // TODO: await tasks cancellation to finish
        _cancellationTokenSource.Dispose();

        base.Dispose(disposing);
    }

    internal async Task<SIDocument> SelectAsync(DBNode item, CancellationToken cancellationToken)
    {
        var doc = await _chgkDbClient.GetXmlAsync(item.Key, cancellationToken);
        var manager = new XmlNamespaceManager(doc.NameTable);

        var siDocument = SIDocument.Create(
            doc.SelectNodes(@"/tournament/Title", manager)[0].InnerText.GrowFirstLetter().ClearPoints(),
            Resources.EmptyValue);

        siDocument.Package.Info.Comments.Text += string.Format(
            Resources.DBStorageComment,
            _chgkDbClient.ServiceUri,
            doc["tournament"]?["FileName"]?.InnerText);

        var s = doc["tournament"]?["Info"]?.InnerText ?? "";

        if (s.Length > 0)
        {
            siDocument.Package.Info.Comments.Text += string.Format("\r\n{0}: {1}", Resources.Info, s);
        }

        s = doc["tournament"]["URL"].InnerText;

        if (s.Length > 0)
        {
            siDocument.Package.Info.Comments.Text += string.Format("\r\nURL: {0}", s);
        }

        s = doc["tournament"]["PlayedAt"].InnerText;

        if (s.Length > 0)
        {
            siDocument.Package.Info.Comments.Text += string.Format("\r\n{0}: {1}", Resources.Played, s);
        }

        s = doc["tournament"]["Editors"].InnerText;

        if (s.Length > 0)
        {
            siDocument.Package.Info.Authors[0] = $"{Resources.Editors}: {s}";
        }

        var round = siDocument.Package.CreateRound(RoundTypes.Standart, Resources.EmptyValue);

        var nodeList2 = doc.SelectNodes(@"/tournament/question", manager);

        foreach (XmlNode node2 in nodeList2)
        {
            var text = node2["Question"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var ans = node2["Answer"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var sour = node2["Sources"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0, j = 0;
            var themeName = new StringBuilder();

            while (i < text.Length && !(text[i].Length > 3 && text[i][..3] == "   "))
            {
                if (themeName.Length > 0)
                {
                    themeName.Append(' ');
                }

                themeName.Append(text[i++]);
            }

            var theme = round.CreateTheme(themeName.ToString().GrowFirstLetter().ClearPoints());
            var authorsText = node2["Authors"].InnerText;

            if (!string.IsNullOrWhiteSpace(authorsText))
            {
                theme.Info.Authors.Add(authorsText);
            }

            var themeComments = new StringBuilder(node2["Comments"].InnerText);

            while (i < text.Length && text[i].Length > 4 && text[i][..3] == "   " && !char.IsDigit(text[i][3]))
            {
                if (themeComments.Length > 0)
                {
                    themeComments.Append(' ');
                }

                themeComments.Append(text[i++]);
            }

            theme.Info.Comments.Text = themeComments.ToString();

            bool final = true;
            var qText = new StringBuilder();
            i--;

            Question quest;

            #region questionsReading

            while (++i < text.Length)
            {
                text[i] = text[i].Trim();
                var js = (j + 1).ToString();

                if (text[i][..js.Length] == js && (text[i].Length > 3 && text[i].Substring(js.Length, 2) == ". " || text[i].Length > 4 && text[i].Substring(js.Length, 3) == "0. "))
                {
                    j++;
                    final = false;

                    if (qText.Length > 0)
                    {
                        quest = theme.CreateQuestion(10 * (j - 1)); // Upgraded later
                        quest.Type.Name = QuestionTypes.Simple;
                        quest.Scenario.Clear();
                        quest.Scenario.Add(qText.ToString().GrowFirstLetter().ClearPoints());
                    }

                    int add = 3;

                    if (text[i].Substring(js.Length, 2) == ". ")
                    {
                        add = 2;
                    }

                    qText = new StringBuilder(text[i][(js.Length + add)..]);
                }
                else
                {
                    if (qText.Length > 0)
                    {
                        qText.Append(' ');
                    }

                    qText.Append(text[i]);
                }
            }

            if (final)
            {
                quest = theme.CreateQuestion(0);
                quest.Right[0] = node2["Answer"].InnerText.Trim().GrowFirstLetter().ClearPoints();
            }
            else
            {
                quest = theme.CreateQuestion(10 * j);
            }

            quest.Scenario.Clear();
            quest.Scenario.Add(qText.ToString().GrowFirstLetter().ClearPoints());
            quest.Type.Name = QuestionTypes.Simple;

            #endregion

            #region answersReading

            int number, number2 = -1, multiplier = 1;

            if (!final)
            {
                i = -1;
                qText = new StringBuilder();

                while (++i < ans.Length)
                {
                    ans[i] = ans[i].Trim();
                    number = 0;
                    int k = 0;

                    while (char.IsDigit(ans[i][k]))
                    {
                        number = number * 10 + int.Parse(ans[i][k++].ToString());
                    }

                    if (!((ans[i].Length > k + 2 && ans[i].Substring(k, 2) == ". " || ans[i].Length > 3 + k && ans[i].Substring(k, 3) == "0. ")))
                    {
                        number = -1;
                    }

                    if (number >= 0)
                    {
                        if (qText.Length > 0 && theme.Questions.Count > number2)
                        {
                            theme.Questions[number2].Right[0] = qText.ToString().GrowFirstLetter().ClearPoints();
                        }

                        if (number2 == -1)
                        {
                            multiplier = number;
                        }

                        number2 = number / multiplier - 1;
                        int add = 3;

                        if (ans[i].Substring(k, 2) == ". ")
                        {
                            add = 2;
                        }

                        qText = new StringBuilder(ans[i][(k + add)..]);
                    }
                    else
                    {
                        if (qText.Length > 0)
                        {
                            qText.Append(' ');
                        }

                        qText.Append(ans[i]);
                    }
                }

                if (theme.Questions.Count > number2)
                {
                    theme.Questions[number2].Right[0] = qText.ToString().GrowFirstLetter().ClearPoints();
                }
            }

            #endregion

            #region sourcesReading

            i = -1;
            qText = new StringBuilder();
            number2 = 0;

            while (++i < sour.Length)
            {
                sour[i] = sour[i].Trim();
                number = 0;
                int k = 0;

                while (char.IsDigit(sour[i][k]))
                {
                    number = number * 10 + int.Parse(sour[i][k++].ToString());
                }

                number--;

                if (!((sour[i].Length > k + 2 && sour[i].Substring(k, 2) == ". " || sour[i].Length > 3 + k && sour[i].Substring(k, 3) == "0. ")))
                {
                    number = -1;
                }

                if (number >= 0)
                {
                    if (qText.Length > 0 && theme.Questions.Count > number2)
                    {
                        theme.Questions[number2].Info.Sources.Add(qText.ToString().GrowFirstLetter().ClearPoints());
                    }

                    number2 = number;
                    int add = 3;

                    if (sour[i].Substring(k, 2) == ". ")
                    {
                        add = 2;
                    }

                    qText = new StringBuilder(sour[i][(k + add)..]);
                }
                else
                {
                    if (qText.Length > 0)
                    {
                        qText.Append(' ');
                    }

                    qText.Append(sour[i]);
                }
            }

            if (theme.Questions.Count > number2 && qText.Length > 0)
            {
                theme.Questions[number2].Info.Sources.Add(qText.ToString().GrowFirstLetter().ClearPoints());
            }

            if (number2 == -1)
            {
                theme.Info.Sources.Add(node2["Sources"].InnerText);
            }

            #endregion
        }

        if (_appOptions.UpgradeNewPackages)
        {
            siDocument.Upgrade();
        }

        return siDocument;
    }

    public async void LoadChildren(DBNode node)
    {
        IsProgress = true;
        node.Children = null;

        try
        {
            node.Children = await LoadNodesAsync(node.Key, _cancellationTokenSource.Token);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            IsProgress = false;
        }

        if (node.Children.Length == 0)
        {
            await SelectNodeAsync(node);
        }
    }

    internal async Task<DBNode[]> LoadNodesAsync(string filename, CancellationToken cancellationToken)
    {
        var xmlDocument = await _chgkDbClient.GetXmlAsync(filename, cancellationToken);

        var manager = new XmlNamespaceManager(xmlDocument.NameTable);
        var nodeList = xmlDocument.SelectNodes(@"/tournament/tour", manager);

        var result = new List<DBNode>();
        var trim = filename == RootNodeName;

        foreach (XmlNode node in nodeList)
        {
            var type = node["Type"].InnerText;
            var isLeaf = type == "Т" || type == "Ч" && node["ChildrenNum"].InnerText == "1";

            result.Add(new DBNode
            {
                Name = $"{node["Title"].InnerText} {node["PlayedAt"].InnerText} ({Resources.OfThemes}: {node["QuestionsNum"].InnerText})",
                Key = trim ? Path.GetFileNameWithoutExtension(node["FileName"].InnerText) : node["FileName"].InnerText,
                Children = isLeaf ? Array.Empty<DBNode>() : new DBNode[] { null }
            });
        }

        return result.ToArray();
    }
}
