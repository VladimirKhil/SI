using SIPackages.Core;
using SIPackages.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a question script step.
/// </summary>
public sealed class Step : PropertyChangedNotifier, ITyped, IEquatable<Step>, IXmlSerializable
{
    private const string DefaultType = StepTypes.ShowContent;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = DefaultType;

    /// <summary>
    /// Step type.
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
    /// Step parameters.
    /// </summary>
    public StepParameters Parameters { get; } = new();

    /// <summary>
    /// Adds simple parameter to the step.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="value">Parameter value.</param>
    public void AddSimpleParameter(string name, string value) => Parameters.Add(name, new StepParameter { SimpleValue = value });

    /// <summary>
    /// Tries to get parameter by name.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    public StepParameter? TryGetParameter(string name) => Parameters.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// Tries to get parameter by name.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    public string? TryGetSimpleParameter(string name) => TryGetParameter(name)?.SimpleValue;

    /// <inheritdoc />
    public override string ToString() => $"{_type}({string.Join(", ", Parameters)})";

    /// <inheritdoc />
    public bool Equals(Step? other) =>
        other is not null
        && Type.Equals(other.Type)
        && Parameters.SequenceEqual(other.Parameters);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Step);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Type, Parameters.GetCollectionHashCode());

    /// <inheritdoc />
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
    {
        if (reader.MoveToAttribute("type"))
        {
            _type = reader.Value;
        }

        Parameters.ReadXml(reader);
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        if (_type != DefaultType)
        {
            writer.WriteAttributeString("type", _type);
        }

        Parameters.WriteXml(writer);
    }
}
