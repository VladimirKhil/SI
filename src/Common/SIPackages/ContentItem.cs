﻿using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a package content item.
/// </summary>
public sealed class ContentItem : PropertyChangedNotifier, ITyped, IEquatable<ContentItem>
{
    private const string DefaultType = ContentTypes.Text;
    private const bool DefaultIsRef = false;
    private static readonly TimeSpan DefaultDuration = TimeSpan.Zero;
    private const bool DefaultWaitForFinish = true;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = DefaultType;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _value = "";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isRef = DefaultIsRef;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string? _placement;

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
    public string Placement
    {
        get => _placement ?? GetDefaultPlacement();
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

    private string GetDefaultPlacement() => _type == ContentTypes.Audio ? ContentPlacements.Background : ContentPlacements.Screen;

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
        if (_type == ContentTypes.Text && _placement == ContentPlacements.Screen)
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
    public void ReadXml(XmlReader reader, PackageLimits? limits)
    {
        if (reader.MoveToAttribute("type"))
        {
            Type = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        if (reader.MoveToAttribute("isRef"))
        {
            _ = bool.TryParse(reader.Value, out _isRef);
        }

        if (reader.MoveToAttribute("placement"))
        {
            _placement = reader.Value.LimitLengthBy(limits?.TextLength);
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
        _value = reader.ReadElementContentAsString().LimitLengthBy(limits?.ContentValueLength);
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("item");

        if (Type != DefaultType)
        {
            writer.WriteAttributeString("type", Type);
        }

        if (IsRef != DefaultIsRef)
        {
            writer.WriteAttributeString("isRef", IsRef.ToString());
        }

        if (_placement != null && _placement != GetDefaultPlacement())
        {
            writer.WriteAttributeString("placement", _placement);
        }
        else if (Type == ContentTypes.Audio)
        {
            writer.WriteAttributeString("placement", GetDefaultPlacement());
        }

        if (Duration != DefaultDuration)
        {
            writer.WriteAttributeString("duration", Duration.ToString());
        }

        if (WaitForFinish != DefaultWaitForFinish)
        {
            writer.WriteAttributeString("waitForFinish", WaitForFinish.ToString());
        }

        writer.WriteValue(_value);
        writer.WriteEndElement();
    }

    internal ContentItem Clone() => new()
    {
        Duration = _duration,
        IsRef = _isRef,
        _placement = _placement,
        WaitForFinish = _waitForFinish,
        Type = _type,
        Value = _value
    };
}
