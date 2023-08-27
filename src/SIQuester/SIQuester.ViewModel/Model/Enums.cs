using System.ComponentModel;

namespace SIQuester.Model;

/// <summary>
/// Defines well-known packge export formats.
/// </summary>
public enum ExportFormats
{
    [Description("ExportFormatsDinabank")]
    Dinabank,
    [Description("ExportFormatsTvSI")]
    TvSI,
    [Description("ExportFormatsSns")]
    Sns,
    [Description("ExportFormatsDb")]
    Db
}

public enum Orientation
{
    [Description("OrientationVertical")]
    Vertical,
    [Description("OrientationHorizontal")]
    Horizontal
}

/// <summary>
/// Defines document view modes.
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// Tree view mode.
    /// </summary>
    [Description("ViewModeTree")]
    TreeFull,
    /// <summary>
    /// Flat view mode.
    /// </summary>
    [Description("ViewModeFlat")]
    Flat
}

/// <summary>
/// Defines document layout modes in the flat view.
/// </summary>
public enum FlatLayoutMode
{
    /// <summary>
    /// Table layout.
    /// </summary>
    [Description("FlatLayoutModeTable")]
    Table,
    /// <summary>
    /// List layout.
    /// </summary>
    [Description("FlatLayoutModeList")]
    List
}

/// <summary>
/// Режим редактирования
/// </summary>
public enum EditMode
{
    [Description("EditModeNone")]
    None,
    [Description("EditModeFixedPanel")]
    FixedPanel,
    [Description("EditModeFloatPanel")]
    FloatPanel
}

/// <summary>
/// Масштаб плиточного представления
/// </summary>
public enum FlatScale
{
    [Description("Package")]
    Package,
    [Description("Round")]
    Round,
    [Description("Theme")]
    Theme,
    [Description("Question")]
    Question
}
