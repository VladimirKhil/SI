namespace SIQuester.ViewModel.Helpers;

internal static class IndexLabelHelper
{
    internal static string GetIndexLabel(int index) => index < 26 ? ((char)('A' + index)).ToString() : 'A' + (index - 25).ToString();
}
