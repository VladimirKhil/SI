using Lingware.Spard;
using Lingware.Spard.Common;
using Lingware.Spard.Expressions;
using Notions;
using QTxtConverter.Properties;
using SIPackages;
using SIPackages.Core;
using Spard.Data;
using Spard.Exceptions;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace QTxtConverter;

/// <summary>
/// Performs all operations related to text import.
/// </summary>
public sealed class QConverter
{
    public event EventHandler<SplitErrorEventArgs>? ParseError;
    public event EventHandler<ReadErrorEventArgs>? ReadError;
    public event Action<int>? Progress;

    /// <summary>
    /// Maximum question length used in calculating question template.
    /// </summary>
    private const int MaximumQuestionLength = 1500;

    private const string LineTemplate = "<Line>";
    private const string LineDefinition = "<SP>*[lazy](<BR><SP>*)+";

    /// <summary>
    /// Maximum number of questions to use in template building.
    /// </summary>
    private const int MaximumUsedQuestionsCount = 10;

    /// <summary>
    /// Случайным образом перемешать список
    /// </summary>
    /// <typeparam name="T">Тип элементов списка</typeparam>
    /// <param name="list">Исходный список</param>
    /// <returns>Массив, содержащий элементы списка, расположенные в случайном порядке</returns>
    private static T[] Shuffle<T>(List<T> list)
    {
        var result = new T[list.Count];
        int k = 0;
        var rand = new Random();

        while (list.Count > 0)
        {
            int pos = rand.Next(list.Count);
            result[k++] = list[pos];
            list.RemoveAt(pos);
        }

        return result;
    }

    /// <summary>
    /// Splits text into array of themes and questions.
    /// </summary>
    /// <param name="source">Text containing questions.</param>
    /// <returns>Array of splitted themes and questions.</returns>
    public SIPart[][]? ExtractQuestions(string source)
    {
        var list = new List<SIPart[]>();
        int progress = 0, progressBase = 0;

        var expressionTree = Spard.TreeTransformer.Create(Resources.QuestsExtractorRules3);
        expressionTree.Mode = Spard.Core.TransformMode.Function;
        expressionTree.ProgressChanged += (pos) => progress = pos;

        var skipTree = Spard.TreeTransformer.Create(Resources.ExtractorRules2);
        skipTree.Mode = Spard.Core.TransformMode.Function;
        skipTree.ProgressChanged += (pos) => progress = pos;

        int previousPosition = -1;
        var matchEnumerator = expressionTree.Transform(source).GetEnumerator();
        object? match = null;
        var next = false;

        do
        {
            try
            {
                while ((next = matchEnumerator.MoveNext()) && (match = matchEnumerator.Current) != null)
                {
                    if (match is not TupleValue tupleValue) // Head
                    {
                        list.Add(new SIPart[] { new SIPart() { Value = match.ToString() ?? "" } });
                    }
                    else
                    {
                        var siParts = new List<SIPart>();
                        var tupleValues = new Queue<TupleValue>(new[] { tupleValue });

                        while (tupleValues.Any())
                        {
                            var value = tupleValues.Dequeue();

                            foreach (var namedValue in value.Items.Cast<NamedValue>())
                            {
                                if (namedValue.Name == "Quest")
                                {
                                    siParts.Add(new SIPart { Value = namedValue.Value.ToString() ?? "" });
                                }
                                else if (namedValue.Name == "Tail")
                                {
                                    if (namedValue.Value is TupleValue tv)
                                    {
                                        tupleValues.Enqueue(tv);
                                    }
                                    else if (namedValue.Value is NamedValue nv && nv.Name == "Quest")
                                    {
                                        siParts.Add(new SIPart { Value = nv.Value.ToString() ?? "" });
                                    }
                                }
                            }
                        }

                        list.Add(siParts.ToArray());
                    }

                    previousPosition = progress;
                    Progress?.Invoke(progressBase + progress);
                }
            }
            catch (TransformException)
            {
                if (ParseError == null)
                {
                    return null;
                }

                var args = new SplitErrorEventArgs(source) { Cancel = false, SourcePosition = progressBase + progress };
                ParseError(null, args);

                if (args.Cancel)
                {
                    return null;
                }

                if (args.Skip)
                {
                    source = source.Substring(progress);
                    progressBase += progress;
                    progress = 0;
                    var last = list[^1];
                    var skipReader = skipTree.StepTransform(source).GetEnumerator();
                    skipReader.MoveNext();
                    var add = new string(skipReader.Current.Cast<char>().ToArray()) ?? "";
                    last[^1].Value += add; // Здесь также должна быть вычитанная строка

                    source = source[(add.Length)..];
                    progressBase += add.Length;

                    // Попробуем подцепить "хвост" к предыдущей теме

                    var first = last[0].Value.TrimStart();
                    var num = new StringBuilder();
                    int index = 0;

                    while (index < first.Length && char.IsDigit(first[index]))
                    {
                        num.Append(first[index++]);
                    }

                    var price = num.ToString();

                    if (int.TryParse(price, out int number))
                    {
                        var counter = last.Length + 1;
                        var appendTree = TextMatchTreeTransformer.Create(string.Format(Resources.AppendRules, price, counter));
                        appendTree.Mode = TransformMode.Function;
                        appendTree.ProgressChanged += (pos) => progress = pos;

                        var matchesEnumerator = appendTree.Transform(source).GetEnumerator();
                        Match<char>? matches = null;

                        if (matchesEnumerator.MoveNext() && (matches = matchesEnumerator.Current) != null && matches.Matches.Count > 0)
                        {
                            var newLast = new List<SIPart>(last);
                            var qMatch = matches.Matches["Quest"];
                            var qText = qMatch.Value.ToString();
                            newLast.Add(new SIPart() { Index = qMatch.Index, Value = qText });
                            list[^1] = newLast.ToArray();

                            source = source.Substring(qText.Length);
                            progressBase += qText.Length;
                        }
                    }

                    matchEnumerator = expressionTree.Transform(source).GetEnumerator();
                    continue;
                }

                source = args.Source;

                if (previousPosition == -1)
                    previousPosition = progress;
                else
                    list.RemoveAt(list.Count - 1);

                source = source.Substring(progressBase + previousPosition);

                matchEnumerator = expressionTree.Transform(source).GetEnumerator();

                progressBase += previousPosition;
                previousPosition = 0;
                progress = 0;
                continue;
            }
        } while (next);

        return list.ToArray();
    }

