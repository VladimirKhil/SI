using SIPackages.Core;
using System.Runtime.Serialization;
using System.Text;

namespace SIPackages;

/// <summary>
/// Defines a package object author info.
/// </summary>
[DataContract]
[Serializable]
public sealed class AuthorInfo : IdOwner
{
    private string? _name;
    private string? _surname;
    private string? _secondName;
    private string? _country;
    private string? _city;

    /// <summary>
    /// Author's name.
    /// </summary>
    [DataMember]
    public string? Name
    {
        get => _name;
        set { var oldValue = _name; if (oldValue != value) { _name = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Author's surname.
    /// </summary>
    [DataMember]
    public string? Surname
    {
        get => _surname;
        set { var oldValue = _surname; if (oldValue != value) { _surname = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Author's second name.
    /// </summary>
    [DataMember]
    public string? SecondName
    {
        get => _secondName;
        set { var oldValue = _secondName; if (oldValue != value) { _secondName = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Author's country.
    /// </summary>
    [DataMember]
    public string? Country
    {
        get => _country;
        set { var oldValue = _country; if (oldValue != value) { _country = value; OnPropertyChanged(oldValue); } }
    }

    /// <summary>
    /// Author's city.
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

        result.Append(_name);

        if (!string.IsNullOrEmpty(_secondName))
        {
            result.Append(' ');
            result.Append(_secondName);
        }

        if (!string.IsNullOrEmpty(_surname))
        {
            result.Append(' ');
            result.Append(_surname);
        }

        if (!string.IsNullOrEmpty(_city) || !string.IsNullOrEmpty(_country))
        {
            result.Append(" (");

            if (!string.IsNullOrEmpty(_city))
            {
                result.Append(_city);

                if (!string.IsNullOrEmpty(_country))
                {
                    result.Append(", ");
                    result.Append(_country);
                }
                else
                {
                    result.Append(_country);
                }
            }

            result.Append(')');
        }

        return result.ToString();
    }

    /// <summary>
    /// Creates a copy of this object.
    /// </summary>
    public AuthorInfo Clone() => new()
    {
        _city = _city,
        _country = _country,
        _name = _name,
        _secondName = _secondName,
        _surname = _surname,
        Id = Id
    };
}
