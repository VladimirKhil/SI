using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIData;

/// <summary>
/// Defines a person account.
/// </summary>
[DataContract]
public class Account : INotifyPropertyChanged
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _name = "";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isMale = true;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _picture = "";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _canBeDeleted = false;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isHuman = false;

    /// <summary>
    /// Account name.
    /// </summary>
    [XmlAttribute]
    [DataMember]
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Account male flag (otherwise - woman).
    /// </summary>
    [XmlAttribute]
    [DefaultValue(true)]
    [DataMember]
    public bool IsMale
    {
        get => _isMale;
        set { _isMale = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Account avatar.
    /// </summary>
    [XmlAttribute]
    [DefaultValue("")]
    [DataMember]
    public virtual string Picture
    {
        get => _picture;
        set { if (_picture != value) { _picture = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Can the account be deleted (otherwise it a built-in account).
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    [DataMember]
    public bool CanBeDeleted
    {
        get => _canBeDeleted;
        set { if (_canBeDeleted != value) { _canBeDeleted = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// It this is a human account.
    /// </summary>
    [XmlIgnore]
    [DataMember]
    public bool IsHuman
    {
        get => _isHuman;
        set { if (_isHuman != value) { _isHuman = value; OnPropertyChanged(); } }
    }

    public Account() { }

    public Account(string name, bool isMale)
    {
        _name = name;
        _isMale = isMale;
    }

    public Account(Account account)
    {
        _name = account.Name;
        _isMale = account.IsMale;
        _picture = account.Picture;
        _isHuman = account._isHuman;
    }

    public override string ToString() => _name;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
