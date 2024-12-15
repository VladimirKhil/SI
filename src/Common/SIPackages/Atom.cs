using SIPackages.Core;
using System.ComponentModel;
using System.Diagnostics;

namespace SIPackages;

/// <summary>
/// Defines a question scenario minimal item.
/// </summary>
[Obsolete]
internal sealed class Atom
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _type = AtomTypes.Text;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _atomTime;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _text = "";

    /// <summary>
    /// Is this atom a link to a file.
    /// </summary>
    public bool IsLink => _text.Length > 0 && _text[0] == '@';

    /// <summary>
    /// Atom type.
    /// </summary>
    [DefaultValue(AtomTypes.Text)]
    public string Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
            }
        }
    }

    /// <summary>
    /// Atom duration in seconds.
    /// </summary>
    [DefaultValue(0)]
    public int AtomTime
    {
        get => _atomTime;
        set
        {
            if (_atomTime != value)
            {
                _atomTime = value;
            }
        }
    }

    /// <summary>
    /// Текст единицы
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
            }
        }
    }
}
