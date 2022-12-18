using SIPackages.Core;
using System.Runtime.Serialization;
using System.Text;

namespace SIPackages;

/// <summary>
/// Defines an source info.
/// </summary>
[DataContract]
[Serializable]
public sealed class SourceInfo : IdOwner
{
    private string? _author;
    private string? _title;
    private int _year;
    private string? _publish;
    private string? _city;

    /// <summary>
    /// Автор источника
    /// </summary>
    [DataMember]
    public string? Author
    {
        get => _author;
        set { var oldValue = _author; if (oldValue != value) { _author = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Название источника
    /// </summary>
    [DataMember]
    public string? Title
    {
        get => _title;
        set { var oldValue = _title; if (oldValue != value) { _title = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Год издания
    /// </summary>
    [DataMember]
    public int Year
    {
        get { return _year; }
        set { var oldValue = _year; if (oldValue != value) { _year = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Издательство
    /// </summary>
    [DataMember]
    public string? Publish
    {
        get => _publish;
        set { var oldValue = _publish; if (oldValue != value) { _publish = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Город
    /// </summary>
    [DataMember]
    public string? City
    {
        get => _city;
        set { var oldValue = _city; if (oldValue != value) { _city = value; OnPropertyChanged(oldValue); } }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(_author))
        {
            result.Append(_author);
            result.Append(". ");
        }

        result.Append(_title ?? "");

        if (!string.IsNullOrEmpty(_city))
        {
            result.Append(".: ");
            result.Append(_city);
        }

        if (!string.IsNullOrEmpty(_publish))
        {
            result.Append(" - ");
            result.Append(_publish);
        }

        if (_year > 0)
        {
            result.Append(", ");
            result.Append(_year);
        }

        return result.ToString();
    }

    /// <summary>
    /// Creates a copy of source info.
    /// </summary>
    public SourceInfo Clone() => new() { _author = _author, _city = _city, _publish = _publish, _title = _title, _year = _year, Id = Id };
}
