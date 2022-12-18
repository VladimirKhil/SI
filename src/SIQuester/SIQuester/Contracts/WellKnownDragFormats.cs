using System.Windows;

namespace SIQuester.Contracts;

/// <summary>
/// Defines well-known format names used in drag&drop operations.
/// </summary>
internal static class WellKnownDragFormats
{
    internal static string FileName = nameof(FileName);

    internal static string FileContents = nameof(FileContents);

    internal static string Round = "siqround";

    internal static string Theme = "siqtheme";

    internal static string Question = "siqquestion";

    internal static string? GetDragFormat(DragEventArgs e) =>
        e.Data.GetFormats(false).FirstOrDefault(f => f.StartsWith("siq"));
}
