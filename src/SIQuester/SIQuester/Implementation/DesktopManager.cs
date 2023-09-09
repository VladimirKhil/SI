using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Notions;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.Properties;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;

namespace SIQuester.Implementation;

/// <summary>
/// Implements SIQuester desktop framework logic.
/// </summary>
internal sealed class DesktopManager : PlatformManager, IDisposable
{
    internal const string STR_Definition = "{0}: {1}";
    internal const string STR_ExtendedDefinition = "{0}: {1} ({2})";

    private readonly Dictionary<string, string> _mediaFiles = new();

    private const int MAX_PATH = 260;

    public override Tuple<int, int, int>? GetCurrentItemSelectionArea() =>
        ActionMenuViewModel.Instance.PlacementTarget is TextList box ? box.GetSelectionInfo() : null;

    public override string[]? ShowOpenUI()
    {
        var openDialog = new OpenFileDialog
        {
            Title = Resources.OpenPackage,
            FileName = "",
            DefaultExt = "siq",
            Filter = Resources.SIQuestions + "|*.siq",
            Multiselect = true
        };

        var result = openDialog.ShowDialog();
        return result.HasValue && result.Value ? openDialog.FileNames : null;
    }

    public override string[]? ShowMediaOpenUI(string mediaCategory)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = mediaCategory switch
            {
                CollectionNames.ImagesStorageName => $"{Resources.Images} (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.AudioStorageName => $"{Resources.Audio} (*.mp3)|*.mp3|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.VideoStorageName => $"{Resources.Video} (*.mp4)|*.mp4|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.HtmlStorageName => $"{SIQuester.ViewModel.Properties.Resources.HtmlFiles} (*.html)|*.html|{Resources.AllFiles} (*.*)|*.*",
                _ => throw new ArgumentException($"Invalid category {mediaCategory}", nameof(mediaCategory))
            }
        };

        var result = dialog.ShowDialog();
        return result.HasValue && result.Value ? dialog.FileNames : null;
    }

    /// <summary>
    /// Сохранить пакет
    /// </summary>
    /// <param name="defaultExtension">Расширение по умолчанию</param>
    /// <param name="filter">Фильтры расширений</param>
    /// <param name="filename">Выбранный файл пакета</param>
    /// <returns>Был ли сделан выбор</returns>
    public override bool ShowSaveUI(string? title, string defaultExtension, Dictionary<string, string>? filter, ref string? filename)
    {
        int filterIndex = 0;
        return ShowSaveUICore(title, defaultExtension, filter, ref filename, ref filterIndex, null);
    }

    public override bool ShowExportUI(
        string title,
        Dictionary<string, string> filter,
        ref string filename,
        ref int filterIndex,
        out Encoding encoding,
        out bool start)
    {
        var checkBox = new Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogCheckBox(Resources.OpenFileAfterSaving, false);

        var comboBoxTitle = new Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogLabel(Resources.Encoding);
        var comboBox = new Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogComboBox();
        var encodings = Encoding.GetEncodings();
        
        foreach (var enc in encodings)
        {
            comboBox.Items.Add(new Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogComboBoxItem(enc.DisplayName));
            
            if (enc.Name == "utf-8")
            {
                comboBox.SelectedIndex = comboBox.Items.Count - 1;
            }
        }

        void handler(int ind)
        {
            comboBoxTitle.Visible = comboBox.Visible = ind == 1;
        }

        var result = ShowSaveUICore(title, null, filter, ref filename, ref filterIndex, handler, checkBox, comboBoxTitle, comboBox);

        encoding = Encoding.UTF8;

        if (result)
        {
            if (comboBox.SelectedIndex > -1 && comboBox.SelectedIndex < comboBox.Items.Count)
            {
                encoding = Encoding.GetEncoding(encodings[comboBox.SelectedIndex].Name);
            }

            start = checkBox.IsChecked;
        }
        else
        {
            start = false;
        }

        return result;
    }

    private static bool ShowSaveUICore(
        string? title,
        string? defaultExtension,
        Dictionary<string, string>? filter,
        ref string? filename,
        ref int filterIndex,
        Action<int>? fileTypeChanged,
        params Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogControl[] richUI)
    {
        if (richUI.Length > 0 && AppSettings.IsVistaOrLater)
        {
            // Show dialog with rich UI
            var dialog = new CommonSaveFileDialog
            {
                OverwritePrompt = true,
                DefaultExtension = defaultExtension,
                DefaultFileName = filename,
                AlwaysAppendDefaultExtension = true
            };

            if (fileTypeChanged != null)
            {
                dialog.FileTypeChanged += (sender, e) => fileTypeChanged(dialog.SelectedFileTypeIndex);
            }

            if (filter != null)
            {
                foreach (var item in filter)
                {
                    dialog.Filters.Add(new CommonFileDialogFilter(item.Key, item.Value));
                }
            }

            foreach (var item in richUI)
            {
                dialog.Controls.Add(item);
            }

            if (title != null)
            {
                dialog.Title = title;
            }

            bool result = dialog.ShowDialog() == CommonFileDialogResult.Ok;
            filterIndex = dialog.SelectedFileTypeIndex;

            if (result)
            {
                filename = dialog.FileName;

                if (Path.GetExtension(filename).Length == 0)
                {
                    filename += "." + dialog.Filters[filterIndex - 1].Extensions[0];

                    if (File.Exists(filename))
                    {
                        if (MessageBox.Show(string.Format(Resources.FileExistReplace, Path.GetFileName(filename)), AppSettings.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        {
                            return false;
                        }
                    }
                }
            }

            return result;
        }
        else
        {
            var filterString = new StringBuilder();

            if (filter != null)
            {
                foreach (var item in filter)
                {
                    if (filterString.Length > 0)
                    {
                        filterString.Append('|');
                    }

                    filterString.Append(item.Key).Append("|*.").Append(item.Value);
                }
            }

            var saveDialog = new SaveFileDialog
            {
                DefaultExt = defaultExtension,
                Filter = filterString.ToString(),
                FileName = filename,
                AddExtension = true
            };

            if (title != null)
            {
                saveDialog.Title = title;
            }

            var result = saveDialog.ShowDialog();
            var selected = result.HasValue && result.Value;

            if (selected)
            {
                filename = saveDialog.FileName;
            }

            filterIndex = saveDialog.FilterIndex;
            return selected;
        }
    }

    public override string? ShowImportUI(string fileExtension, string fileFilter)
    {
        var openFileDialog = new OpenFileDialog { DefaultExt = fileExtension, Filter = fileFilter };
        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }

    public override string? SelectSearchFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog 
        {
            Description = Resources.SelectSearchFolder
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    public override IMedia PrepareMedia(IMedia media, string type)
    {
        if (media.GetStream == null) // This is an external link
        {
            return media;
        }

        if (_mediaFiles.TryGetValue(media.Uri, out var fileName))
        {
            return new Media(fileName, media.StreamLength);
        }

        // This is file itself
        var tempMediaDirectory = Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName);
        Directory.CreateDirectory(tempMediaDirectory);

        fileName = Path.Combine(tempMediaDirectory, new Random().Next() + media.Uri);

        if (fileName.Length >= MAX_PATH)
        {
            fileName = fileName[..(MAX_PATH - 1)];
        }

        var stream = media.GetStream();

        if (stream == null)
        {
            return new Media(media.Uri);
        }

        using (stream.Stream)
        {
            using var fs = File.Create(fileName);
            stream.Stream.CopyTo(fs);
        }

        _mediaFiles[media.Uri] = fileName;

        return new Media(fileName, media.StreamLength);
    }

    public override void ClearMedia(IEnumerable<string> media)
    {
        foreach (var item in media)
        {
            if (_mediaFiles.TryGetValue(item, out var path) && File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception exc)
                {
                    Trace.TraceError(exc.ToString());
                }

                _mediaFiles.Remove(item);
            }
        }
    }

    public override string? AskText(string title, bool multiline = false)
    {
        var viewModel = new LinkViewModel { Title = title, IsMultiline = multiline };
        var view = new InputLinkView { DataContext = viewModel, Owner = Application.Current.MainWindow };
        return view.ShowDialog() == true ? viewModel.Uri : null;
    }

    /// <summary>
    /// Forms a document of required format.
    /// </summary>
    public override IFlowDocumentWrapper BuildDocument(SIDocument doc, ExportFormats format)
    {
        var document = new FlowDocument { ColumnWidth = double.PositiveInfinity };

        switch (format)
        {
            case ExportFormats.Dinabank:
                {
                    var paragraph = new Paragraph();

                    paragraph.AppendText(doc.Package.Name);
                    AppendInfo(doc, paragraph, doc.Package);
                    paragraph.AppendLine();

                    doc.Package.Rounds.ForEach(round =>
                    {
                        paragraph.AppendLine();
                        paragraph.AppendLine(round.Name);

                        AppendInfo(doc, paragraph, round);

                        for (int i = 0; i < round.Themes.Count; i++)
                        {
                            var theme = round.Themes[i];
                            
                            paragraph.AppendLine();
                            paragraph.Append(Resources.Theme).AppendFormat(" {0}. {1}", i + 1, (theme.Name ?? "").ToUpper().EndWithPoint());
                            
                            AppendInfo(doc, paragraph, theme);
                            
                            paragraph.AppendLine();
                            paragraph.AppendLine();

                            theme.Questions.ForEach(quest =>
                            {
                                paragraph.AppendFormat("{0}. ", quest.Price);
                                paragraph.AppendLine(quest.GetText().EndWithPoint());
                                paragraph.AppendFormat(STR_Definition, Resources.Answer, string.Join(", ", quest.Right.ToArray()).GrowFirstLetter().EndWithPoint());
                                AppendInfo(doc, paragraph, quest);
                                paragraph.AppendLine();
                            });
                        }
                    });

                    document.Blocks.Add(paragraph);
                }
                break;

            case ExportFormats.TvSI:
                {
                    var paragraph = new Paragraph();

                    doc.Package.Rounds.ForEach(
                        round => round.Themes.ForEach(
                            theme => theme.Questions.ForEach(
                                quest =>
                                {
                                    paragraph.AppendFormat("\\{0}\\", theme.Name).AppendLine();
                                    paragraph.AppendFormat("\\{0}", quest.GetText());

                                    if (quest.Info.Comments.Text.Length > 0)
                                    {
                                        paragraph.AppendFormat(". {0}: {1}", Resources.Comments, quest.Info.Comments.Text);
                                    }

                                    paragraph.Append('\\').AppendLine().Append('\\');
                                    paragraph.Append(string.Join(", ", quest.Right.ToArray()));

                                    if (quest.Info.Sources.Count > 0)
                                    {
                                        paragraph.Append('\\').AppendLine().Append('\\');
                                        paragraph.Append(string.Join(", ", doc.GetRealSources(quest.Info.Sources)));
                                    }

                                    paragraph.Append('\\').AppendLine().AppendLine();
                                })));

                    document.Blocks.Add(paragraph);
                }
                break;

            case ExportFormats.Sns:
                {
                    var paragraph = new Paragraph();

                    paragraph.AppendLine(doc.Package.Name);
                    int i = 0;

                    doc.Package.Rounds.ForEach(round =>
                    {
                        paragraph.AppendLine();
                        paragraph.AppendLine(round.Name);
                        paragraph.AppendLine();
                        paragraph.AppendLine(Resources.YourThemes);

                        round.Themes.ForEach(theme =>
                        {
                            paragraph.AppendFormat(STR_ExtendedDefinition, Resources.Theme, theme.Name.ToUpper(), ++i);
                            paragraph.AppendLine();
                        });

                        round.Themes.ForEach(theme =>
                        {
                            paragraph.AppendLine();
                            paragraph.AppendFormat(STR_Definition, Resources.Theme, theme.Name.ToUpper());
                            paragraph.AppendLine();

                            if (theme.Info.Comments.Text.Length > 0)
                            {
                                paragraph.AppendFormat(STR_ExtendedDefinition, Resources.Author, string.Join(", ", doc.GetRealAuthors(theme.Info.Authors)), theme.Info.Comments.Text);
                            }
                            else
                            {
                                paragraph.AppendFormat(STR_Definition, Resources.Author, string.Join(", ", doc.GetRealAuthors(theme.Info.Authors)));
                            }

                            paragraph.AppendLine();
                            
                            theme.Questions.ForEach(quest =>
                            {
                                paragraph.AppendLine();
                                paragraph.Append(quest.Price.ToString());
                                paragraph.AppendLine(".");
                                paragraph.AppendLine(quest.GetText().Replace(Environment.NewLine, "//").EndWithPoint());
                                paragraph.AppendLine();
                                paragraph.AppendFormat(STR_Definition, Resources.Answer, string.Join(", ", quest.Right.ToArray()).GrowFirstLetter().EndWithPoint());
                                paragraph.AppendLine();
                                paragraph.AppendFormat(STR_Definition, Resources.Comment, (quest.Info.Comments.Text.Length > 0 ? quest.Info.Comments.Text.GrowFirstLetter() : Resources.No.ToLower()).EndWithPoint());
                                paragraph.AppendLine();
                                paragraph.AppendFormat(STR_Definition, Resources.Source, (quest.Info.Sources.Count > 0 ? string.Join(", ", quest.Info.Sources.ToArray()).GrowFirstLetter() : Resources.No.ToLower()).EndWithPoint());
                                paragraph.AppendLine();
                            });
                        });
                    });

                    document.Blocks.Add(paragraph);
                }
                break;

            case ExportFormats.Db:
                {
                    var text = new StringBuilder();
                    text.AppendLine(string.Format("{0}:", Resources.Championship));
                    text.AppendLine(doc.Package.Name.EndWithPoint().GrowFirstLetter().Trim());
                    text.AppendLine();

                    var info = new StringBuilder();
                    int authorsCount = doc.Package.Info.Authors.Count;

                    if (authorsCount > 0 && !(authorsCount == 1 && doc.Package.Info.Authors[0] == Resources.Empty))
                    {
                        info.AppendLine(string.Join(Environment.NewLine, doc.GetRealAuthors(doc.Package.Info.Authors)).Trim());
                        info.AppendLine();
                    }

                    if (doc.Package.Info.Sources.Count > 0)
                    {
                        info.AppendLine(string.Join(Environment.NewLine, doc.GetRealSources(doc.Package.Info.Sources)).Trim());
                        info.AppendLine();
                    }

                    if (doc.Package.Info.Comments.Text.Length > 0)
                    {
                        info.AppendLine(doc.Package.Info.Comments.Text.GrowFirstLetter().EndWithPoint().Trim());
                        info.AppendLine();
                    }

                    if (info.Length > 0)
                    {
                        text.AppendLine("Инфо:");
                        text.Append(info);
                    }

                    var r = 1;

                    foreach (var round in doc.Package.Rounds)
                    {
                        text.AppendLine(string.Format("{0}:", Resources.Tour));
                        text.Append(round.Name.GrowFirstLetter().Trim());

                        if (round.Type == RoundTypes.Final)
                        {
                            text.Append(string.Format(" ({0})", Resources.Final.ToUpper()));
                        }

                        text.AppendLine();
                        text.AppendLine();

                        if (round.Info.Authors.Count > 0)
                        {
                            text.AppendLine(string.Format("{0}:", Resources.BaseAuthors));
                            text.AppendLine(string.Join(Environment.NewLine, doc.GetRealAuthors(round.Info.Authors)).Trim());
                            text.AppendLine();
                        }

                        if (round.Info.Sources.Count > 0)
                        {
                            text.AppendLine(string.Format("{0}:", Resources.BaseSources));
                            text.AppendLine(string.Join(Environment.NewLine, doc.GetRealSources(round.Info.Sources)).Trim());
                            text.AppendLine();
                        }

                        if (round.Info.Comments.Text.Length > 0)
                        {
                            text.AppendLine(string.Format("{0}:", Resources.Comments));
                            text.AppendLine(round.Info.Comments.Text.GrowFirstLetter().EndWithPoint().Trim());
                            text.AppendLine();
                        }

                        var i = 1;

                        foreach (var theme in round.Themes)
                        {
                            text.AppendLine(string.Format("{0} {1}:", Resources.Question, i));
                            text.AppendLine(theme.Name.EndWithPoint().GrowFirstLetter().Trim());

                            if (theme.Info.Comments.Text.Length > 0)
                            {
                                text.Append("   (");
                                text.Append(theme.Info.Comments.Text.GrowFirstLetter().Trim());
                                text.AppendLine(")");
                            }

                            var questionsCount = theme.Questions.Count;

                            for (var j = 0; j < questionsCount; j++)
                            {
                                text.Append("   ");

                                if (j < 5)
                                {
                                    text.Append(j + 1);
                                }
                                else
                                {
                                    text.Append(Resources.Reserve);
                                }

                                text.AppendLine(string.Format(". {0}", theme.Questions[j].GetText().EndWithPoint().GrowFirstLetter().Trim()));
                            }

                            text.AppendLine();
                            text.AppendLine(string.Format("{0}:", Resources.Answer));

                            for (var j = 0; j < questionsCount; j++)
                            {
                                var qLine = new StringBuilder("   ");

                                if (j < 5)
                                {
                                    qLine.Append(j + 1);
                                }
                                else
                                {
                                    qLine.Append(Resources.Reserve);
                                }

                                qLine.Append(string.Format(". {0}", theme.Questions[j].Right[0].ClearPoints().GrowFirstLetter().Trim()));
                                
                                var rightCount = theme.Questions[j].Right.Count;

                                if (rightCount > 1)
                                {
                                    qLine.Append(string.Format(" {0}: ", Resources.Accept));

                                    for (int k = 1; k < rightCount; k++)
                                    {
                                        qLine.Append(theme.Questions[j].Right[k].ClearPoints().GrowFirstLetter());

                                        if (k < rightCount - 1)
                                        {
                                            qLine.Append(", ");
                                        }
                                    }
                                }

                                if (theme.Questions[j].Info.Comments.Text.Length > 0)
                                {
                                    qLine.Append(string.Format(" ({0})", theme.Questions[j].Info.Comments.Text.ClearPoints().GrowFirstLetter().Trim()));
                                }

                                text.AppendLine(qLine.ToString().EndWithPoint());
                            }

                            static bool qHasSource(Question quest) => quest.Info.Sources.Count > 0 && quest.Info.Sources[0].Length > 3;
                            
                            if (theme.Questions.Any(qHasSource))
                            {
                                text.AppendLine();
                                text.AppendLine(string.Format("{0}:", Resources.BaseSources));

                                for (int j = 0; j < questionsCount; j++)
                                {
                                    if (qHasSource(theme.Questions[j]))
                                    {
                                        if (j < 5)
                                        {
                                            text.Append(string.Format("   {0}. ", j + 1));
                                        }
                                        else
                                        {
                                            text.Append($"   {Resources.Reserve}.");
                                        }

                                        text.AppendLine(string.Join(", ", doc.GetRealSources(theme.Questions[j].Info.Sources)).EndWithPoint().Trim());
                                    }
                                }
                            }

                            text.AppendLine();

                            var authors = new List<string>(doc.GetRealAuthors(theme.Info.Authors));

                            foreach (var quest in theme.Questions)
                            {
                                authors.AddRange(doc.GetRealAuthors(quest.Info.Authors));
                            }

                            if (authors.Count == 0)
                            {
                                authors.AddRange(doc.GetRealAuthors(round.Info.Authors));
                            }

                            if (authors.Count == 0)
                            {
                                authors.AddRange(doc.GetRealAuthors(doc.Package.Info.Authors));
                            }

                            authorsCount = authors.Count;
                            if (authorsCount > 0 && !(authorsCount == 1 && authors[0] == Resources.Empty))
                            {
                                text.AppendLine(string.Format("{0}:", Resources.BaseAuthors));
                                text.AppendLine(string.Join(", ", authors.ToArray()).Trim());
                                text.AppendLine();
                            }

                            if (theme.Info.Sources.Count > 0)
                            {
                                text.AppendLine(string.Format("{0}:", Resources.BaseSources));
                                text.AppendLine(string.Join(Environment.NewLine, doc.GetRealSources(theme.Info.Sources)).Trim());
                                text.AppendLine();
                            }

                            i++;
                        }

                        r++;
                    }

                    int counter = 0, iold = 0;

                    var fileStr = text
                        .Replace('«', '\"')
                        .Replace('»', '\"')
                        .Replace('–', '-')
                        .Replace('—', '-')
                        .Replace("…", "...")
                        .ToString();
                    
                    int fl = fileStr.Length;
                    var fileRes = new StringBuilder();

                    for (int i = 0; i < fl; i++)
                    {
                        if (fileStr[i] == '\r')
                        {
                            counter = 0;
                            fileRes.Append(fileStr.AsSpan(iold, i - iold + 2));
                            i++;
                            iold = i + 1;
                        }
                        else
                        {
                            counter++;
                            if (counter == 73)
                            {
                                while (!char.IsWhiteSpace(fileStr, i) && i > 0 && i > iold)
                                {
                                    i--;
                                }

                                if (i == iold || i == iold + 5 && char.IsDigit(fileStr[iold + 3]) && fileStr[iold + 4] == '.')
                                {
                                    i = iold + 72;

                                    while (!char.IsWhiteSpace(fileStr, i) && i < fl)
                                    {
                                        i++;
                                    }
                                }

                                fileRes.AppendLine(fileStr[iold..i]);

                                if (fileStr[i] == '\r')
                                {
                                    i++;
                                    iold = i + 1;
                                }
                                else
                                {
                                    iold = i + (char.IsWhiteSpace(fileStr, i) ? 1 : 0);
                                }

                                counter = 0;
                            }
                        }
                    }

                    var paragraph = new Paragraph();

                    foreach (var line in fileRes.ToString().Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        if (paragraph.Inlines.Count > 0)
                        {
                            paragraph.AppendLine();
                        }

                        if (line.Length > 0)
                        {
                            paragraph.AppendText(line);
                        }
                    }

                    document.Blocks.Add(paragraph);
                }
                break;
        }

        document.PageWidth = 21.0 / 2.54 * 96;
        document.PageHeight = 29.7 / 2.54 * 96;

        return new FlowDocumentWrapper(document);
    }

    private static void AppendInfo(SIDocument doc, Paragraph paragraph, InfoOwner owner)
    {
        var count = owner.Info.Authors.Count;

        if (count > 0)
        {
            paragraph.AppendLine().Append(string.Format("{0}{1}: ", Resources.BaseAuthors, count > 1 ? "ы" : ""));
            paragraph.Append(string.Join(", ", doc.GetRealAuthors(owner.Info.Authors)).EndWithPoint());
        }

        count = owner.Info.Sources.Count;

        if (count > 0)
        {
            paragraph.AppendLine().Append(string.Format("{0}{1}: ", Resources.BaseSources, count > 1 ? "и" : ""));
            paragraph.Append(string.Join(", ", doc.GetRealSources(owner.Info.Sources)).EndWithPoint());
        }

        if (owner.Info.Comments.Text.Length > 0)
        {
            paragraph.AppendLine().Append(string.Format("{0}: ", Resources.Comments));
            paragraph.Append(owner.Info.Comments.Text);
        }
    }

    public override void ExportTable(SIDocument doc, string filename)
    {
        var current = (uint)0;
        var total = (uint)doc.Package.Rounds.Sum(round => round.Themes.Count);

        var document = new FlowDocument
        {
            PagePadding = new Thickness(0.0),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Times New Roman")
        };

        var packageCaption = new Paragraph { KeepWithNext = true, Margin = new Thickness(10.0, 5.0, 0.0, 0.0) };

        var textP = new Run { FontSize = 30, FontWeight = FontWeights.Bold, Text = doc.Package.Name };
        packageCaption.Inlines.Add(textP);

        document.Blocks.Add(packageCaption);

        foreach (var round in doc.Package.Rounds)
        {
            var caption = new Paragraph { KeepWithNext = true, Margin = new Thickness(10.0, 5.0, 0.0, 0.0) };

            var textCaption = new Run { FontSize = 24, FontWeight = FontWeights.Bold, Text = round.Name };

            caption.Inlines.Add(textCaption);
            document.Blocks.Add(caption);

            var table = new Table { CellSpacing = 0.0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.0, 0.5, 0.0, 0.5) };
            var rowGroup = new TableRowGroup();
            var columnNumber = round.Themes.Max(theme => theme.Questions.Count);

            for (int i = 0; i < columnNumber; i++)
            {
                table.Columns.Add(new TableColumn());
            }

            foreach (var theme in round.Themes)
            {
                var row = new TableRow();

                foreach (var quest in theme.Questions)
                {
                    var cell = new TableCell
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        TextAlignment = TextAlignment.Center
                    };

                    var paragraph = new Paragraph { Margin = new Thickness(10.0), KeepTogether = true };
                    
                    paragraph.Inlines.Add(string.Format(round.Type == RoundTypes.Standart ? "{0}, {1}" : "{0}", theme.Name, quest.Price));
                    paragraph.Inlines.Add(new LineBreak());

                    if (quest.Type.Name != QuestionTypes.Simple)
                    {
                        if (quest.Type.Name == QuestionTypes.Sponsored)
                        {
                            paragraph.Inlines.Add(ViewModel.Properties.Resources.NoRiskQuestion.ToUpper());
                        }
                        else if (quest.Type.Name == QuestionTypes.Auction)
                        {
                            paragraph.Inlines.Add(ViewModel.Properties.Resources.StakeQuestion.ToUpper());
                        }
                        else if (quest.Type.Name == QuestionTypes.Cat)
                        {
                            paragraph.Inlines.Add(ViewModel.Properties.Resources.SecretQuestion.ToUpper());
                            paragraph.Inlines.Add(new LineBreak());

                            paragraph.Inlines.Add(string.Format(
                                "{0}, {1}",
                                quest.Type[QuestionTypeParams.Cat_Theme],
                                quest.Type[QuestionTypeParams.Cat_Cost]));
                        }
                        else if (quest.Type.Name == QuestionTypes.BagCat)
                        {
                            paragraph.Inlines.Add(ViewModel.Properties.Resources.SecretQuestion.ToUpper());
                            var knows = quest.Type[QuestionTypeParams.BagCat_Knows];
                            var cost = quest.Type[QuestionTypeParams.Cat_Cost];

                            if (cost == "0")
                            {
                                cost = Resources.NumberSetModeMinimumOrMaximumInRound;
                            }

                            if (knows == QuestionTypeParams.BagCat_Knows_Value_Never)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                                paragraph.Inlines.Add(string.Format("{0}: {1}", Resources.SecretNoQuestion, cost));
                                continue;
                            }

                            paragraph.Inlines.Add(new LineBreak());
                            paragraph.Inlines.Add(string.Format("{0}, {1}", quest.Type[QuestionTypeParams.Cat_Theme], cost));

                            if (knows == QuestionTypeParams.BagCat_Knows_Value_Before)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                                paragraph.Inlines.Add(Resources.SecretPublicPrice);
                            }

                            if (quest.Type[QuestionTypeParams.BagCat_Self] == QuestionTypeParams.BagCat_Self_Value_True)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                                paragraph.Inlines.Add(Resources.SecretCanBeGivenToSelf);
                            }
                        }
                        else // Unsupported type
                        {
                            paragraph.Inlines.Add(quest.Type.Name);

                            foreach (var param in quest.Type.Params)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                                paragraph.Inlines.Add(string.Format(STR_Definition, param.Name, param.Value));
                            }
                        }

                        paragraph.Inlines.Add(new LineBreak());
                    }

                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(quest.GetText());

                    cell.Blocks.Add(paragraph);
                    row.Cells.Add(cell);
                }

                rowGroup.Rows.Add(row);
                current++;
            }

            table.RowGroups.Add(rowGroup);
            document.Blocks.Add(table);
        }

        using var package = System.IO.Packaging.Package.Open(filename, FileMode.Create);
        using var xpsDocument = new XpsDocument(package);
        using var manager = new XpsSerializationManager(new XpsPackagingPolicy(xpsDocument), false);

        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(1056.0, 816.0); // A4
        manager.SaveAsXaml(paginator);
        manager.Commit();
    }

    public override void ShowHelp()
    {
        var helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help.pdf");

        try
        {
            Utils.Browser.Open(helpPath);
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.ToString(), App.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public override void AddToRecentCategory(string fileName) => JumpList.AddToRecentCategory(fileName);

    public override void ShowErrorMessage(string message) =>
        MessageBox.Show(message, AppSettings.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

    public override void ShowExclamationMessage(string message) =>
        MessageBox.Show(message, AppSettings.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);

    public override void ShowSelectOptionDialog(string message, params UserOption[] options)
    {
        var dialog = new TaskDialog
        {
            Caption = AppSettings.ProductName,
            Icon = TaskDialogStandardIcon.Warning,
            Text = message
        };

        foreach (var option in options)
        {
            var button = new TaskDialogCommandLink(option.Title, option.Title, option.Description);

            button.Click += (sender, e) =>
            {
                option.Callback();
                dialog.Close();
            };

            dialog.Controls.Add(button);
        }
        
        dialog.Show();
    }

    public override void Inform(string message, bool exclamation = false) =>
        MessageBox.Show(
            message,
            AppSettings.ProductName,
            MessageBoxButton.OK,
            exclamation ? MessageBoxImage.Exclamation : MessageBoxImage.Information);

    public override bool Confirm(string message) =>
        MessageBox.Show(message, AppSettings.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public override bool? ConfirmWithCancel(string message)
    {
        var result = MessageBox.Show(message, AppSettings.ProductName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            return null;
        }

        return result == MessageBoxResult.Yes;
    }

    public override bool ConfirmExclWithWindow(string message)
    {
        var window = Application.Current.MainWindow;

        if (window != null)
        {
            return MessageBox.Show(
                window,
                message,
                AppSettings.ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation) == MessageBoxResult.Yes;
        }

        return MessageBox.Show(message, AppSettings.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes;
    }

    public override void Exit() => Application.Current.MainWindow?.Close();

    public void Dispose()
    {
        
    }
}
