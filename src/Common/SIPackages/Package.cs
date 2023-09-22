using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Represents a SIGame package object.
/// </summary>
public sealed class Package : InfoOwner, IEquatable<Package>
{
    /// <summary>
    /// Default package version.
    /// </summary>
    public const double DefaultVersion = 4.0;

    /// <summary>
    /// Maximum supported package version.
    /// </summary>
    public const double MaximumSupportedVersion = 5.0;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private double _version = DefaultVersion;

    /// <summary>
    /// Package version.
    /// </summary>
    public double Version { get => _version; set { _version = value; } }

    private string _id = "";

    /// <summary>
    /// Unique package identifier.
    /// </summary>
    public string ID
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                var oldValue = _id;
                _id = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private string _restriction = "";

    /// <summary>
    /// Package restrictions (by age, region etc.).
    /// </summary>
    [DefaultValue("")]
    public string Restriction
    {
        get => _restriction;
        set
        {
            var oldValue = _restriction;

            if (_restriction != value)
            {
                _restriction = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private string _publisher = "";

    /// <summary>
    /// Package publisher.
    /// </summary>
    [DefaultValue("")]
    public string Publisher
    {
        get => _publisher;
        set
        {
            if (_publisher != value)
            {
                var oldValue = _publisher;
                _publisher = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private string _contactUri = "";

    /// <summary>
    /// Package author contact uri.
    /// </summary>
    [DefaultValue("")]
    public string ContactUri
    {
        get => _contactUri;
        set
        {
            if (_contactUri != value)
            {
                var oldValue = _contactUri;
                _contactUri = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private int _difficulty = 5;

    /// <summary>
    /// Package difficulty (from 0 to 10).
    /// </summary>
    public int Difficulty
    {
        get => _difficulty;
        set
        {
            if (_difficulty != value)
            {
                var oldValue = _difficulty;
                _difficulty = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private string _logo = "";

    /// <summary>
    /// Package logo link.
    /// </summary>
    [DefaultValue("")]
    public string Logo
    {
        get => _logo;
        set
        {
            if (_logo != value)
            {
                var oldValue = _logo;
                _logo = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Gets package logo content item.
    /// </summary>
    public ContentItem? LogoItem
    {
        get
        {
            if (string.IsNullOrEmpty(Logo))
            {
                return null;
            }

            var link = Logo.ExtractLink();

            if (string.IsNullOrEmpty(link)) // External link
            {
                return new ContentItem { IsRef = false, Value = Logo, Type = AtomTypes.Image };
            }

            return new ContentItem { IsRef = true, Value = link, Type = AtomTypes.Image };
        }
    }

    private string _date = "";

    /// <summary>
    /// Package creation data (in arbitrary form - "year 2005", "31.12.2014", "2008 fall" etc.).
    /// </summary>
    public string Date
    {
        get => _date;
        set
        {
            if (_date != value)
            {
                var oldValue = _date;
                _date = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    private string _language = "";

    /// <summary>
    /// Package language.
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                var oldValue = _language;
                _language = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <summary>
    /// Package tags.
    /// </summary>
    public List<string> Tags { get; } = new List<string>();

    /// <summary>
    /// Global package data.
    /// </summary>
    public GlobalData Global { get; set; } = new();

    /// <summary>
    /// Package rounds.
    /// </summary>
    public List<Round> Rounds { get; } = new List<Round>();

    /// <inheritdoc />
    public override string ToString() => $"{Resources.Package}: {Name}";

    /// <summary>
    /// Creates a new round.
    /// </summary>
    /// <param name="type">Round type.</param>
    /// <param name="name">Round name.</param>
    public Round CreateRound(string type, string? name)
    {
        var round = new Round
        {
            Name = name ?? $"{Rounds.Count + 1}{Resources.RoundTrailing}",
            Type = type
        };

        Rounds.Add(round);
        return round;
    }

    /// <summary>
    /// Reads data from XML reader.
    /// </summary>
    /// <param name="reader">XML reader.</param>
    public override void ReadXml(XmlReader reader)
    {
        Name = reader.GetAttribute("name") ?? "";

        var versionString = reader.GetAttribute("version");

        if (double.TryParse(versionString, out var version))
        {
            if (version > MaximumSupportedVersion)
            {
                throw new InvalidOperationException(string.Format(Resources.UnsupportedVersion, version, MaximumSupportedVersion));
            }

            _version = version;
        }

        if (reader.MoveToAttribute("id"))
        {
            _id = reader.Value;
        }

        if (reader.MoveToAttribute("restriction"))
        {
            _restriction = reader.Value;
        }

        if (reader.MoveToAttribute("date"))
        {
            _date = reader.Value;
        }

        if (reader.MoveToAttribute("publisher"))
        {
            _publisher = reader.Value;
        }

        if (reader.MoveToAttribute("contactUri"))
        {
            _contactUri = reader.Value;
        }

        if (reader.MoveToAttribute("difficulty"))
        {
            _ = int.TryParse(reader.Value, out _difficulty);
        }

        if (reader.MoveToAttribute("logo"))
        {
            _logo = reader.Value;
        }

        if (reader.MoveToAttribute("language"))
        {
            _language = reader.Value;
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
                        case "tag":
                            Tags.Add(reader.ReadElementContentAsString());
                            read = false;
                            break;

                        case "info":
                            base.ReadXml(reader);
                            read = false;
                            break;

                        case "global":
                            Global = new GlobalData();
                            Global.ReadXml(reader);
                            break;

                        case "round":
                            var round = new Round();
                            round.ReadXml(reader);
                            Rounds.Add(round);
                            read = false;
                            break;
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Writes data to XML writer.
    /// </summary>
    /// <param name="writer">XML writer.</param>
    public override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("package", "http://vladimirkhil.com/ygpackage3.0.xsd");
        writer.WriteAttributeString("name", Name);
        writer.WriteAttributeString("version", Math.Max(DefaultVersion, Version).ToString());

        if (!string.IsNullOrEmpty(_id))
        {
            writer.WriteAttributeString("id", _id);
        }

        if (!string.IsNullOrEmpty(_restriction))
        {
            writer.WriteAttributeString("restriction", _restriction);
        }

        if (!string.IsNullOrEmpty(_date))
        {
            writer.WriteAttributeString("date", _date);
        }

        if (!string.IsNullOrEmpty(_publisher))
        {
            writer.WriteAttributeString("publisher", _publisher);
        }

        if (!string.IsNullOrEmpty(_contactUri))
        {
            writer.WriteAttributeString("contactUri", _contactUri);
        }

        if (_difficulty > 0)
        {
            writer.WriteAttributeString("difficulty", _difficulty.ToString());
        }

        if (!string.IsNullOrEmpty(_logo))
        {
            writer.WriteAttributeString("logo", _logo);
        }

        if (!string.IsNullOrEmpty(_language))
        {
            writer.WriteAttributeString("language", _language);
        }
        
        if (Tags.Count > 0)
        {
            writer.WriteStartElement("tags");

            foreach (var item in Tags)
            {
                writer.WriteElementString("tag", item);
            }

            writer.WriteEndElement();
        }

        if (Global.Authors.Count > 0 || Global.Sources.Count > 0)
        {
            writer.WriteStartElement("global");
            Global.WriteXml(writer);
            writer.WriteEndElement();
        }

        base.WriteXml(writer);

        if (Rounds.Any())
        {
            writer.WriteStartElement("rounds");

            foreach (var item in Rounds)
            {
                item.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Creates a copy of this object.
    /// </summary>
    public Package Clone()
    {
        var package = new Package
        {
            Name = Name,
            _date = _date,
            _restriction = _restriction,
            _publisher = _publisher,
            _difficulty = _difficulty,
            _logo = _logo
        };

        package.Tags.AddRange(Tags);

        package.SetInfoFromOwner(this);

        foreach (var round in Rounds)
        {
            package.Rounds.Add(round.Clone());
        }

        return package;
    }

    /// <inheritdoc />
    public bool Equals(Package? other) =>
        other is not null
        && base.Equals(other)
        && ID.Equals(other.ID)
        && Date.Equals(other.Date)
        && Language.Equals(other.Language)
        && Version.Equals(other.Version)
        && Restriction.Equals(other.Restriction)
        && Publisher.Equals(other.Publisher)
        && ContactUri.Equals(other.ContactUri)
        && Difficulty.Equals(other.Difficulty)
        && Logo.Equals(other.Logo)
        && Tags.SequenceEqual(other.Tags)
        && Rounds.SequenceEqual(other.Rounds);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Package);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        HashCode.Combine(base.GetHashCode(), ID, Date, Language, Version, Restriction, Publisher, ContactUri),
        Difficulty,
        Logo,
        Tags.GetCollectionHashCode(),
        Rounds.GetCollectionHashCode());
}
