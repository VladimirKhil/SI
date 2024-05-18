using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Notions;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.Properties;
using SIQuester.View;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using Utils;

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

    public override string[] FontFamilies => Fonts.SystemFontFamilies.Select(ff => ff.Source).OrderBy(f => f).ToArray();

    private static readonly Dictionary<string, string> QuestionTypeMap = new()
    {
        { QuestionTypes.NoRisk, ViewModel.Properties.Resources.NoRiskQuestion.ToUpper() },
        { QuestionTypes.Stake, ViewModel.Properties.Resources.StakeQuestion.ToUpper() },
        { QuestionTypes.Secret, ViewModel.Properties.Resources.SecretQuestion.ToUpper() },
        { QuestionTypes.SecretPublicPrice, ViewModel.Properties.Resources.SecretQuestion.ToUpper() },
        { QuestionTypes.SecretNoQuestion, Resources.SecretNoQuestion.ToUpper() }
    };

    public override Tuple<int, int, int>? GetCurrentItemSelectionArea() =>
        ActionMenuViewModel.Instance.PlacementTarget is TextList box ? box.GetSelectionInfo() : null;

    public override void CreatePreview(SIDocument document)
    {
        string? fileName = null;

        var filter = new Dictionary<string, string>
        {
            [Resources.Images] = "png"
        };

        if (ShowSaveUI(null, "png", filter, ref fileName))
        {
            var view = new PackagePreview { DataContext = new PreviewViewModel(document) };
            using var source = new HwndSource(new HwndSourceParameters()) { RootVisual = view };
            
            SaveImage(view, fileName);
        }
    }

    private static void SaveImage(Control control, string path)
    {
        using var stream = File.Create(path);
        GenerateImage(control, stream);
    }

    private static void GenerateImage(Control control, Stream result)
    {
        var controlSize = RetrieveDesiredSize(control);
        var rect = new Rect(0, 0, controlSize.Width, controlSize.Height);

        var rtb = new RenderTargetBitmap((int)controlSize.Width, (int)controlSize.Height, 96, 96, PixelFormats.Pbgra32);

        control.Arrange(rect);
        rtb.Render(control);

        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(rtb));
        png.Save(result);
    }

    private static Size RetrieveDesiredSize(Control control)
    {
        control.Measure(new Size(1200.0, double.PositiveInfinity));
        return control.DesiredSize;
    }

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
                CollectionNames.ImagesStorageName => $"{Resources.Images} (*.jpg, *.jpeg, *.png, *.gif, *.webp)|*.jpg;*.jpeg;*.png;*.gif;*.webp|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.AudioStorageName => $"{Resources.Audio} (*.mp3)|*.mp3|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.VideoStorageName => $"{Resources.Video} (*.mp4)|*.mp4|{Resources.AllFiles} (*.*)|*.*",
                CollectionNames.HtmlStorageName => $"{ViewModel.Properties.Resources.HtmlFiles} (*.html)|*.html|{Resources.AllFiles} (*.*)|*.*",
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
    public override bool ShowSaveUI(string? title, string defaultExtension, Dictionary<string, string>? filter, [NotNullWhen(true)] ref string? filename)
    {
        int filterIndex = 0;
        return ShowSaveUICore(title, defaultExtension, filter, ref filename, ref filterIndex, null);
    }

    public override bool ShowExportUI(
        string title,
        Dictionary<string, string> filter,
        [NotNullWhen(true)] ref string? filename,
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
        [NotNullWhen(true)] ref string? filename,
        ref int filterIndex,
        Action<int>? fileTypeChanged,
        params Microsoft.WindowsAPICodePack.Dialogs.Controls.CommonFileDialogControl[] richUI)
    {
        if (richUI.Length > 0)
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

            var dialogResult = dialog.ShowDialog();
            filterIndex = dialog.SelectedFileTypeIndex;

            if (dialogResult == CommonFileDialogResult.Ok && dialog.FileName != null)
            {
                filename = dialog.FileName;

                if (Path.GetExtension(filename).Length == 0)
                {
                    filename += "." + dialog.Filters[filterIndex - 1].Extensions[0];

                    if (File.Exists(filename))
                    {
                        if (MessageBox.Show(
                            string.Format(Resources.FileExistReplace, Path.GetFileName(filename)),
                            AppSettings.ProductName,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.No)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
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

        fileName = Path.Combine(tempMediaDirectory, new Random().Next() + FilePathHelper.GetSafeFileName(media.Uri));

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

    public override IEnumerable<string>? AskTags(ItemsViewModel<string> tags)
    {
        var viewModel = new SelectTagsViewModel(tags);
        var view = new SelectTagsView { DataContext = viewModel, Owner = Application.Current.MainWindow };

        return view.ShowDialog() == true ? viewModel.Items : null;
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

                    var questionType = quest.TypeName;

                    if (questionType != QuestionTypes.Default)
                    {
                        if (!QuestionTypeMap.TryGetValue(questionType, out var mappedType))
                        {
                            mappedType = questionType;
                        }

                        paragraph.Inlines.Add(mappedType);

                        if (quest.Parameters != null)
                        {
                            foreach (var param in quest.Parameters)
                            {
                                if (param.Key == QuestionParameterNames.Question || param.Key == QuestionParameterNames.Answer)
                                {
                                    continue;
                                }

                                paragraph.Inlines.Add(new LineBreak());

                                if (param.Key == QuestionParameterNames.Price)
                                {
                                    printPrice(paragraph, param);
                                }
                                else if (param.Value.Type == StepParameterTypes.Simple)
                                {
                                    paragraph.Inlines.Add(string.Format(STR_Definition, param.Key, param.Value.SimpleValue));
                                }
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

        static void printPrice(Paragraph paragraph, KeyValuePair<string, StepParameter> param)
        {
            var price = param.Value.NumberSetValue;

            if (price != null)
            {
                string priceValue;

                if (price.Maximum == 0)
                {
                    priceValue = Resources.NumberSetModeMinimumOrMaximumInRound;
                }
                else
                {
                    priceValue = price.ToString();
                }

                paragraph.Inlines.Add(string.Format(STR_Definition, Resources.Price, priceValue));
            }
        }
    }

    public override void ShowHelp()
    {
        var helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.HelpFile);

        try
        {
            Browser.Open(helpPath);
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

    public override void CopyInfo(object info) => Clipboard.SetText(JsonSerializer.Serialize(info));

    public override Dictionary<string, JsonElement>? PasteInfo() => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Clipboard.GetText());

    public override string CompressImage(string imageUri)
    {
        var extension = Path.GetExtension(imageUri);

        if (extension != ".jpg" && extension != ".png")
        {
            return imageUri;
        }

        var bitmapImage = new BitmapImage(new Uri(imageUri));

        var width = bitmapImage.PixelWidth;
        var height = bitmapImage.PixelHeight;

        const double TargetPixelSize = 800.0;
        const int TargetQualityLevel = 90;

        if (width <= TargetPixelSize && height <= TargetPixelSize)
        {
            return imageUri;
        }

        var widthScale = TargetPixelSize / width;
        var heightScale = TargetPixelSize / height;

        var scale = Math.Min(widthScale, heightScale);

        var resizedImage = new TransformedBitmap(bitmapImage, new ScaleTransform(scale, scale));

        BitmapEncoder encoder = extension == ".jpg" ? new JpegBitmapEncoder
        {
            QualityLevel = TargetQualityLevel
        } : new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(resizedImage));

        var fileName = Path.GetFileName(imageUri);
        var outputDir = Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, fileName);

        using (var fileStream = new FileStream(outputPath, FileMode.Create))
        {
            encoder.Save(fileStream);
        }

        return outputPath;
    }

    public void Dispose()
    {
        
    }
}
