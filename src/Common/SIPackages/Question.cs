using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Models;
using SIPackages.TypeConverters;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a game question.
/// </summary>
public sealed class Question : InfoOwner, IEquatable<Question>
{
    /// <summary>
    /// Question price that means empty question.
    /// </summary>
    public const int InvalidPrice = -1;

    private const char UniversalLineSeparatorChar = '\n';

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _price;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _typeName = QuestionTypes.Default;

    /// <summary>
    /// Question base price.
    /// </summary>
    [DefaultValue(0)]
    public int Price
    {
        get => _price;
        set { var oldValue = _price; if (oldValue != value) { _price = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Question type.
    /// </summary>
    [Obsolete("Left for backward compatibility with old format. Use TypeName and Parameters properties")]
    [DefaultValue(typeof(QuestionType), QuestionTypes.Simple)]
    internal QuestionType Type { get; } = new();

    /// <summary>
    /// Question type name.
    /// </summary>
    /// <remarks>
    /// Replaces deprecated <see cref="Type" /> property.
    /// </remarks>
    [DefaultValue(QuestionTypes.Default)]
    public string TypeName
    {
        get => _typeName;
        set { _typeName = value; }
    }

    /// <summary>
    /// Question scenario.
    /// </summary>
    [Obsolete("Left for backward compatibility with old format. Use Parameters property")]
    internal Scenario Scenario { get; } = new();

    /// <summary>
    /// Question script.
    /// </summary>
    /// <remarks>
    /// Replaces deprecated <see cref="Scenario" /> property.
    /// </remarks>
    public Script? Script { get; set; }

    /// <summary>
    /// Question parameters.
    /// </summary>
    /// <remarks>
    /// Replaces deprecated <see cref="Scenario" /> and <see cref="Type" /> properties.
    /// </remarks>
    public StepParameters Parameters { get; } = new();

    /// <summary>
    /// Right answers.
    /// </summary>
    public Answers Right { get; } = new();

    /// <summary>
    /// Wrong answers.
    /// </summary>
    public Answers Wrong { get; } = new();

    /// <inheritdoc />
    public override string ToString() =>
        Parameters != null && Parameters.TryGetValue(QuestionParameterNames.Question, out var questionBody)
            ? $"{_price}: {questionBody} ({Right.FirstOrDefault()})"
            : $"{_price}: ({(Right.Count > 0 ? Right[0] : "")})";

    /// <summary>
    /// Question name (not used).
    /// </summary>
    [DefaultValue("")]
    public override string Name => "";

    /// <inheritdoc />
    public override void ReadXml(XmlReader reader, PackageLimits? limits = null)
    {
        var priceStr = reader.GetAttribute("price");
        _ = int.TryParse(priceStr, out _price);

        if (reader.MoveToAttribute("type"))
        {
            _typeName = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        var right = true;
        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "info":
                            base.ReadXml(reader, limits);
                            read = false;
                            break;

                        case "type":
                            Type.Name = (reader.GetAttribute("name") ?? "").LimitLengthBy(limits?.TextLength);
                            break;

                        case "param":
                            if (limits == null || Type.Params.Count < limits.CollectionCount)
                            {
                                var param = new QuestionTypeParam
                                {
                                    Name = (reader.GetAttribute("name") ?? "").LimitLengthBy(limits?.TextLength),
                                    Value = reader.ReadElementContentAsString()
                                };

                                Type.Params.Add(param);
                                read = false;
                            }
                            break;

                        case "script":
                            Script = new();
                            Script.ReadXml(reader, limits);
                            read = false;
                            break;

                        case "params":
                            if (!reader.IsEmptyElement)
                            {
                                Parameters.ReadXml(reader, limits);
                                read = false;
                            }
                            break;

                        case "atom":
                            if (limits == null || Scenario.Count < limits.ContentItemCount)
                            {
                                var atom = new Atom();

                                if (reader.MoveToAttribute("time"))
                                {
                                    if (int.TryParse(reader.Value, out int time))
                                    {
                                        atom.AtomTime = time;
                                    }
                                }

                                if (reader.MoveToAttribute("type"))
                                {
                                    atom.Type = reader.Value.LimitLengthBy(limits?.TextLength);
                                }

                                reader.MoveToElement();
                                atom.Text = reader.ReadElementContentAsString().LimitLengthBy(limits?.ContentValueLength);

                                Scenario.Add(atom);
                                read = false;
                            }
                            break;

                        case "right":
                            right = true;
                            break;

                        case "wrong":
                            right = false;
                            break;

                        case "answer":
                            var answer = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);

                            if (right)
                            {
                                Right.Add(answer);
                            }
                            else
                            {
                                Wrong.Add(answer);
                            }

                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "question")
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }

        if (Right.Count == 0)
        {
            Right.Add("");
        }
    }

    /// <inheritdoc />
    public override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("question");
        writer.WriteAttributeString("price", _price.ToString());

        if (_typeName != QuestionTypes.Default)
        {
            writer.WriteAttributeString("type", _typeName.ToString());
        }

        base.WriteXml(writer);

        if (Script != null)
        {
            writer.WriteStartElement("script");
            Script.WriteXml(writer);
            writer.WriteEndElement();
        }

        if (Parameters != null)
        {
            writer.WriteStartElement("params");
            Parameters.WriteXml(writer);
            writer.WriteEndElement();
        }

        writer.WriteStartElement("right");

        foreach (var item in Right)
        {
            writer.WriteElementString("answer", item);
        }

        writer.WriteEndElement();

        if (Wrong.Any())
        {
            writer.WriteStartElement("wrong");

            foreach (var item in Wrong)
            {
                writer.WriteElementString("answer", item);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Creates a copy of this question.
    /// </summary>
    public Question Clone()
    {
        var question = new Question { _price = _price, TypeName = _typeName };

        question.SetInfoFromOwner(this);

        if (Parameters != null)
        {
            foreach (var parameter in Parameters)
            {
                question.Parameters[parameter.Key] = parameter.Value.Clone();
            }
        }

        question.Right.Clear();

        question.Right.AddRange(Right);
        question.Wrong.AddRange(Wrong);

        return question;
    }

    /// <summary>
    /// Upgrades the question to new format.
    /// </summary>
    internal void Upgrade()
    {
        if (Price == InvalidPrice)
        {
            Scenario.Clear();
            Type.Params.Clear();
            TypeName = Type.Name = QuestionTypes.Default;
            return;
        }

        switch (Type.Name)
        {
            case QuestionTypes.Auction:
                {
                    TypeName = QuestionTypes.Stake;
                }
                break;

            case QuestionTypes.Sponsored:
                {
                    TypeName = QuestionTypes.NoRisk;
                }
                break;

            case QuestionTypes.BagCat:
            case QuestionTypes.Cat:
                {
                    var theme = Type[QuestionTypeParams.Cat_Theme] ?? "";
                    var price = Type[QuestionTypeParams.Cat_Cost] ?? "";

                    var knows = Type.Name == QuestionTypes.BagCat
                        ? Type[QuestionTypeParams.BagCat_Knows] ?? QuestionTypeParams.BagCat_Knows_Value_After
                        : QuestionTypeParams.BagCat_Knows_Value_After;

                    var canGiveSelf = Type.Name == QuestionTypes.BagCat
                        ? Type[QuestionTypeParams.BagCat_Self] ?? QuestionTypeParams.BagCat_Self_Value_False
                        : QuestionTypeParams.BagCat_Self_Value_False;

                    var selectAnswererMode = canGiveSelf == QuestionTypeParams.BagCat_Self_Value_True
                        ? StepParameterValues.SetAnswererSelect_Any
                        : StepParameterValues.SetAnswererSelect_ExceptCurrent;

                    var numberSet = (NumberSet?)new NumberSetTypeConverter().ConvertFromString(price) ?? new NumberSet();

                    switch (knows)
                    {
                        case QuestionTypeParams.BagCat_Knows_Value_Never:
                            Scenario.Clear();
                            Type.Params.Clear();
                            Type.Name = QuestionTypes.Simple;
                            TypeName = QuestionTypes.SecretNoQuestion;

                            Parameters[QuestionParameterNames.Price] = new StepParameter
                            {
                                Type = StepParameterTypes.NumberSet,
                                NumberSetValue = numberSet
                            };

                            Parameters[QuestionParameterNames.SelectionMode] = new StepParameter { SimpleValue = selectAnswererMode };
                            return;

                        case QuestionTypeParams.BagCat_Knows_Value_Before:
                        case QuestionTypeParams.BagCat_Knows_Value_After:
                        default:
                            TypeName = knows == QuestionTypeParams.BagCat_Knows_Value_Before ? QuestionTypes.SecretPublicPrice : QuestionTypes.Secret;

                            Parameters[QuestionParameterNames.Theme] = new StepParameter { SimpleValue = theme };
                            
                            Parameters[QuestionParameterNames.Price] = new StepParameter
                            {
                                Type = StepParameterTypes.NumberSet,
                                NumberSetValue = numberSet
                            };
                            
                            Parameters[QuestionParameterNames.SelectionMode] = new StepParameter { SimpleValue = selectAnswererMode };
                            break;
                    }
                }
                break;

            case QuestionTypes.Simple:
                TypeName = QuestionTypes.Default;
                break;

            default:
                TypeName = Type.Name;
                Type.Name = QuestionTypes.Default;

                foreach (var item in Type.Params)
                {
                    Parameters.Add(item.Name, new StepParameter { SimpleValue = item.Value });
                }

                Type.Params.Clear();
                break;
        }

        var content = new StepParameter { Type = StepParameterTypes.Content, ContentValue = new() };

        Parameters[QuestionParameterNames.Question] = content;

        var currentContent = content;
        var useMarker = false;

        foreach (var atom in Scenario)
        {
            if (atom.Type == AtomTypes.Marker)
            {
                if (useMarker)
                {
                    continue;
                }

                useMarker = true;
                currentContent = new StepParameter { Type = StepParameterTypes.Content, ContentValue = new() };
                continue;
            }

            currentContent.ContentValue.Add(
                new ContentItem
                {
                    Type = GetContentType(atom.Type),
                    Duration = atom.AtomTime != -1 ? TimeSpan.FromSeconds(atom.AtomTime) : TimeSpan.Zero,
                    Value = atom.IsLink ? atom.Text.ExtractLink() : atom.Text,
                    Placement = GetPlacement(atom.Type),
                    WaitForFinish = atom.AtomTime != -1,
                    IsRef = atom.IsLink
                });            
        }

        if (useMarker && currentContent.ContentValue.Count > 0)
        {
            Parameters[QuestionParameterNames.Answer] = currentContent;
        }

        Scenario.Clear();
        Type.Params.Clear();
        Type.Name = QuestionTypes.Simple;
    }

    private static string GetContentType(string type) =>
        type switch
        {
            AtomTypes.Oral => AtomTypes.Text,
            AtomTypes.Audio => AtomTypes.AudioNew,
            _ => type,
        };

    private static string GetPlacement(string type) =>
        type switch
        {
            AtomTypes.Oral => ContentPlacements.Replic,
            AtomTypes.Audio => ContentPlacements.Background,
            _ => ContentPlacements.Screen,
        };

    /// <inheritdoc />
    public bool Equals(Question? other) =>
        other is not null
        && Price.Equals(other.Price)
        && Equals(Script, other.Script)
        && Equals(Parameters, other.Parameters)
        && Right.Equals(other.Right)
        && Wrong.Equals(other.Wrong);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Question);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Price, Script, Parameters, Right, Wrong);

    /// <summary>
    /// Gets question text part.
    /// </summary>
    public string GetText()
    {
        if (Script == null)
        {
            if (Parameters.TryGetValue(QuestionParameterNames.Question, out var question))
            {
                return GetTextFromContent(question);
            }

            return "";
        }

        var result = new StringBuilder();

        for (int i = 0; i < Script.Steps.Count; i++)
        {
            if (Script.Steps[i].Type == StepTypes.AskAnswer)
            {
                break;
            }

            if (Script.Steps[i].Type != StepTypes.ShowContent)
            {
                continue;
            }

            if (!Script.Steps[i].Parameters.TryGetValue(StepParameterNames.Content, out var content))
            {
                continue;
            }

            if (result.Length > 0)
            {
                result.Append(UniversalLineSeparatorChar);
            }

            result.Append(GetTextFromContent(content));
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets question content parts.
    /// </summary>
    public IEnumerable<ContentItem> GetContent() => GetContentFromParameters(Parameters);

    private static IEnumerable<ContentItem> GetContentFromParameters(StepParameters parameters)
    {
        foreach (var parameter in parameters.Values)
        {
            if (parameter.ContentValue == null)
            {
                if (parameter.GroupValue != null)
                {
                    foreach (var contentItem in GetContentFromParameters(parameter.GroupValue))
                    {
                        yield return contentItem;
                    }
                }

                continue;
            }

            foreach (var contentItem in parameter.ContentValue)
            {
                yield return contentItem;
            }
        }
    }

    private static string GetTextFromContent(StepParameter content)
    {
        if (content.ContentValue == null)
        {
            if (content.GroupValue != null)
            {
                var sb = new StringBuilder();

                foreach (var parameter in content.GroupValue.Values)
                {
                    var parameterText = GetTextFromContent(parameter);

                    if (parameterText.Length > 0)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(UniversalLineSeparatorChar);
                        }

                        sb.Append(parameterText);
                    }
                }

                return sb.ToString();
            }

            return "";
        }

        var result = new StringBuilder();

        foreach (var contentItem in content.ContentValue)
        {
            if (contentItem.Type != ContentTypes.Text || contentItem.Value.Length == 0)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result.Append(UniversalLineSeparatorChar);
            }

            result.Append(contentItem.Value);
        }

        return result.ToString();
    }

    public bool HasMediaContent()
    {
        foreach (var item in GetContent())
        {
            if (item.Type != ContentTypes.Text)
            {
                return true;
            }
        }

        return false;
    }
}
