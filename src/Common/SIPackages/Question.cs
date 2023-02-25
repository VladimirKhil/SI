using SIPackages.Core;
using SIPackages.TypeConverters;
using System.ComponentModel;
using System.Diagnostics;
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _price;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string? _typeName;

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
    [DefaultValue(typeof(QuestionType), QuestionTypes.Simple)]
    public QuestionType Type { get; } = new();

    /// <summary>
    /// Question type name.
    /// </summary>
    public string? TypeName
    {
        get => _typeName;
        set { var oldValue = _typeName; if (oldValue != value) { _typeName = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Question scenario.
    /// </summary>
    public Scenario Scenario { get; } = new();

    /// <summary>
    /// Question script.
    /// </summary>
    public Script? Script { get; set; }

    /// <summary>
    /// Question parameters.
    /// </summary>
    public StepParameters? Parameters { get; set; }

    /// <summary>
    /// Right answers.
    /// </summary>
    public Answers Right { get; } = new();

    /// <summary>
    /// Wrong answers.
    /// </summary>
    public Answers Wrong { get; } = new();

    /// <inheritdoc />
    public override string ToString() => $"{_price}: {Scenario} ({(Right.Count > 0 ? Right[0] : "")})";

    /// <summary>
    /// Question name (not used).
    /// </summary>
    [DefaultValue("")]
    public override string Name => "";

    /// <inheritdoc />
    public override bool Contains(string value) =>
        base.Contains(value) ||
        Type.Contains(value) ||
        Scenario.ContainsQuery(value) ||
        Right.ContainsQuery(value) ||
        Wrong.ContainsQuery(value);

    /// <inheritdoc />
    public override IEnumerable<SearchData> Search(string value)
    {
        foreach (var item in base.Search(value))
        {
            yield return item;
        }

        foreach (var item in Type.Search(value))
        {
            yield return item;
        }

        foreach (var item in Scenario.Search(value))
        {
            yield return item;
        }

        foreach (var item in Right.Search(value))
        {
            item.Kind = ResultKind.Right;
            yield return item;
        }

        foreach (var item in Wrong.Search(value))
        {
            item.Kind = ResultKind.Wrong;
            yield return item;
        }
    }

    /// <inheritdoc />
    public override void ReadXml(XmlReader reader)
    {
        var priceStr = reader.GetAttribute("price");
        _ = int.TryParse(priceStr, out _price);

        if (reader.MoveToAttribute("type"))
        {
            _typeName = reader.Value;
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
                            base.ReadXml(reader);
                            read = false;
                            break;

                        case "type":
                            Type.Name = reader.GetAttribute("name");
                            break;

                        case "param":
                            var param = new QuestionTypeParam
                            {
                                Name = reader.GetAttribute("name"),
                                Value = reader.ReadElementContentAsString()
                            };

                            Type.Params.Add(param);
                            read = false;
                            break;

                        case "script":
                            Script = new();
                            Script.ReadXml(reader);
                            read = false;
                            break;

                        case "params":
                            Parameters = new();
                            Parameters.ReadXml(reader);
                            read = false;
                            break;

                        case "atom":
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
                                atom.Type = reader.Value;
                            }

                            reader.MoveToElement();
                            atom.Text = reader.ReadElementContentAsString();

                            Scenario.Add(atom);
                            read = false;
                            break;

                        case "right":
                            right = true;
                            break;

                        case "wrong":
                            right = false;
                            break;

                        case "answer":
                            var answer = reader.ReadElementContentAsString();

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
    }

    /// <inheritdoc />
    public override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("question");
        writer.WriteAttributeString("price", _price.ToString());

        if (_typeName != null && _typeName != QuestionTypes.Simple)
        {
            writer.WriteAttributeString("type", _typeName.ToString());
        }

        base.WriteXml(writer);

        if (Type.Name != QuestionTypes.Simple)
        {
            writer.WriteStartElement("type");
            writer.WriteAttributeString("name", Type.Name);

            foreach (var item in Type.Params)
            {
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", item.Name);
                writer.WriteValue(item.Value);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        if (Scenario.Any())
        {
            writer.WriteStartElement("scenario");

            foreach (var atom in Scenario)
            {
                writer.WriteStartElement("atom");

                if (atom.AtomTime != 0)
                {
                    writer.WriteAttributeString("time", atom.AtomTime.ToString());
                }

                if (atom.Type != AtomTypes.Text)
                {
                    writer.WriteAttributeString("type", atom.Type);
                }

                writer.WriteValue(atom.Text);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

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
        var question = new Question { _price = _price };
        question.Type.Name = Type.Name;

        question.SetInfoFromOwner(this);

        foreach (var atom in Scenario)
        {
            question.Scenario.Add(new Atom { AtomTime = atom.AtomTime, Text = atom.Text, Type = atom.Type });
        }

        question.Right.Clear();

        question.Right.AddRange(Right);
        question.Wrong.AddRange(Wrong);

        foreach (var par in Type.Params)
        {
            question.Type[par.Name] = par.Value;
        }

        return question;
    }

    /// <summary>
    /// Upgrades the question to new format.
    /// </summary>
    /// <param name="isFinal">Final round question flag.</param>
    public void Upgrade(bool isFinal = false)
    {
        if (TypeName != null || Script != null)
        {
            return;
        }

        Script = new();

        if (Price == -1)
        {
            Scenario.Clear();
            Type.Params.Clear();
            TypeName = Type.Name = QuestionTypes.Simple;
            return;
        }

        var askAnswerMode = StepParameterValues.AskAnswerMode_Button;

        switch (Type.Name)
        {
            case QuestionTypes.Auction:
                {
                    var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };

                    setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Stake);
                    setAnswererStep.AddSimpleParameter(StepParameterNames.Select, StepParameterValues.SetAnswererSelect_Highest);
                    setAnswererStep.AddSimpleParameter(StepParameterNames.StakeVisibity, StepParameterValues.SetAnswererStakeVisibility_Visible);

                    Script.Steps.Add(setAnswererStep);

                    askAnswerMode = StepParameterValues.AskAnswerMode_Direct;
                    TypeName = QuestionTypes.Stake;
                }
                break;

            case QuestionTypes.Sponsored:
                {
                    var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };
                    setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Current);
                    Script.Steps.Add(setAnswererStep);

                    var setPriceStep = new Step { Type = StepTypes.SetPrice };
                    setPriceStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetPriceMode_NoRisk);
                    Script.Steps.Add(setPriceStep);

                    askAnswerMode = StepParameterValues.AskAnswerMode_Direct;
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

                    var canGiveSelf = TypeName == QuestionTypes.BagCat
                        ? Type[QuestionTypeParams.BagCat_Self] ?? QuestionTypeParams.BagCat_Self_Value_False
                        : QuestionTypeParams.BagCat_Self_Value_False;

                    var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };
                    setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_ByCurrent);

                    var selectAnswererMode = canGiveSelf == QuestionTypeParams.BagCat_Self_Value_True
                        ? StepParameterValues.SetAnswererSelect_Any
                        : StepParameterValues.SetAnswererSelect_ExceptCurrent;

                    setAnswererStep.AddSimpleParameter(StepParameterNames.Select, selectAnswererMode);

                    var setThemeStep = new Step { Type = StepTypes.SetTheme };
                    setThemeStep.AddSimpleParameter(StepParameterNames.Content, theme);

                    var setPriceStep = new Step { Type = StepTypes.SetPrice };
                    setPriceStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetPriceMode_Select);

                    var numberSet = (NumberSet?)new NumberSetTypeConverter().ConvertFromString(price);

                    setPriceStep.Parameters.Add(
                        StepParameterNames.Content,
                        new StepParameter
                        {
                            Type = StepParameterTypes.NumberSet,
                            NumberSetValue = numberSet
                        });

                    switch (knows)
                    {
                        case QuestionTypeParams.BagCat_Knows_Value_Before:
                            Script.Steps.Add(setThemeStep);
                            Script.Steps.Add(setPriceStep);
                            Script.Steps.Add(setAnswererStep);
                            TypeName = QuestionTypes.SecretOpenerPrice;

                            Parameters = new StepParameters
                            {
                                [QuestionParameterNames.Theme] = new StepParameter { SimpleValue = theme },
                                [QuestionParameterNames.Price] = new StepParameter
                                {
                                    Type = StepParameterTypes.NumberSet,
                                    NumberSetValue = numberSet
                                },
                                [QuestionParameterNames.SelectionMode] = new StepParameter { SimpleValue = selectAnswererMode },
                            };
                            break;

                        case QuestionTypeParams.BagCat_Knows_Value_Never:
                            Script.Steps.Add(setAnswererStep);
                            Script.Steps.Add(setThemeStep);
                            Script.Steps.Add(setPriceStep);
                            Script.Steps.Add(new Step { Type = StepTypes.Accept });

                            Scenario.Clear();
                            Type.Params.Clear();
                            Type.Name = QuestionTypes.Simple;
                            TypeName = QuestionTypes.SecretNoQuestion;
                            return;

                        case QuestionTypeParams.BagCat_Knows_Value_After:
                        default:
                            Script.Steps.Add(setAnswererStep);
                            Script.Steps.Add(setThemeStep);
                            Script.Steps.Add(setPriceStep);
                            TypeName = QuestionTypes.Secret;

                            Parameters = new StepParameters
                            {
                                [QuestionParameterNames.Theme] = new StepParameter { SimpleValue = theme },
                                [QuestionParameterNames.Price] = new StepParameter
                                {
                                    Type = StepParameterTypes.NumberSet,
                                    NumberSetValue = numberSet
                                },
                                [QuestionParameterNames.SelectionMode] = new StepParameter { SimpleValue = selectAnswererMode },
                            };
                            break;
                    }

                    askAnswerMode = StepParameterValues.AskAnswerMode_Direct;
                }
                break;

            case QuestionTypes.Simple:
                if (isFinal)
                {
                    var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };

                    setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Stake);
                    setAnswererStep.AddSimpleParameter(StepParameterNames.Select, StepParameterValues.SetAnswererSelect_AllPossible);
                    setAnswererStep.AddSimpleParameter(StepParameterNames.StakeVisibity, StepParameterValues.SetAnswererStakeVisibility_Hidden);

                    Script.Steps.Add(setAnswererStep);

                    askAnswerMode = StepParameterValues.AskAnswerMode_Direct;
                }

                TypeName = QuestionTypes.Simple;
                break;

            default:
                Scenario.Clear();
                Type.Params.Clear();
                TypeName = Type.Name;
                Type.Name = QuestionTypes.Simple;
                return;
        }

        var contentStep = new Step { Type = StepTypes.ShowContent };
        var content = new StepParameter { Type = StepParameterTypes.Content, ContentValue = new() };
        contentStep.Parameters.Add(StepParameterNames.Content, content);

        Script.Steps.Add(contentStep);

        var askAnswerStep = new Step { Type = StepTypes.AskAnswer };
        askAnswerStep.AddSimpleParameter(StepParameterNames.Mode, askAnswerMode);

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
                Script.Steps.Add(askAnswerStep);

                var answerStep = new Step { Type = StepTypes.ShowContent };
                var answerContent = new StepParameter { Type = StepParameterTypes.Content, ContentValue = new() };
                answerStep.Parameters.Add(StepParameterNames.Content, answerContent);

                Script.Steps.Add(answerStep);

                currentContent = answerContent;
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

        if (!useMarker)
        {
            Script.Steps.Add(askAnswerStep);

            var answerStep = new Step { Type = StepTypes.ShowContent };

            var answerContent = new StepParameter
            {
                IsRef = true,
                Type = StepParameterTypes.Simple,
                SimpleValue = StepParameterValues.ContentRef_Answer,
            };

            answerStep.Parameters.Add(StepParameterNames.Content, answerContent);

            answerStep.Parameters.Add(
                StepParameterNames.FallbackRefId,
                new StepParameter
                {
                    IsRef = true,
                    Type = StepParameterTypes.Simple,
                    SimpleValue = StepParameterValues.FallbackStepIdRef_Right,
                });

            Script.Steps.Add(answerStep);
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

    /// <summary>
    /// Gets question right answers.
    /// </summary>
    [Obsolete("Use Right property")]
    public IList<string> GetRightAnswers() => Right;

    /// <inheritdoc />
    public bool Equals(Question? other) =>
        other is not null
        && Price.Equals(other.Price)
        && Type.Equals(other.Type)
        && Scenario.Equals(other.Scenario)
        && Equals(Script, other.Script)
        && Equals(Parameters, other.Parameters)
        && Right.Equals(other.Right)
        && Wrong.Equals(other.Wrong);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Question);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Price, Type, Scenario, Script, Parameters, Right, Wrong);
}
