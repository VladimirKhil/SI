using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Notions;
using System.Xml;
using SIQuester.ViewModel.Properties;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Core;
using System.IO;

namespace SIQuester.ViewModel
{
    public sealed class ImportDBStorageViewModel: WorkspaceViewModel
    {
        public override string Header => Resources.DBStorage;

        private DBNode[] _tours = null;

        public DBNode[] Tours
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
            get { return _isProgress; }
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
                _tours = await LoadNodes("SVOYAK");
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

        public ImportDBStorageViewModel(StorageContextViewModel storageContextViewModel)
        {
            _storageContextViewModel = storageContextViewModel;
        }

        public void Select(DBNode item)
        {
            async Task<QDocument> loader()
            {
                var siDoc = await SelectAsync(item);
                return new QDocument(siDoc, _storageContextViewModel) { FileName = siDoc.Package.Name, Changed = true };
            };

            OnNewItem(new DocumentLoaderViewModel(item.Name, loader));
        }

        internal async Task<SIDocument> SelectAsync(DBNode item)
        {
            var doc = await Utils.GetXml(item.Key);
            var manager = new XmlNamespaceManager(doc.NameTable);

            var siDoc = SIDocument.Create(doc.SelectNodes(@"/tournament/Title", manager)[0].InnerText.GrowFirstLetter().ClearPoints(), Resources.EmptyValue);
            siDoc.Package.Info.Comments.Text += string.Format(Resources.DBStorageComment, doc["tournament"]["FileName"].InnerText);
            string s = doc["tournament"]["Info"].InnerText;
            if (s.Length > 0)
                siDoc.Package.Info.Comments.Text += string.Format("\r\nИнфо: {0}", s);
            s = doc["tournament"]["URL"].InnerText;
            if (s.Length > 0)
                siDoc.Package.Info.Comments.Text += string.Format("\r\nURL: {0}", s);
            s = doc["tournament"]["PlayedAt"].InnerText;
            if (s.Length > 0)
                siDoc.Package.Info.Comments.Text += string.Format("\r\nИграно: {0}", s);
            s = doc["tournament"]["Editors"].InnerText;
            if (s.Length > 0)
                siDoc.Package.Info.Authors[0] = $"{Resources.Editors}: {s}";

            var round = siDoc.Package.CreateRound(RoundTypes.Standart, Resources.EmptyValue);

            var nodeList2 = doc.SelectNodes(@"/tournament/question", manager);

            foreach (XmlNode node2 in nodeList2)
            {
                var text = node2["Question"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var ans = node2["Answer"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var sour = node2["Sources"].InnerText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                int i = 0, j = 0;
                var themeName = new StringBuilder();
                while (i < text.Length && !(text[i].Length > 3 && text[i].Substring(0, 3) == "   "))
                {
                    if (themeName.Length > 0)
                        themeName.Append(' ');
                    themeName.Append(text[i++]);
                }

                var theme = round.CreateTheme(themeName.ToString().GrowFirstLetter().ClearPoints());
                var authorsText = node2["Authors"].InnerText;
                if (!string.IsNullOrWhiteSpace(authorsText))
                    theme.Info.Authors.Add(authorsText);

                var themeComments = new StringBuilder(node2["Comments"].InnerText);

                while (i < text.Length && text[i].Length > 4 && text[i].Substring(0, 3) == "   " && !char.IsDigit(text[i][3]))
                {
                    if (themeComments.Length > 0)
                        themeComments.Append(' ');
                    themeComments.Append(text[i++]);
                }

                theme.Info.Comments.Text = themeComments.ToString();

                bool final = true;
                Question quest = null;
                var qText = new StringBuilder();
                i--;

                #region questionsReading

                while (++i < text.Length)
                {
                    text[i] = text[i].Trim();
                    string js = (j + 1).ToString();
                    if (text[i].Substring(0, js.Length) == js && (text[i].Length > 3 && text[i].Substring(js.Length, 2) == ". " || text[i].Length > 4 && text[i].Substring(js.Length, 3) == "0. "))
                    {
                        j++;
                        final = false;
                        if (qText.Length > 0)
                        {
                            quest = theme.CreateQuestion(10 * (j - 1));
                            quest.Type.Name = QuestionTypes.Simple;
                            quest.Scenario.Clear();
                            quest.Scenario.Add(qText.ToString().GrowFirstLetter().ClearPoints());
                        }
                        int add = 3;
                        if (text[i].Substring(js.Length, 2) == ". ")
                            add = 2;

                        qText = new StringBuilder(text[i].Substring(js.Length + add));
                    }
                    else
                    {
                        if (qText.Length > 0)
                            qText.Append(' ');
                        qText.Append(text[i]);
                    }
                }

                if (final)
                {
                    quest = theme.CreateQuestion(0);
                    quest.Right[0] = node2["Answer"].InnerText.Trim().GrowFirstLetter().ClearPoints();
                }
                else
                    quest = theme.CreateQuestion(10 * j);

                quest.Scenario.Clear();
                quest.Scenario.Add(qText.ToString().GrowFirstLetter().ClearPoints());
                quest.Type.Name = QuestionTypes.Simple;

                #endregion

                #region answersReading

                int number = 0, number2 = -1, multiplier = 1;

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
                            number = number * 10 + int.Parse(ans[i][k++].ToString());

                        if (!((ans[i].Length > k + 2 && ans[i].Substring(k, 2) == ". " || ans[i].Length > 3 + k && ans[i].Substring(k, 3) == "0. ")))
                            number = -1;
                        if (number >= 0)
                        {
                            if (qText.Length > 0 && theme.Questions.Count > number2)
                                theme.Questions[number2].Right[0] = qText.ToString().GrowFirstLetter().ClearPoints();

                            if (number2 == -1)
                                multiplier = number;
                            number2 = number / multiplier - 1;
                            int add = 3;
                            if (ans[i].Substring(k, 2) == ". ")
                                add = 2;
                            qText = new StringBuilder(ans[i].Substring(k + add));
                        }
                        else
                        {
                            if (qText.Length > 0)
                                qText.Append(' ');
                            qText.Append(ans[i]);
                        }
                    }

                    if (theme.Questions.Count > number2)
                        theme.Questions[number2].Right[0] = qText.ToString().GrowFirstLetter().ClearPoints();
                }

                #endregion

                #region sourcesReading

                i = -1;
                qText = new StringBuilder();
                number = 0;
                number2 = 0;

                while (++i < sour.Length)
                {
                    sour[i] = sour[i].Trim();
                    number = 0;
                    int k = 0;
                    while (char.IsDigit(sour[i][k]))
                        number = number * 10 + int.Parse(sour[i][k++].ToString());
                    number--;
                    if (!((sour[i].Length > k + 2 && sour[i].Substring(k, 2) == ". " || sour[i].Length > 3 + k && sour[i].Substring(k, 3) == "0. ")))
                        number = -1;
                    if (number >= 0)
                    {
                        if (qText.Length > 0 && theme.Questions.Count > number2)
                            theme.Questions[number2].Info.Sources.Add(qText.ToString().GrowFirstLetter().ClearPoints());

                        number2 = number;
                        int add = 3;
                        if (sour[i].Substring(k, 2) == ". ")
                            add = 2;
                        qText = new StringBuilder(sour[i].Substring(k + add));
                    }
                    else
                    {
                        if (qText.Length > 0)
                            qText.Append(' ');
                        qText.Append(sour[i]);
                    }
                }

                if (theme.Questions.Count > number2 && qText.Length > 0)
                    theme.Questions[number2].Info.Sources.Add(qText.ToString().GrowFirstLetter().ClearPoints());

                if (number2 == -1)
                    theme.Info.Sources.Add(node2["Sources"].InnerText);

                #endregion
            }

            return siDoc;
        }

        public async void LoadChildren(DBNode node)
        {
            IsProgress = true;
            node.Children = null;

            try
            {
                node.Children = await LoadNodes(node.Key);
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
                Select(node);
        }

        internal static async Task<DBNode[]> LoadNodes(string filename)
        {
            var xmlDocument = await Utils.GetXml(filename);

            var manager = new XmlNamespaceManager(xmlDocument.NameTable);
            var nodeList = xmlDocument.SelectNodes(@"/tournament/tour", manager);

            var result = new List<DBNode>();
            var trim = filename == "SVOYAK";

            foreach (XmlNode node in nodeList)
            {
                var type = node["Type"].InnerText;
                var isLeaf = type == "Т" || type == "Ч" && node["ChildrenNum"].InnerText == "1";

                result.Add(new DBNode
                {
                    Name = string.Format("{0} {1} (тем: {2})", node["Title"].InnerText, node["PlayedAt"].InnerText, node["QuestionsNum"].InnerText),
                    Key = trim ? Path.GetFileNameWithoutExtension(node["FileName"].InnerText) : node["FileName"].InnerText,
                    Children = isLeaf ? Array.Empty<DBNode>() : new DBNode[] { null }
                });
            }

            return result.ToArray();
        }
    }
}
