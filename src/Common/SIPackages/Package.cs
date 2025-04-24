using SIPackages.Core;
using SIPackages.Exceptions;
using SIPackages.Helpers;
using SIPackages.Models;
using SIPackages.Serializers;
using System.ComponentModel;
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
    public const double DefaultVersion = 5.0;

    /// <summary>
    /// Maximum supported package version.
    /// </summary>
    public const double MaximumSupportedVersion = 5.0;

    /// <summary>
    /// Package version.
    /// </summary>
    public double Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Unique package identifier.
    /// </summary>
    public string ID { get; set; } = "";

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
                return new ContentItem { IsRef = false, Value = Logo, Type = ContentTypes.Image };
            }

            return new ContentItem { IsRef = true, Value = link, Type = ContentTypes.Image };
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
    /// Does this package have additional quality control:
    /// - maximum size: 150 MB;
    /// - limited media files size and extensions;
    /// - forbidden external links.
    /// </summary>
    public bool HasQualityControl { get; set; }

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
    public override string ToString() => Name;

    /// <inheritdoc />
    public override void ReadXml(XmlReader reader, PackageLimits? limits = null)
    {
        Name = (reader.GetAttribute("name") ?? "").LimitLengthBy(limits?.TextLength);

        var versionString = reader.GetAttribute("version");

        if (double.TryParse(versionString, out var version))
        {
            if (version > MaximumSupportedVersion)
            {
                throw new UnsupportedPackageVersionException(version, MaximumSupportedVersion);
            }

            Version = version;
        }

        if (reader.MoveToAttribute("id"))
        {
            ID = reader.Value;
        }

        if (reader.MoveToAttribute("restriction"))
        {
            Restriction = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        if (reader.MoveToAttribute("date"))
        {
            Date = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        if (reader.MoveToAttribute("publisher"))
        {
            Publisher = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        if (reader.MoveToAttribute("contactUri"))
        {
            ContactUri = reader.Value.LimitLengthBy(limits?.TextLength);
        }

        if (reader.MoveToAttribute("difficulty"))
        {
            _ = int.TryParse(reader.Value, out _difficulty);
        }

        if (reader.MoveToAttribute("logo"))
        {
            Logo = reader.Value;
        }

        if (reader.MoveToAttribute("language"))
        {
            Language = reader.Value.LimitLengthBy(limits?.TextLength);
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
                            if (limits == null || Tags.Count < limits.CollectionCount)
                            {
                                Tags.Add(reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength));
                                read = false;
                            }
                            break;

                        case "info":
                            Info.ReadXml(reader, limits);
                            read = false;
                            break;

                        case "global":
                            Global = new GlobalData();
                            Global.ReadXml(reader, limits);
                            break;

                        case "round":
                            if (limits == null || Rounds.Count < limits.RoundCount)
                            {
                                var round = new Round();
                                round.ReadXml(reader, limits);
                                Rounds.Add(round);
                            }
                            else
                            {
                                reader.Skip();
                            }

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
        writer.WriteStartElement("package", "https://github.com/VladimirKhil/SI/blob/master/assets/siq_5.xsd");
        writer.WriteAttributeString("name", Name);
        writer.WriteAttributeString("version", Math.Max(DefaultVersion, Version).ToString());

        if (!string.IsNullOrEmpty(ID))
        {
            writer.WriteAttributeString("id", ID);
        }

        if (!string.IsNullOrEmpty(Restriction))
        {
            writer.WriteAttributeString("restriction", Restriction);
        }

        if (!string.IsNullOrEmpty(Date))
        {
            writer.WriteAttributeString("date", Date);
        }

        if (!string.IsNullOrEmpty(Publisher))
        {
            writer.WriteAttributeString("publisher", Publisher);
        }

        if (!string.IsNullOrEmpty(ContactUri))
        {
            writer.WriteAttributeString("contactUri", ContactUri);
        }

        if (Difficulty > 0)
        {
            writer.WriteAttributeString("difficulty", Difficulty.ToString());
        }

        if (!string.IsNullOrEmpty(Logo))
        {
            writer.WriteAttributeString("logo", Logo);
        }

        if (!string.IsNullOrEmpty(Language))
        {
            writer.WriteAttributeString("language", Language);
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

        Info.WriteXml(writer);

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
