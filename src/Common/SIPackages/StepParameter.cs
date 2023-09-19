using SIPackages.Core;
using SIPackages.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a question script step parameter.
/// </summary>
public sealed class StepParameter : PropertyChangedNotifier, ITyped, IEquatable<StepParameter>, IXmlSerializable
{
    private const string DefaultType = StepParameterTypes.Simple;

    private const bool DefaultIsRef = false;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = DefaultType;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _simpleValue = "";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private List<ContentItem>? _contentValue;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private StepParameters? _groupValue;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private NumberSet? _numberSetValue;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isRef = DefaultIsRef;

    /// <summary>
    /// Parameter type.
    /// </summary>
    [DefaultValue(DefaultType)]
    public string Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                var oldValue = _type;
                _type = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Parameter value for simple type.
    /// </summary>
    /// <remarks>
    /// If <see cref="IsRef" /> is true, the value is a reference to a parameter inside the data section.
    /// </remarks>
    public string SimpleValue
    {
        get => _simpleValue;
        set
        {
            if (_simpleValue != value)
            {
                var oldValue = _simpleValue;
                _simpleValue = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Parameter value for content type.
    /// </summary>
    public List<ContentItem>? ContentValue
    {
        get => _contentValue;
        set
        {
            if (_contentValue != value)
            {
                var oldValue = _contentValue;
                _contentValue = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Parameter value for group type.
    /// </summary>
    public StepParameters? GroupValue
    {
        get => _groupValue;
        set
        {
            if (_groupValue != value)
            {
                var oldValue = _groupValue;
                _groupValue = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Parameter value for number set type.
    /// </summary>
    public NumberSet? NumberSetValue
    {
        get => _numberSetValue;
        set
        {
            if (_numberSetValue != value)
            {
                var oldValue = _numberSetValue;
                _numberSetValue = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Marks that the <see cref="SimpleValue" /> contains a reference to a parameter inside the data section.
    /// </summary>
    [DefaultValue(DefaultIsRef)]
    public bool IsRef
    {
        get => _isRef;
        set
        {
            if (_isRef != value)
            {
                var oldValue = _isRef;
                _isRef = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Does parameter contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public bool ContainsQuery(string value) => _type switch
    {
        StepParameterTypes.Content => _contentValue != null && _contentValue.Any(item => item.Value.Contains(value)),
        StepParameterTypes.Group => _groupValue != null && _groupValue.ContainsQuery(value),
        StepParameterTypes.NumberSet => _numberSetValue != null && _numberSetValue.ToString().Contains(value),
        _ => _simpleValue.Contains(value),
    };

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public IEnumerable<SearchData> Search(string value)
    {
        switch (_type)
        {
            case StepParameterTypes.Content:
                if (_contentValue == null)
                {
                    break;
                }

                foreach (var item in _contentValue)
                {
                    foreach (var searchResult in SearchExtensions.Search(ResultKind.Text, item.Value, value))
                    {
                        yield return searchResult;
                    }
                }
                break;

            case StepParameterTypes.Group:
                if (_groupValue == null)
                {
                    break;
                }

                foreach (var searchResult in _groupValue.Search(value))
                {
                    yield return searchResult;
                }
                break;

            case StepParameterTypes.NumberSet:
                if (_numberSetValue == null)
                {
                    break;
                }

                foreach (var searchResult in SearchExtensions.Search(ResultKind.Text, _numberSetValue.ToString(), value))
                {
                    yield return searchResult;
                }
                break;

            default:
                foreach (var searchResult in SearchExtensions.Search(ResultKind.Text, _simpleValue, value))
                {
                    yield return searchResult;
                }
                break;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (_isRef)
        {
            return $"{_type}:@{_simpleValue}";
        }

        var value = _type switch
        {
            StepParameterTypes.Content => _contentValue == null ? "" : string.Join("; ", _contentValue.Select(item => item.ToString())),
            StepParameterTypes.Group => _groupValue == null ? "" : string.Join("; ", _groupValue.Select(p => $"{p.Key}: {p.Value}")),
            StepParameterTypes.NumberSet => _numberSetValue == null ? "" : _numberSetValue.ToString(),
            _ => _simpleValue,
        };

        return $"{_type}:@{value}";
    }

    /// <inheritdoc />
    public bool Equals(StepParameter? other) =>
        other is not null
        && Type.Equals(other.Type)
        && SimpleValue.Equals(other.SimpleValue)
        && IsRef.Equals(other.IsRef)
        && Equals(ContentValue, other.ContentValue)
        && Equals(GroupValue, other.GroupValue)
        && Equals(NumberSetValue, other.NumberSetValue);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as StepParameter);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Type, SimpleValue, IsRef, ContentValue?.GetCollectionHashCode(), GroupValue, NumberSetValue);

    /// <inheritdoc />
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
    {
        if (reader.MoveToAttribute("type"))
        {
            _type = reader.Value;
        }

        if (reader.MoveToAttribute("isRef"))
        {
            _ = bool.TryParse(reader.Value, out _isRef);
        }

        if (_isRef != DefaultIsRef)
        {
            reader.MoveToElement();
            _simpleValue = reader.ReadElementContentAsString();
        }
        else
        {
            switch (_type)
            {
                case StepParameterTypes.Content:
                    ReadContent(reader);
                    break;

                case StepParameterTypes.Group:
                    ReadGroup(reader);
                    break;

                case StepParameterTypes.NumberSet:
                    ReadNumberSet(reader);
                    break;

                default:
                    ReadValue(reader);
                    break;
            }
        }
    }

    private void ReadContent(XmlReader reader)
    {
        var read = true;
        _contentValue = new();

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "item":
                            var item = new ContentItem();
                            item.ReadXml(reader);
                            _contentValue.Add(item);
                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "param")
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }
    }

    private void ReadGroup(XmlReader reader)
    {
        _groupValue = new();
        reader.MoveToElement();
        _groupValue.ReadXml(reader);
    }

    private void ReadNumberSet(XmlReader reader)
    {
        _numberSetValue = new();

        reader.MoveToElement();
        reader.ReadToDescendant("numberSet");

        if (int.TryParse(reader.GetAttribute("minimum"), out var minimum))
        {
            _numberSetValue.Minimum = minimum;
        }

        if (int.TryParse(reader.GetAttribute("maximum"), out var maximum))
        {
            _numberSetValue.Maximum = maximum;
        }

        if (int.TryParse(reader.GetAttribute("step"), out var step))
        {
            _numberSetValue.Step = step;
        }

        reader.Read(); // numberSet
        reader.Read(); // param
    }

    private void ReadValue(XmlReader reader)
    {
        reader.MoveToElement();
        _simpleValue = reader.ReadElementContentAsString();
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        if (_type != DefaultType)
        {
            writer.WriteAttributeString("type", _type);
        }

        if (_isRef != DefaultIsRef)
        {
            writer.WriteAttributeString("isRef", _isRef.ToString());
            writer.WriteValue(_simpleValue);
        }
        else
        {
            switch (_type)
            {
                case StepParameterTypes.Content:
                    if (_contentValue == null)
                    {
                        break;
                    }

                    foreach (var item in _contentValue)
                    {
                        item.WriteXml(writer);
                    }
                    break;

                case StepParameterTypes.Group:
                    if (_groupValue == null)
                    {
                        break;
                    }

                    _groupValue.WriteXml(writer);
                    break;

                case StepParameterTypes.NumberSet:
                    if (_numberSetValue == null)
                    {
                        break;
                    }

                    writer.WriteStartElement("numberSet");
                    writer.WriteAttributeString("minimum", _numberSetValue.Minimum.ToString());
                    writer.WriteAttributeString("maximum", _numberSetValue.Maximum.ToString());
                    writer.WriteAttributeString("step", _numberSetValue.Step.ToString());
                    writer.WriteEndElement();
                    break;

                default:
                    writer.WriteValue(_simpleValue);
                    break;
            }
        }
    }

    internal StepParameter Clone() => new()
    {
        Type = _type,
        IsRef = _isRef,
        ContentValue = _contentValue?.Select(item => item.Clone()).ToList(),
        GroupValue = _groupValue?.Clone(),
        NumberSetValue = _numberSetValue?.Clone(),
        SimpleValue = _simpleValue
    };
}