    /// <summary>
    /// Generates question template.
    /// </summary>
    /// <param name="list">Array of themes and questions in plain text format.</param>
    /// <param name="useStandardLogic">Should standard logic be used.</param>
    /// <returns>Generated template with question (and answer for non-standard logic).</returns>
    private static SITemplate CreateQuestionTemplate(SIPart[][] list, bool useStandardLogic)
    {
        var questionsList = new List<SIPart>();
        var answersList = new List<SIPart>();
        var questionIndiciesList = new List<int>();
        var numListAns = new List<int>();

        if (useStandardLogic)
        {
            BuildQuestionListInStandardLogic(list, questionsList, questionIndiciesList);
            return CreateQuestionTemplateNew(useStandardLogic, questionsList);
        }
        else
        {
            BuildQuestionListInNonStandardLogic(list, questionsList, answersList, questionIndiciesList, numListAns);
            // TODO: create new template builing code for non-standard logic and remove all the code below
        }

        var questions = questionsList.ToArray();
        var answers = answersList.ToArray();
        var numAns = numListAns.ToArray();

        var oper = new CombinedStringsOperator();

        for (var i = 0; i < questions.Length; i++)
        {
            oper.Add(
                new CombinedString(
                    questions[i].Value.Length > MaximumQuestionLength
                    ? string.Concat(
                        questions[i].Value.AsSpan(0, 5),
                        questions[i].Value.AsSpan(questions[i].Value.Length - MaximumQuestionLength + 5, MaximumQuestionLength - 5))
                    : questions[i].Value,
                i));
        }

        var numbers = Shuffle(questionIndiciesList);
        var commonQuestionString = oper.CreateCombination(numbers).ToString();

        string? commonAnswerString = null;

        if (!useStandardLogic)
        {
            oper.Clear();

            for (int i = 0; i < answers.Length; i++)
            {
                oper.Add(new CombinedString(
                    answers[i].Value.Length > MaximumQuestionLength ?
                        string.Concat(answers[i].Value.AsSpan(0, 5), answers[i].Value.AsSpan(answers[i].Value.Length - MaximumQuestionLength + 5, MaximumQuestionLength - 5))
                        : answers[i].Value,
                    i));
            }

            commonAnswerString = oper.CreateCombination(numAns).ToString();
        }

        var use = new Random().Next(questions.Length);
        var match = StringManager.BestCommonMatch(commonQuestionString, questions[use].Value, StringManager.TemplateSearchingNorm, true);
        commonQuestionString = ClearMatch(commonQuestionString, match, out int[] res);

        int len = res.Length;
        var delta = new int[len + 1];

        delta[0] = res[0] + 1;

        for (int i = 1; i < len; i++)
        {
            delta[i] = res[i] - res[i - 1];
        }

        delta[len] = questions[use].ToString().Length - (len > 0 ? res[len - 1] : 0);
        int dmax = -1, dmax2 = -2, posmax = 0, posmax2 = 0;

        for (int i = 0; i < len + 1; i++)
        {
            if (delta[i] > dmax)
            {
                dmax2 = dmax;
                posmax2 = posmax;
                dmax = delta[i];
                posmax = i;
            }
            else if (delta[i] > dmax2)
            {
                dmax2 = delta[i];
                posmax2 = i;
            }
        }

        var numberAdded = false;
        var qTemplate = new StringBuilder();
        var aTemplate = new StringBuilder();
        var convertText = new StringBuilder();

        convertText.AppendLine("[X=](' |'\t|'.|':|'=|',|'||'&|'@|'^|'$|'!|'?|'*|'+|'-|'(|')|'[|']|'<|'>|'{|'}|'\"|'') => ''[X]");
        var converter = TextTreeTransformer.Create(convertText.ToString());
        converter.Mode = TransformMode.Modification;

        int needAdd = 1;
        int multiplier = 1;

        if (useStandardLogic)
        {
            for (int i = 0; i < len + 1; i++)
            {
                if (delta[i] != 1)
                {
                    bool found = false;
                    if (!numberAdded)
                    {
                        numberAdded = true;
                        found = true;
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");
                        qTemplate.Append("<Number>");
                        qTemplate.Append("<Some2>");

                        int j = i;
                        while (commonQuestionString[j++] == '0')
                            multiplier *= 10;
                    }
                    if (i == Math.Min(posmax, posmax2))
                    {
                        qTemplate.Append("<QText>");
                        found = true;
                    }
                    if (i == Math.Max(posmax, posmax2))
                    {
                        qTemplate.Append("<Answer>");
                        found = true;
                    }
                    needAdd = 0;
                    if (!found)
                    {
                        bool val = false;
                        var top = i < res.Length ? res[i] : questions[use].Value.Length;
                        for (int j = res[i - 1] + 1; j < top; j++)
                            if (!char.IsWhiteSpace(questions[use].Value[j]))
                            {
                                val = true;
                                break;
                            }
                        if (val)
                            qTemplate.Append("<Some>");
                        else
                        {
                            var item = qTemplate.Length > 1 ? qTemplate[qTemplate.Length - 1] : '\n';
                            needAdd = item != '\r' && item != '\n' ? 1 : 0;
                        }
                    }                        
                }

                if (i < len)
                {
                    bool add = true;
                    if (delta[i] == 1 && char.IsWhiteSpace(commonQuestionString[i]) && commonQuestionString[i] != '\r' && commonQuestionString[i] != '\n')
                        if (i > 0 && (char.IsWhiteSpace(commonQuestionString[i - 1]) && commonQuestionString[i - 1] != '\r' && commonQuestionString[i - 1] != '\n' || commonQuestionString[i - 1] == '\n') || i < len - 1 && commonQuestionString[i + 1] == '\r')
                            add = false;
                        else if (i > 0 && delta[i - 1] != 1 && (i < len - 1 && delta[i + 1] == 1 || i == len - 1))
                            add = false;
                        else if (i < len - 1 && delta[i + 1] != 1 && (i > 0 && delta[i - 1] == 1 || i == 0))
                            add = false;
                    if (add)
                    {
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");

                        qTemplate.Append(converter.TransformToText(commonQuestionString[i].ToString()));
                        needAdd = (commonQuestionString[i] != '\r' && commonQuestionString[i] != '\n') ? (delta[i] > 1 ? 1 : 2) : 0;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < len + 1; i++)
            {
                if (delta[i] != 1)
                {
                    bool found = false;
                    if (!numberAdded)
                    {
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");

                        numberAdded = true;
                        found = true;
                        qTemplate.Append("<Number>");
                        qTemplate.Append("<Some2>");
                        int j = i;
                        while (commonQuestionString[j++] == '0')
                            multiplier *= 10;
                    }
                    if (i == posmax)
                    {
                        qTemplate.Append("<QText>");
                        found = true;
                    }
                    if (!found)
                        qTemplate.Append("<Some>");
                    needAdd = 0;
                }

                if (i < len)
                {
                    bool add = true;
                    if (delta[i] == 1 && char.IsWhiteSpace(commonQuestionString[i]) && commonQuestionString[i] != '\r' && commonQuestionString[i] != '\n')
                        if (i > 0 && (char.IsWhiteSpace(commonQuestionString[i - 1]) && commonQuestionString[i - 1] != '\r' && commonQuestionString[i - 1] != '\n' || commonQuestionString[i - 1] == '\n') || i < len - 1 && commonQuestionString[i + 1] == '\r')
                            add = false;
                        else if (i > 0 && delta[i - 1] != 1 && (i < len - 1 && delta[i + 1] == 1 || i == len - 1))
                            add = false;
                        else if (i < len - 1 && delta[i + 1] != 1 && (i > 0 && delta[i - 1] == 1 || i == 0))
                            add = false;
                    if (add)
                    {
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");

                        qTemplate.Append(converter.TransformToText(commonQuestionString[i].ToString()));
                        needAdd = (commonQuestionString[i] != '\r' && commonQuestionString[i] != '\n') ? (delta[i] > 1 ? 1 : 2) : 0;
                    }
                }
            }

            Debug.Assert(!qTemplate.ToString().Contains("<QText>\n<Some>0") && !qTemplate.ToString().Contains("<QText>\n<Some>'."));

            match = StringManager.BestCommonMatch(commonAnswerString, answers[0].Value, StringManager.TemplateSearchingNorm, true);

            commonAnswerString = ClearMatch(commonAnswerString, match, out res);

            len = commonAnswerString.Length;
            delta = new int[len + 1];

            delta[0] = res[0] + 1;
            for (int i = 1; i < len; i++)
                delta[i] = res[i] - res[i - 1];

            delta[len] = answers[0].Value.Length - (len > 0 ? res[len - 1] : 0);

            dmax = -1; dmax2 = -2; posmax = 0; posmax2 = 0;

            for (int i = 0; i < len + 1; i++)
                if (delta[i] > dmax)
                {
                    dmax2 = dmax;
                    posmax2 = posmax;
                    dmax = delta[i];
                    posmax = i;
                }
                else if (delta[i] > dmax2)
                {
                    dmax2 = delta[i];
                    posmax2 = i;
                }

            needAdd = 1;
            for (int i = 0; i < len + 1; i++)
            {
                if (delta[i] != 1)
                {
                    bool found = false;
                    if (!numberAdded)
                    {
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");

                        numberAdded = true;
                        found = true;
                        aTemplate.Append("<Number>");
                        aTemplate.Append("<Some2>");
                    }
                    if (i == posmax)
                    {
                        found = true;
                        aTemplate.Append("<Answer>");
                    }
                    if (!found)
                        aTemplate.Append("<Some>");
                    needAdd = 0;
                }

                if (i < len)
                {
                    bool add = true;
                    if (delta[i] == 1 && char.IsWhiteSpace(commonAnswerString[i]) && commonAnswerString[i] != '\r' && commonAnswerString[i] != '\n')
                        if (i > 0 && (char.IsWhiteSpace(commonAnswerString[i - 1]) && commonAnswerString[i - 1] != '\r' && commonAnswerString[i - 1] != '\n' || commonAnswerString[i - 1] == '\n') || i < len - 1 && commonAnswerString[i + 1] == '\r')
                            add = false;
                        else if (i > 0 && delta[i - 1] != 1 && (i < len - 1 && delta[i + 1] == 1 || i == len - 1))
                            add = false;
                        else if (i < len - 1 && delta[i + 1] != 1 && (i > 0 && delta[i - 1] == 1 || i == 0))
                            add = false;
                    if (add)
                    {
                        if (needAdd == 1)
                            qTemplate.Append("<Some2>");
                        else if (needAdd == 2)
                            qTemplate.Append("<Some3>");
                        aTemplate.Append(converter.TransformToText(commonAnswerString[i].ToString()));
                        needAdd = (commonQuestionString[i] != '\r' && commonQuestionString[i] != '\n') ? (delta[i] > 1 ? 1 : 2) : 0;
                    }
                }
            }
        }

        var template = new SITemplate()
        {
            StandartLogic = useStandardLogic,
            Multiplier = multiplier,
            QuestionTemplate = new List<string>(new string[] { qTemplate.ToString().Replace(Environment.NewLine, LineTemplate).Replace("\r", LineTemplate).Replace("\n", LineTemplate) }),
            AnswerTemplate = new List<string>(new string[] { aTemplate.ToString().Replace(Environment.NewLine, LineTemplate).Replace("\r", LineTemplate).Replace("\n", LineTemplate) })
        };

        return template;
    }

    private enum TemplateState { Start, Number, Text };

    private static SITemplate CreateQuestionTemplateNew(bool useStandardLogic, List<SIPart> questionsList)
    {
        var commonSubstring = StringAnalyzer.LongestCommonSubstring(questionsList.Select(p => p.Value).ToArray());

        var qTemplate = new StringBuilder();
        var multiplier = 1;

        if (commonSubstring != null)
        {
            var positions = commonSubstring.Value.PositionsHistory;
            var substring = commonSubstring.Value.Substring;

            var (averageDistances, questionIndex, answerIndex, bestDistanceIndices) = CalculateDistancesAndIndicies(positions);
            multiplier = BuildQuestionTemplate(qTemplate, multiplier, substring, averageDistances, questionIndex, answerIndex, bestDistanceIndices);
        }

        var template = new SITemplate()
        {
            StandartLogic = useStandardLogic,
            Multiplier = multiplier,
            QuestionTemplate = new List<string>(
                new string[] {
                    qTemplate.ToString().Replace(Environment.NewLine, LineTemplate).Replace("\r", LineTemplate).Replace("\n", LineTemplate)
                }),
            AnswerTemplate = new List<string>(new string[1] { "" })
        };

        return template;
    }

    private static int BuildQuestionTemplate(
        StringBuilder qTemplate,
        int multiplier,
        string substring,
        double[] averageDistances,
        int questionIndex,
        int answerIndex,
        int[] bestDistanceIndices)
    {
        // TODO: replace Question Template with separate templates between Number, QText, Answer and "after answer"

        var state = TemplateState.Start;

        var convertText = new StringBuilder();

        convertText.AppendLine("[X=](' |'\t|'.|':|'=|',|'||'&|'@|'^|'$|'!|'?|'*|'+|'-|'(|')|'[|']|'<|'>|'{|'}|'\"|'') => ''[X]");
        var converter = TextTreeTransformer.Create(convertText.ToString()); // Allows to escape SPARD symbols
        converter.Mode = TransformMode.Modification;

        var someItemsCounter = 0;
        const int SomeItemsMaxCount = 2;

        for (var i = 0; i < substring.Length; i++)
        {
            if (state == TemplateState.Start)
            {
                if (substring[i] == '\r' || substring[i] == '\n')
                {
                    qTemplate.Append(substring[i]);
                    continue;
                }

                qTemplate.Append("<Number>"); // Question price
                state = TemplateState.Number;
            }

            if (state == TemplateState.Number)
            {
                if (substring[i] == '0')
                {
                    qTemplate.Append(substring[i]);
                    multiplier *= 10;
                    continue;
                }

                state = TemplateState.Text;
            }

            if (i == questionIndex)
            {
                qTemplate.Append("<QText>");
            }
            else if (i == answerIndex)
            {
                qTemplate.Append("<Answer>");
            }
            else if (i < substring.Length - 1 && averageDistances[i] > 5.0 && someItemsCounter < SomeItemsMaxCount && bestDistanceIndices.Contains(i))
            {
                qTemplate.Append("<Some>");
                someItemsCounter++;
            }

            qTemplate.Append(converter.TransformToText(substring[i].ToString()));
        }

        return multiplier;
    }

    private static (double[] averageDistances, int questionIndex, int answerIndex, int[] bestDistanceIndices) CalculateDistancesAndIndicies(int[][] positions)
    {
        var bestDistance = new double[4];
        var bestDistanceIndices = new int[4];

        var averageDistances = new double[positions.Length - 1];
        for (int i = 0; i < positions.Length - 1; i++)
        {
            var distanceSum = 0;

            for (int j = 0; j < positions[i].Length; j++)
            {
                distanceSum += positions[i + 1][j] - positions[i][j];
            }

            var averageDistance = (double)distanceSum / positions[i].Length;
            averageDistances[i] = averageDistance;

            for (int k = 0; k < bestDistance.Length; k++)
            {
                if (averageDistance > bestDistance[k])
                {
                    for (int l = bestDistance.Length - 1; l > k; l--)
                    {
                        bestDistance[l] = bestDistance[l - 1];
                        bestDistanceIndices[l] = bestDistanceIndices[l - 1];
                    }

                    bestDistance[k] = averageDistance;
                    bestDistanceIndices[k] = i;
                    break;
                }
            }
        }

        var questionIndex = Math.Min(bestDistanceIndices[0], bestDistanceIndices[1]);
        var answerIndex = Math.Max(bestDistanceIndices[0], bestDistanceIndices[1]); // The answer always follows the question

        return (averageDistances, questionIndex, answerIndex, bestDistanceIndices.Skip(2).Take(2).ToArray());
    }

    private static void BuildQuestionListInNonStandardLogic(SIPart[][] list, List<SIPart> questsList, List<SIPart> answersList, List<int> numList, List<int> numListAns)
    {
        var numberOfQuests = 0; // Потенциально возможное количество вопросов
        var j = 1;

        while (j < list.Length && numberOfQuests < 5)
        {
            numberOfQuests += list[j].Length;
            j += 2;
        }

        if (numberOfQuests > 5)
        {
            numberOfQuests = 5;
        }

        var numberOfThemes = Math.Min((list.Length - 1) / 2, numberOfQuests);
        var questsPerTheme = (int)Math.Ceiling((double)numberOfQuests / numberOfThemes); // Количество вопросов, приходящееся на одну тему
        j = 0;

        for (var i = 1; i < numberOfThemes + 1; i++)
        {
            var start = Math.Min(list[i * 2 - 1].Length - questsPerTheme, i - 1);

            for (var k = 0; k < questsPerTheme; k++)
            {
                questsList.Add(list[i * 2 - 1][start + k]);
                answersList.Add(list[i * 2][start + k]);
                numList.Add(j);
                numListAns.Add(j++);
            }
        }
    }

    private static void BuildQuestionListInStandardLogic(SIPart[][] list, List<SIPart> questsList, List<int> numList)
    {
        // Count of question to use in template building
        var usedQuestionCount = Math.Min(MaximumUsedQuestionsCount, list.Sum(part => part.Length));
        var usedQuestionCounter = 0;

        var usedThemesCount = Math.Min(list.Length - 1, usedQuestionCount);
        var questionsPerThemeRatio = (int)Math.Ceiling((double)usedQuestionCount / usedThemesCount); // Количество вопросов, приходящееся на одну тему

        for (var i = 1; i < usedThemesCount + 1; i++)
        {
            int start = Math.Min(list[i].Length - 1 - questionsPerThemeRatio, i - 1);

            for (var k = 0; k < questionsPerThemeRatio; k++)
            {
                var index = start + k;

                if (index == list[i].Length - 1)
                {
                    // Last question in theme could contain next theme part and this would make analysis harder
                    break;
                }
                else
                {
                    var newQuest = list[i][index];
                    questsList.Add(newQuest);
                }

                numList.Add(usedQuestionCounter++);

                if (usedQuestionCounter == usedQuestionCount)
                {
                    break;
                }
            }

            if (usedQuestionCounter == usedQuestionCount)
            {
                break;
            }
        }
    }

    private static string ClearMatch(string s, Point[] match, out int[] res)
    {
        var resList = new List<int>();
        var stringList = new StringBuilder();

        for (int i = 0; i < match.Length; i++)
        {
            var c = s[i];

            if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) || c == '0' || c == '\r' || c == '\n' || c == '.' || c == '\t'
                || (i > 1 && match[i - 1].Y == match[i].Y - 1 &&
                match[i - 2].Y == match[i].Y - 2
                || i < match.Length - 2 && match[i + 1].Y == match[i].Y + 1 &&
                match[i + 2].Y == match[i].Y + 2
                || i > 0 && match[i - 1].Y == match[i].Y - 1 &&
                i < match.Length - 1 && match[i + 1].Y == match[i].Y + 1))
            {
                resList.Add(match[i].Y);
                stringList.Append(c);
            }
        }

        s = stringList.ToString();
        res = resList.ToArray();
        return s;
    }

