using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a game round.
/// </summary>
/// <inheritdoc cref="InfoOwner" />
public sealed class Round : InfoOwner, IEquatable<Round>
{
    /// <summary>
    /// Round themes.
    /// </summary>
    public List<Theme> Themes { get; } = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = RoundTypes.Standart;

    /// <summary>
    /// Round type.
    /// </summary>
    [DefaultValue(RoundTypes.Standart)]
    public string Type
    {
        get => _type;
        set { var oldValue = _type; if (oldValue != value) { _type = value; OnPropertyChanged(oldValue); } }
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Resources.Round}: {Resources.Round}";

    /// <summary>
    /// Создание темы
    /// </summary>
    /// <param name="name">Название темы</param>
    public Theme CreateTheme(string? name)
    {
        var theme = new Theme { Name = name ?? "" };
        Themes.Add(theme);
        return theme;
    }

    /// <inheritdoc/>
    public override void ReadXml(XmlReader reader)
    {
        Name = reader.GetAttribute("name") ?? "";

        if (reader.MoveToAttribute("type"))
        {
            _type = reader.Value;
        }

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

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

                        case "theme":
                            var theme = new Theme();
                            theme.ReadXml(reader);
                            Themes.Add(theme);
                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "round")
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("round");
        writer.WriteAttributeString("name", Name);

        if (_type != RoundTypes.Standart)
        {
            writer.WriteAttributeString("type", _type);
        }

        base.WriteXml(writer);

        if (Themes.Any())
        {
            writer.WriteStartElement("themes");

            foreach (var item in Themes)
            {
                item.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Creates a copy of round.
    /// </summary>
    public Round Clone()
    {
        var round = new Round
        {
            Name = Name,
            _type = _type
        };

        round.SetInfoFromOwner(this);

        foreach (var theme in Themes)
        {
            round.Themes.Add(theme.Clone());
        }

        return round;
    }

    /// <inheritdoc />
    public bool Equals(Round? other) => other is not null && base.Equals(other) && Type.Equals(other.Type) && Themes.SequenceEqual(other.Themes);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Round);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Type, Themes.GetCollectionHashCode());
}
