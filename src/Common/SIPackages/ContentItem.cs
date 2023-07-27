using SIPackages.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a package content item.
/// </summary>
public sealed class ContentItem : PropertyChangedNotifier, ITyped, IEquatable<ContentItem>, IXmlSerializable
{
    private const string DefaultType = AtomTypes.Text;
    private const bool DefaultIsRef = false;
    private const string DefaultPlacement = ContentPlacements.Screen;
    private static readonly TimeSpan DefaultDuration = TimeSpan.Zero;
    private const bool DefaultWaitForFinish = true;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = DefaultType;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _value = "";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isRef = DefaultIsRef;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _placement = DefaultPlacement;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private TimeSpan _duration = DefaultDuration;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _waitForFinish = DefaultWaitForFinish;

    /// <summary>
    /// Content type.
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
    /// Content value.
    /// </summary>
    /// <remarks>
    /// If <see cref="IsRef" /> is true, the value is a reference to a media resource inside the package.
    /// </remarks>
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                var oldValue = _value;
                _value = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Marks that the <see cref="Value" /> contains a reference to a media resource inside the package.
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
    /// Content placement.
    /// </summary>
    [DefaultValue(DefaultPlacement)]
    public string Placement
    {
        get => _placement;
        set
        {
            if (_placement != value)
            {
                var oldValue = _placement;
                _placement = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Content play duration.
    /// </summary>
    [DefaultValue(typeof(TimeSpan), "0")]
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                var oldValue = _duration;
                _duration = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Should the play wait for the content to finish to move futher.
    /// </summary>
    [DefaultValue(DefaultWaitForFinish)]
    public bool WaitForFinish
    {
        get => _waitForFinish;
        set
        {
            if (_waitForFinish != value)
            {
                var oldValue = _waitForFinish;
                _waitForFinish = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Does the value contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public bool Contains(string value) => _value.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

    /// <inheritdoc />
    public override string ToString()
    {
        if (_type == AtomTypes.Text && _placement == ContentPlacements.Screen)
        {
            return _value;
        }

        var res = new StringBuilder();
        res.AppendFormat("#{0}:{1} ", _placement, _type);
        res.Append(_value);

        return res.ToString();
    }

    /// <inheritdoc />
    public bool Equals(ContentItem? other) =>
        other is not null
        && Type.Equals(other.Type)
        && Value.Equals(other.Value)
        && IsRef.Equals(other.IsRef)
        && Placement.Equals(other.Placement)
        && Duration.Equals(other.Duration)
        && WaitForFinish.Equals(other.WaitForFinish);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ContentItem);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Type, Value, IsRef, Placement, Duration, WaitForFinish);

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

        if (reader.MoveToAttribute("placement"))
        {
            _placement = reader.Value;
        }

        if (reader.MoveToAttribute("duration"))
        {
            _ = TimeSpan.TryParse(reader.Value, out _duration);
        }

        if (reader.MoveToAttribute("waitForFinish"))
        {
            _ = bool.TryParse(reader.Value, out _waitForFinish);
        }

        reader.MoveToElement();
        _value = reader.ReadElementContentAsString();
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        if (_type != DefaultType)
        {
            writer.WriteAttributeString("type", _type);
        }

        if (_isRef != DefaultIsRef)
        {
            writer.WriteAttributeString("isRef", _isRef.ToString());
        }

        if (_placement != DefaultPlacement)
        {
            writer.WriteAttributeString("placement", _placement);
        }

        if (_duration != DefaultDuration)
        {
            writer.WriteAttributeString("duration", _duration.ToString());
        }

        if (_waitForFinish != DefaultWaitForFinish)
        {
            writer.WriteAttributeString("waitForFinish", _waitForFinish.ToString());
        }

        writer.WriteValue(_value);
        writer.WriteEndElement();
    }

    internal ContentItem Clone() => new()
    {
        Duration = _duration,
        IsRef = _isRef,
        Placement = _placement,
        WaitForFinish = _waitForFinish,
        Type = _type,
        Value = _value
    };
}