    /// <summary>
    /// Создать шаблон темы
    /// </summary>
    /// <param name="list">Список тем с разбивкой по вопросам входного файла</param>
    /// <param name="template">Шаблон вопроса (и ответа)</param>
    /// <param name="standartLogic">Применять ли стандартную логику распознавания</param>
    /// <returns>Строка, содержащая сформированный шаблон темы</returns>
    private static void CreateThemeTemplate(SIPart[][] list, SITemplate template)
    {
        var themesList = new List<SIPart>();
        var themeNumsList = new List<int>();
        var separatorsList = new List<SIPart>();

        if (template.StandartLogic)
        {
            for (int i = 1; i < Math.Min(6, list.Length - 1); i++)
            {
                themesList.Add(list[i][list[i].Length - 1]);                    
            }
        }
        else
        {
            for (int i = 1; i < Math.Min(11, list.Length - 1); i++)
            {
                if (i % 2 == 1)
                {
                    themesList.Add(list[i][list[i].Length - 1]);
                }
                else
                {
                    separatorsList.Add(list[i][list[i].Length - 1]);
                }
            }
        }

        if (themesList.Count == 0)
        {
            template.ThemeTemplate = new List<string>(new string[] { "[m]<TName>" });
            template.SeparatorTemplate = new List<string>(new string[] { string.Empty });
            return;
        }

        var themes = themesList.ToArray();
        var separators = separatorsList.ToArray();

        string separator = string.Empty;

        if (!template.StandartLogic)
        {
            var readTemplateBuilder = new StringBuilder(template.QuestionTemplate[0]);

            readTemplateBuilder
                .AppendLine("<Line>[m]<Separator> => [matches, Separator]")
                .AppendLine("<Number> := <Digit>+")
                .AppendLine("<QText> := [lazy]<Text>")
                .AppendLine("<Answer> := .[lazy]<Text>")
                .AppendLine("<Some> := [lazy]<Text>")
                .AppendLine("<Some2> := [lazy]<Text>")
                .AppendLine("<Some3> := ")
                .AppendLine("<Separator> := .<Text>")
                .AppendLine(string.Format("{0} := {1}", LineTemplate, LineDefinition));

            var reader = TextMatchTreeTransformer.Create(readTemplateBuilder.ToString());
            reader.Mode = TransformMode.Function;
            var result = new List<string>();

            foreach (var item in separators)
            {
                var match = reader.Transform(item.Value).First();

                if (match != null)
                {
                    result.Add(match.Value.ToString());
                }
            }

            var separatorsNumsList = new List<int>();
            var oper = new CombinedStringsOperator();

            for (int i = 0; i < result.Count; i++)
            {
                oper.Add(new CombinedString(result[i], i));
                separatorsNumsList.Add(i);
            }

            Debug.Assert(separatorsNumsList.Count > 0);
            separator = oper.CreateCombination(Shuffle(separatorsNumsList)).ToString();
        }

        var themeTemplateBuilder = new StringBuilder("<Body> := ")
            .AppendLine(template.StandartLogic ? template.QuestionTemplate[0] : template.AnswerTemplate[0])
            .AppendLine("<TName> := .[lazy]<Text>")
            .AppendLine("<TAuthor> := .[lazy]<Text>")
            .AppendLine("<Number> := <Digit>+")
            .AppendLine("<QText> := [lazy]<Text>")
            .AppendLine("<Answer> := .[lazy]<Text>")
            .AppendLine("<Some> := [lazy]<Text>")
            .AppendLine("<Some2> := [lazy]<Text>")
            .AppendLine(string.Format("{0} := {1}", LineTemplate, LineDefinition))
            .Append("[m]<Body><BR><TName><BR>(<TAuthor><BR>)?|[m]<Body><TName>(<BR><SP>*|<BR><TAuthor>|)<BR>? => [matches, Body]");

        var parser = TextMatchTreeTransformer.Create(themeTemplateBuilder.ToString().Replace("<Some3>", ""));
        parser.Mode = TransformMode.Function;
        parser.Runtime.SearchBestVariant = true;
        var resultList = new List<SIPart>();

        int n = 0;

        for (int i = 0; i < themes.Length; i++)
        {
            var matchEnumerator = parser.Transform(themes[i].Value).GetEnumerator();

            if (matchEnumerator.MoveNext())
            {
                var match = matchEnumerator.Current;
                var body = match.Value.ToString();

                if (body.Length > 0)
                {
                    var themeName = themes[i].Value[body.Length..];

                    if (themeName.Length < 150) // If theme name is so long that means we are having an error
                    {
                        resultList.Add(new SIPart { Value = themeName.Replace("\r\n", "\n") });
                        themeNumsList.Add(n++);
                    }
                }
            }
        }

        if (resultList.Count < 2)
        {
            template.ThemeTemplate = new List<string>(new string[]
            {
                "[m]<TName>"
            });

            template.SeparatorTemplate = new List<string>(new string[] { string.Empty });
            return;
        }

        themes = resultList.ToArray();

        var oper2 = new CombinedStringsOperator();

        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i].Value.Length > MaximumQuestionLength)
            {
                themes[i].Value = themes[i].Value.Substring(themes[i].Value.Length - MaximumQuestionLength);
            }

            oper2.Add(new CombinedString(themes[i].Value, i));
        }

        var s = oper2.CreateCombination(Shuffle(themeNumsList)).ToString();

        int use = new Random().Next(themes.Length);

        var convertText = new StringBuilder();
        convertText.AppendLine("[X=](' |'.|':|'=|',|'||'&|'@|'^|'$|'!|'?|'*|'+|'-|'(|')|'[|']|'<|'>|'{|'}|'\"|'') => ''[X]");
        var converter = TextTreeTransformer.Create(convertText.ToString());
        converter.Mode = TransformMode.Modification;

        var len = s.Length;
        var theme = new StringBuilder();

        if (len > 0)
        {
            var match = StringManager.BestCommonMatch(s, themes[use].Value, StringManager.TemplateSearchingNorm, true);

            int needAdd = 1;
            s = ClearMatch(s, match, out int[] res);

            len = s.Length;

            if (len > 0)
            {
                var delta = new int[len + 1];
                delta[0] = res[0];

                for (int i = 1; i < len; i++)
                {
                    delta[i] = res[i] - res[i - 1];
                }

                delta[len] = themes[use].Value.Length - res[len - 1];
                int dmax = -1, dmax2 = -2, posmax = 0, posmax2 = 0;

                for (int i = 0; i < len + 1; i++)
                {
                    if (delta[i] > dmax)
                    {
                        dmax2 = dmax;
                        posmax2 = posmax;
                        dmax = delta[i];
                        posmax = i;
                    }
                    else if (delta[i] > dmax2)
                    {
                        dmax2 = delta[i];
                        posmax2 = i;
                    }
                }

                if (delta[posmax2] < 6)
                {
                    posmax2 = len;
                }

                bool nameAdded = false;

                for (int i = 0; i <= len; i++)
                {
                    if (delta[i] != 1)
                    {
                        if (i == Math.Min(posmax, posmax2))
                        {
                            theme.Append("[m]<TName>");
                            nameAdded = true;
                        }
                        else if (i == Math.Max(posmax, posmax2))
                        {
                            if (delta[i] > 5)
                            {
                                theme.Append("[m]<TAuthor>");
                            }
                        }
                        else
                        {
                            theme.Append("<Some>");
                        }

                        needAdd = 0;
                    }

                    bool add = true;

                    if (i < len && char.IsWhiteSpace(s[i]) && s[i] != '\r' && s[i] != '\n')
                    {
                        if (i > 0 && (char.IsWhiteSpace(s[i - 1]) && s[i - 1] != '\r' && s[i - 1] != '\n' || s[i - 1] == '\n') || i < len - 1 && s[i + 1] == '\r')
                        {
                            add = false;
                        }
                        else if (i > 0 && delta[i - 1] != 1 && (i < len && delta[i + 1] == 1 || i == len))
                        {
                            add = false;
                        }
                        else if (i < len && delta[i + 1] != 1 && (i > 0 && delta[i - 1] == 1 || i == 0))
                        {
                            add = false;
                        }
                    }

                    if (add && i < len)
                    {
                        if (needAdd == 1)
                        {
                            theme.Append("<Some2>");
                        }
                        else if (needAdd == 2)
                        {
                            theme.Append("<Some3>");
                        }

                        theme.Append(converter.TransformToText(s[i].ToString()));
                        needAdd = (s[i] != '\r' && s[i] != '\n') ? (delta[i] > 1 ? 1 : 2) : 0;
                    }
                }

                if (!nameAdded)
                {
                    theme.Append("[m]<TName>");
                }
            }
            else
            {
                theme.Append("[m]<TName>");
            }
        }
        else
        {
            theme.Append("[m]<TName>");
        }

        template.ThemeTemplate = new List<string>(new string[] { theme.ToString().Replace(Environment.NewLine, LineTemplate).Replace("\r", LineTemplate).Replace("\n", LineTemplate) });
        template.SeparatorTemplate = new List<string>(new string[] { converter.TransformToText(separator).Replace(Environment.NewLine, LineTemplate).Replace("\r", LineTemplate).Replace("\n", LineTemplate) });
    }

    /// <summary>
    /// Prepares templates for transformation.
    /// </summary>
    /// <param name="qTemplate">All used templates.</param>
    private static void PrepareTemplates(SITemplate qTemplate)
    {
        var temp = qTemplate.StandartLogic ? qTemplate.QuestionTemplate[0] : qTemplate.AnswerTemplate[0];
        int S = temp.Length - LineTemplate.Length;

        while (S > 0 && temp.AsSpan(S, LineTemplate.Length) == LineTemplate)
        {
            S -= LineTemplate.Length;
        }

        var newTemp = new StringBuilder(temp[..(S + LineTemplate.Length)])
            .Append("([ignoresp]Комментарий':' [m]<QComment>)?")
            .Append("([ignoresp]Источник':' [m]<QSource>)?")
            .Append("([ignoresp]Автор' вопроса':' [m]<QAuthor>)?")
            .Append(temp.AsSpan(S + LineTemplate.Length));

        if (qTemplate.StandartLogic)
            qTemplate.QuestionTemplate[0] = AddIgnore(
                newTemp.ToString()
                .Replace("<Number><Some2>", "[m]<Number>")
                .Replace("<Number>", "[m]<Number>")
                .Replace("<QText>", "[m]<QText>")
                .Replace("<Answer>", "[m]<Answer>"));
        else
        {
            qTemplate.QuestionTemplate[0] = AddIgnore(
                qTemplate.QuestionTemplate[0]
                .Replace("<Number><Some2>", "[m]<Number>")
                .Replace("<Number>", "[m]<Number>")
                .Replace("<QText>", "[m]<QText>"));

            qTemplate.AnswerTemplate[0] = AddIgnore(
                newTemp.ToString()
                .Replace("<Number><Some2>", "[m]<Number>")
                .Replace("<Number>", "[m]<Number>")
                .Replace("<Answer>", "[m]<Answer>"));
        }

        var tTemp = qTemplate.ThemeTemplate[0];
        int S1 = tTemp.Length - LineTemplate.Length;

        while (S1 > 0 && tTemp.AsSpan(S1, LineTemplate.Length) == LineTemplate)
        {
            S1 -= LineTemplate.Length;
        }

        var newTTemp = new StringBuilder(tTemp[..(S1 + LineTemplate.Length)]);
        newTTemp.Append("('([m]<TComment>'))?");
        newTTemp.Append(tTemp.AsSpan(S1 + LineTemplate.Length));
        qTemplate.ThemeTemplate[0] = AddIgnore(newTTemp.ToString());

        if (!qTemplate.StandartLogic)
        {
            qTemplate.SeparatorTemplate[0] = AddIgnore(qTemplate.SeparatorTemplate[0]);
        }

        qTemplate.RoundTemplate = new List<string>(new string[] { "<Line><Line>[m]<RName>" });
        qTemplate.PackageTemplate = new List<string>(new string[] { "[m]<PName>" });
    }

    private static string AddIgnore(string template)
    {
        Debug.WriteLine(string.Format("Before: {0}", template));

        template = template.Replace("<Some3>", "<Some2>");
        int j = -1;
        int i;

        while ((i = template.IndexOf("<Some2>")) != -1)
        {
            if (i == 0)
            {
                template = template[7..];
                continue;
            }
            else if (i > 0 && i - 4 != j && (i - 5 != j || i < 2 || template[i - 2] != '\''))
            {
                if (i >= 2 && (template[i - 2] == '\'' || template[i - 2] == '"'))
                {
                    template = string.Format("{0}[ignoresp]{1}{2}{3}", template[..(i - 2)], template[i - 2], template[i - 1], template[(i + 7)..]);
                }
                else
                {
                    template = string.Format("{0}[ignoresp]{1}{2}", template[..(i - 1)], template[i - 1], template[(i + 7)..]);
                }

                j = i + 7;
            }
            else
            {
                template = string.Format("{0}{1}{2}", template[..(i - 1)], template[i - 1], template[(i + 7)..]);

                if (i - 5 == j)
                {
                    j++;
                }

                j++;
            }
        }

        Debug.WriteLine(string.Format("After: {0}", template));
        return template;
    }

    /// <summary>
    /// Автоматически создать шаблоны пакета, раунда, темы, вопроса
    /// </summary>
    /// <param name="list">Список тем с разбивкой по вопросам входного файла</param>
    /// <param name="standardLogic">Применять ли стандартную логику распознавания</param>
    /// <returns>Сформированные шаблоны</returns>
    public SITemplate GetGeneratedTemplates(SIPart[][] list, bool standardLogic)
    {
        var siTemplate = CreateQuestionTemplate(list, standardLogic);
        Progress?.Invoke(40);
        CreateThemeTemplate(list, siTemplate);
        Progress?.Invoke(80);
        PrepareTemplates(siTemplate);
        Progress?.Invoke(100);
        return siTemplate;
    }

    /// <summary>
    /// Получить шаблоны СНС
    /// </summary>
    /// <param name="list">Список тем с разбивкой по вопросам входного файла</param>
    /// <param name="standartLogic">Применять ли стандартную логику распознавания</param>
    /// <returns>Сформированные шаблоны</returns>
    public static SITemplate GetSnsTemplates(SIPart[][] list, bool standartLogic)
    {
        return new SITemplate()
        {
            StandartLogic = true,
            Multiplier = 10,
            PackageTemplate = new List<string>(new string[] { "[m]<PName>" }),
            RoundTemplate = new List<string>(new string[] { "<Line><Line>[m]<RName>" }),
            ThemeTemplate = new List<string>(new string[] { "<Line><Line>[ignoresp]Тема':[m]<TName>(<Line>[ignoresp]Автор':[m]<TAuthor>)?" }),
            QuestionTemplate = new List<string>(new string[] { "<Line>[m]<Number>[ignoresp]0'.<Line>[m]<QText><Line><Line>[ignoresp]Ответ':[m]<Answer>(<Line>[ignoresp]Комментарий':[m]<QComment>)?(<Line>[ignoresp]Источник':[m]<QSource>)?" }),
            SeparatorTemplate = new List<string>(new string[] { string.Empty }),
            AnswerTemplate = new List<string>(new string[] { string.Empty })
        };
    }

    private static int CountLines(string template)
    {
        int pos = 0, count = 0;
        while (pos + LineTemplate.Length < template.Length && template.Substring(pos, LineTemplate.Length) == LineTemplate)
        {
            pos += LineTemplate.Length;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Прочитать темы и вопросы и создать итоговый SIDocument
    /// </summary>
    /// <param name="list"></param>
    /// <param name="templates"></param>
    /// <param name="doc"></param>
    /// <param name="addToExisting"></param>
    public bool ReadFile(SIPart[][] list, SITemplate templates, ref SIDocument document, bool addToExisting, string docName, string authorName, string emptyRoundName, out int themesNum)
    {
        // Title reading
        var titleTemplate = new StringBuilder("<Body> := (<P>?<R>)?<T><SP>*$")
            .AppendLine()
            .AppendLine("<P> := <P0>")
            .Append("<P0> := [log, p, 0]")
            .AppendLine(templates.PackageTemplate[0])
            .AppendLine("<R> := <R0>")
            .Append("<R0> := [log, r, 0]")
            .AppendLine(templates.RoundTemplate[0])
            .AppendLine("<T> := <T0>")
            .Append("<T0> := [log, t, 0]")
            .AppendLine(templates.ThemeTemplate[0])
            .AppendLine("<PName> := .<Text>")
            .AppendLine("<RName> := .<Text>")
            .AppendLine("<TName> := .[lazy]<Text>")
            .AppendLine("<TAuthor> := .[lazy]<String>")
            .AppendLine("<TComment> := .[lazy]<Text>")
            .AppendLine("<Some> := [lazy]<String>")
            .AppendLine(string.Format("{0} := {1}", LineTemplate, LineDefinition))
            .Append("<Body> => @(linearize, [matches, Body])");

        int progress = 0;

        var titleParser = TextMatchTreeTransformer.Create(titleTemplate.ToString());
        titleParser.Mode = TransformMode.Function;
        titleParser.Runtime.SearchBestVariant = true;

        int roundIndex = -1, themeIndex = -1, themesCounter = 0, questIndex = -1;

        themesNum = 0;

        if (!addToExisting)
        {
            document = SIDocument.Create(docName, authorName);
        }
        else
        {
            roundIndex = document.Package.Rounds.Count - 1;
        }

        string roundName = "";
        string themeName = "";
        string themeAuthor = "";
        string themeComment = "";
        string questNumber = "";
        string questText = "";
        string questAnswer = "";
        string questComment = "";
        string questSource = "";
        string questAuthor = "";

        StringBuilder? templateBuilder = null, variants = null;

        var packageFollow = new Follow() { TemplatesCollection = templates.PackageTemplate, SetLetter = 'P', Letter = 'p' };
        var roundFollow = new Follow() { TemplatesCollection = templates.RoundTemplate, SetLetter = 'R', Letter = 'r' };
        var themeFollow = new Follow() { TemplatesCollection = templates.ThemeTemplate, SetLetter = 'T', Letter = 't' };
        var questFollow = new Follow() { TemplatesCollection = templates.QuestionTemplate, SetLetter = 'Q', Letter = 'q' };
        var answerFollow = new Follow() { TemplatesCollection = templates.AnswerTemplate, SetLetter = 'A', Letter = 'a' };
        var separatorFollow = new Follow() { TemplatesCollection = templates.SeparatorTemplate, SetLetter = 'S', Letter = 's' };

        // Вставим дополнительные переносы в начале для корректной работы шаблона
        int numLines = Math.Max(CountLines(templates.RoundTemplate[0]), CountLines(templates.ThemeTemplate[0]));
        var newTitle = new StringBuilder();

        for (int i = 0; i < numLines; i++)
        {
            newTitle.AppendLine();
        }

        var matches = Analyze(titleParser, list, 0, 0, newTitle.ToString(), new Follow[] { roundFollow, themeFollow, packageFollow }, out Decision decision);

        switch (decision)
        {
            case Decision.Go:
                Match<char> match;
                if (matches.TryGetValue("PName", out match))
                    document.Package.Info.Comments.Text += match.Value.ToString().Trim();

                if (matches.TryGetValue("RName", out match))
                    roundName = match.Value.ToString().Trim();

                if (matches.TryGetValue("TName", out match))
                    themeName = match.Value.ToString().Trim();

                if (matches.TryGetValue("TAuthor", out match))
                    themeAuthor = match.Value.ToString().Trim();

                if (matches.TryGetValue("TCommment", out match))
                    themeComment = match.Value.ToString().Trim();
                break;

            case Decision.Cancel:
                return false;

            case Decision.Skip:
                // Добавим в комментариии к пакету
                document.Package.Info.Comments.Text += list[0][0].Value;
                break;

            default:
                break;
        }
        
        progress += list[0][0].Value.Length;
        int themeProgress = progress;
        Progress?.Invoke(progress);

        // Theme reading
        templateBuilder = new StringBuilder("<Body> := <Q><SP>*$")
            .AppendLine()
            .AppendLine("<Q> := <Q0>")
            .Append("<Q0> := [log, q, 0]")
            .AppendLine(templates.QuestionTemplate[0]);

        variants = new StringBuilder();

        for (int i = 0; i < templates.ThemeTemplate.Count; i++)
        {            
            templateBuilder.AppendFormat("<T{0}> := [log, t, {0}]", i).AppendLine(templates.ThemeTemplate[i]);

            if (variants.Length > 0)
            {
                variants.Append('|');
            }

            variants.AppendFormat("<T{0}>", i);
        }

        templateBuilder.AppendFormat("<T> := {0}", variants).AppendLine();
        
        variants = new StringBuilder();

        for (int i = 0; i < templates.RoundTemplate.Count; i++)
        {            
            templateBuilder.AppendFormat("<R{0}> := [log, r, {0}]", i)
            .AppendLine(templates.RoundTemplate[i]);

            if (variants.Length > 0)
            {
                variants.Append('|');
            }

            variants.AppendFormat("<R{0}>", i);
        }

        templateBuilder.AppendFormat("<R> := {0}", variants).AppendLine();

        if (!templates.StandartLogic)
        {
            templateBuilder.AppendLine("<A> := <A0>")
                .Append("<A0> := [log, a, 0]")
                .AppendLine(templates.AnswerTemplate[0]);

            templateBuilder.AppendLine("<S> := <S0>")
                .Append("<S0> := [log, s, 0]")
                .AppendLine(templates.SeparatorTemplate[0]);
        }

        templateBuilder.AppendLine("<RName> := .<Text>")
            .AppendLine("<TName> := .[lazy]<Text>")
            .AppendLine("<TAuthor> := .[lazy]<String>")
            .AppendLine("<TComment> := .[lazy]<String>")
            .AppendLine("<Number> := <Digit>+")
            .AppendLine("<QText> := [lazy]<Text>")
            .AppendLine("<Answer> := [lazy].+")
            .AppendLine("<QComment> := .[lazy]<Text>")
            .AppendLine("<QSource> := .[lazy]<Text>")
            .AppendLine("<QAuthor> := .[lazy]<Text>")
            .AppendLine("<Some> := [lazy]<String>")
            .AppendLine(string.Format("{0} := {1}", LineTemplate, LineDefinition))
            .Append("<Body> => @(linearize, [matches, Body])");

        var parser = TextMatchTreeTransformer.Create(templateBuilder.ToString());
        parser.Mode = TransformMode.Function;
        parser.Runtime.SearchBestVariant = true;

        templateBuilder = new StringBuilder("<Body> := <Q><SP>*$");
        Definition templateWithoutTheme, templateWithTheme;
        using (var reader = new StringReader(templateBuilder.ToString()))
        {
            templateWithoutTheme = (Definition)ExpressionBuilder.Parse(reader);
        }

        templateBuilder = new StringBuilder(
            templates.StandartLogic ?
            "<Body> := <Q>(<R>)?<T><SP>*$"
            : "<Body> := <Q><S><SP>*$");

        using (var reader = new StringReader(templateBuilder.ToString()))
        {
            templateWithTheme = (Definition)ExpressionBuilder.Parse(reader);
        }

        Definition nsAnswerWithoutTheme = null, nsAnswerWithTheme = null;

        if (!templates.StandartLogic)
        {
            templateBuilder = new StringBuilder("<Body> := <A><SP>*$");
            using (var reader = new StringReader(templateBuilder.ToString()))
            {
                nsAnswerWithoutTheme = (Definition)ExpressionBuilder.Parse(reader);
            }

            templateBuilder = new StringBuilder("<Body> := <A>(<R>)?<T><SP>*$");
            using (var reader = new StringReader(templateBuilder.ToString()))
            {
                nsAnswerWithTheme = (Definition)ExpressionBuilder.Parse(reader);
            }
        }

        for (int i = 1; i < list.Length && (templates.StandartLogic || i + 1 < list.Length); i += templates.StandartLogic ? 1 : 2)
        {
            if (roundName.Length > 0 || roundIndex == -1)
            {
                var roundComments = ExtractComments(ref roundName);
                var round = document.Package.CreateRound(RoundTypes.Standart, (roundName.Length > 0 ? roundName : emptyRoundName).ClearPoints().GrowFirstLetter());
                if (roundComments != null)
                    round.Info.Comments.Text = roundComments;
                roundIndex++;
                themesCounter += themeIndex + 1;
                themeIndex = -1;
                roundName = string.Empty;
            }

            var themeComments = ExtractComments(ref themeName);
            var theme = document.Package.Rounds[roundIndex].CreateTheme(themeName.ClearPoints().GrowFirstLetter());
            themeIndex++;
            questIndex = -1;

            if (themeComments != null)
            {
                if (themeComment.Length > 0)
                    themeComment = themeComments + Environment.NewLine + themeComment;
                else
                    themeComment = themeComments;
            }

            if (themeAuthor.Length > 0)
            {
                theme.Info.Authors.Add(themeAuthor.ClearPoints().GrowFirstLetter());
                themeAuthor = string.Empty;
            }

            if (themeComment.Length > 0)
            {
                theme.Info.Comments.Text = themeComment.ClearPoints().GrowFirstLetter();
                themeComment = string.Empty;
            }

            for (int j = 0; j < list[i].Length && (templates.StandartLogic || j < list[i + 1].Length); j++)
            {
                Follow[] use;
                if (i > 1 && j == 0)
                {
                    parser.ReplaceSetDefinition(templateWithoutTheme);
                    use = new Follow[] { questFollow };
                }
                else if (j == list[i].Length - 1 && (!templates.StandartLogic || i + 1 < list.Length)) // Добавим шаблон с темой                
                {
                    parser.ReplaceSetDefinition(templateWithTheme);
                    use = templates.StandartLogic ? new Follow[] { questFollow, themeFollow, roundFollow } : new Follow[] { questFollow, separatorFollow };
                }
                else
                    use = new Follow[] { questFollow };

                matches = Analyze(parser, list, i, j, null, use, out decision);

                switch (decision)
                {
                    case Decision.Go:
                        if (!templates.StandartLogic)
                        {
                            if (j == list[i + 1].Length - 1 && i + 2 < list.Length) // Добавим шаблон с темой
                            {
                                parser.ReplaceSetDefinition(nsAnswerWithTheme);
                                use = new Follow[] { answerFollow, themeFollow, roundFollow };
                            }
                            else
                            {
                                parser.ReplaceSetDefinition(nsAnswerWithoutTheme);
                                use = new Follow[] { answerFollow };
                            }
                            var matches2 = Analyze(parser, list, i + 1, j, null, use, out decision);

                            switch (decision)
                            {
                                case Decision.Go:
                                    foreach (var item in matches2)
                                    {
                                        matches[item.Key] = item.Value;
                                    }
                                    break;

                                case Decision.Cancel:
                                    return false;

                                case Decision.Skip:
                                    questAnswer = list[i + 1][j].Value;
                                    break;

                                default:
                                    break;
                            }

                            parser.ReplaceSetDefinition(templateWithoutTheme);
                        }

                        #region Fill Vars

                        Match<char> match;

                        if (matches.TryGetValue("Number", out match))
                            questNumber = (int.Parse(match.Value.ToString()) * templates.Multiplier).ToString();

                        if (matches.TryGetValue("QText", out match))
                            questText = match.Value.ToString().Trim();

                        if (matches.TryGetValue("Answer", out match))
                            questAnswer = match.Value.ToString().Trim();

                        if (matches.TryGetValue("QComment", out match))
                            questComment = match.Value.ToString().Trim();
                        else
                            questComment = string.Empty;

                        if (matches.TryGetValue("QSource", out match))
                            questSource = match.Value.ToString().Trim();
                        else
                            questSource = string.Empty;

                        if (matches.TryGetValue("QAuthor", out match))
                            questAuthor = match.Value.ToString().Trim();
                        else
                            questAuthor = string.Empty;

                        if (j == list[i].Length - 1)
                        {
                            if (matches.TryGetValue("RName", out match))
                                roundName = match.Value.ToString().Trim();

                            if (matches.TryGetValue("TName", out match))
                                themeName = match.Value.ToString().Trim();

                            if (matches.TryGetValue("TAuthor", out match))
                                themeAuthor = match.Value.ToString().Trim();

                            if (matches.TryGetValue("TComment", out match))
                                themeComment = match.Value.ToString().Trim();
                        }

                        #endregion

                        var quest = document.Package.Rounds[roundIndex].Themes[themeIndex].CreateQuestion(int.Parse(questNumber.ClearPoints().GrowFirstLetter()));
                        questIndex++;

                        // The whole package is upgraded after completion
                        quest.Scenario.Clear();
                        quest.Scenario.Add(questText.ClearPoints().GrowFirstLetter());
                        quest.Right.Clear();
                        quest.Right.Add(questAnswer.ClearPoints().GrowFirstLetter());

                        if (questSource.Length > 0)
                            quest.Info.Sources.Add(questSource.ClearPoints().GrowFirstLetter());
                        if (questComment.Length > 0)
                            quest.Info.Comments.Text = questComment.ClearPoints().GrowFirstLetter();
                        if (questAuthor.Length > 0)
                            quest.Info.Authors.Add(questAuthor);

                        break;

                    case Decision.Cancel:
                        return false;

                    case Decision.Skip:
                        if (document.Package.Rounds[roundIndex].Themes[themeIndex].Info.Comments.Text.Length > 0)
                            document.Package.Rounds[roundIndex].Themes[themeIndex].Info.Comments.Text += Environment.NewLine;
                        document.Package.Rounds[roundIndex].Themes[themeIndex].Info.Comments.Text += string.Format("?: {0}{1}", list[i][j].Value, (!templates.StandartLogic ? string.Format(" ({0})", list[i + 1][j].Value) : string.Empty));
                        break;

                    default:
                        break;
                }

                progress += list[i][j].Value.Length;
                Progress?.Invoke(progress);
            }

            themeProgress = progress;
            themesNum++;
        }
        return true;
    }

    private static string ExtractComments(ref string text)
    {
        string comments = null;
        if (text.Length > 0)
        {
            var parts = text.Split(new string[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                text = parts[0]; // Остальное - в комментарии
                var commentBuilder = new StringBuilder();
                for (int b = 1; b < parts.Length; b++)
                {
                    commentBuilder.Append(parts[b]);
                    if (b < parts.Length - 1)
                        commentBuilder.AppendLine();
                }
                comments = commentBuilder.ToString();
            }
        }
        return comments;
    }

    /// <summary>
    /// Принятое решение пользователя
    /// </summary>
    private enum Decision 
    {
        /// <summary>
        /// Продолжать
        /// </summary>
        Go,
        /// <summary>
        /// Прекратить
        /// </summary>
        Cancel,
        /// <summary>
        /// Пропустить фрагмент
        /// </summary>
        Skip
    };

    /// <summary>
    /// Класс с информацией о слежении за коллекцией шаблонов
    /// </summary>
    private sealed class Follow
    {
        internal List<string> TemplatesCollection { get; set; }
        internal char Letter { get; set; }
        internal char SetLetter { get; set; }
    }

    private Dictionary<string, Match<char>>? Analyze(
        TreeTransformer<char, Match<char>> parser,
        SIPart[][] list,
        int i,
        int j,
        string top,
        Follow[] follow,
        out Decision decision)
    {
        var input = (top ?? string.Empty) + list[i][j].Value;

        try
        {
            Match<char> matches = null;
            while ((matches = parser.Transform(input).FirstOrDefault()) == null || matches.Matches.Count == 0)
            {
                if (ReadError == null)
                {
                    decision = Decision.Cancel;
                    return null;
                }

                var args = new ReadErrorEventArgs { BestTry = parser.Runtime.BestTry, Index = Tuple.Create(i, j), Move = top == null ? 0 : top.Length };

                var stack = parser.Runtime.BestTry.StackTrace;

                int rootIndex = stack.Length - 1;
                for (int s = 2; s < stack.Length; s++)
                {
                    if (stack[s].Expression is Set set)
                    {
                        var name = set.Operands().First().ToString();
                        if (Regex.Match(name, @"[PRTQ]\d+").Success)
                        {
                            rootIndex = s;
                            break;
                        }
                    }
                }

                var topExpr = stack[rootIndex - 1].Expression;
                var bottomExpr = stack[rootIndex - 2].Expression;

                var operands = topExpr.Operands().ToArray();
                var result = new List<Expression>();
                var collect = false;
                for (int k = 0; k < operands.Length; k++)
                {
                    if (collect)
                        result.Add(operands[k]);
                    else if (operands[k] == bottomExpr)
                        collect = true;
                }

                args.NotReaded = new Sequence(result.ToArray());
                args.Missing = BuildMissing(bottomExpr);

                var count = follow.Select(item => item.TemplatesCollection.Count).ToArray();

                ReadError(null, args);

                if (args.Cancel)
                {
                    decision = Decision.Cancel;
                    return null;
                }

                if (args.Skip)
                {
                    decision = Decision.Skip;
                    return null;
                }

                for (int k = 0; k < follow.Length; k++)
                {
                    if (count[k] < follow[k].TemplatesCollection.Count)
                    {
                        UpdateTemplate(parser, follow[k]);
                    }
                }

                input = (top ?? string.Empty) + list[i][j].Value;
            }
            
            decision = Decision.Go;

            return matches.Matches;
        }
        catch (Exception exc)
        {
            throw new Exception(string.Format("Ошибка разбора.\r\n{0}\r\nТекст: {1}", string.Join(Environment.NewLine, parser.SetDefinitions.Select(def => def.ToString())), input), exc);
        }
    }

    private Expression BuildMissing(Expression expression)
    {
        var operands = expression.Operands().ToArray();

        if (operands.Length == 0 || expression is Set)
        {
            return expression;
        }
        else if (expression is Instruction)
        {
            if (operands.Length == 1)
                return new StringValue("");

            return BuildMissing(operands[1]);
        }

        return BuildMissing(operands[0]);
    }

    private static bool Contains(Expression item, Expression expression)
    {
        if (item == expression)
            return true;

        foreach (var expr in item.Operands())
        {
            if (Contains(expr, expression))
                return true;
        }

        return false;
    }

    private static void UpdateTemplate(TreeTransformer<char, Match<char>> parser, Follow follow)
    {
        var templateBuilder = new StringBuilder();
        templateBuilder.AppendFormat("<{1}{0}> := [log, {2}, {0}]", follow.TemplatesCollection.Count - 1, follow.SetLetter, follow.Letter).AppendLine(follow.TemplatesCollection[follow.TemplatesCollection.Count - 1]);

        Definition newTemp;
        using (var reader = new StringReader(templateBuilder.ToString()))
        {
            newTemp = (Definition)ExpressionBuilder.Parse(reader);
        }

        parser.AddSetDefinition(newTemp);

        templateBuilder = new StringBuilder();
        var variants = new StringBuilder();
        for (int k = 0; k < follow.TemplatesCollection.Count; k++)
        {
            if (variants.Length > 0)
                variants.Append('|');
            variants.AppendFormat("<{1}{0}>", k, follow.SetLetter);
        }

        templateBuilder.AppendFormat("<{1}> := {0}", variants, follow.SetLetter).AppendLine();

        using (var reader = new StringReader(templateBuilder.ToString()))
        {
            newTemp = (Definition)ExpressionBuilder.Parse(reader);
        }

        parser.ReplaceSetDefinition(newTemp);
    }
}
