
namespace SIUI.ViewModel;

/// <summary>
/// Defines a content group.
/// </summary>
public sealed record ContentGroup
{
    /// <summary>
    /// Group weight.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Group content.
    /// </summary>
    public List<ContentViewModel> Content { get; } = new();

    /// <summary>
    /// Group row count.
    /// </summary>
    public int RowCount { get; private set; } = 1;

    public void Init()
    {
        var bestRowCount = 1;
        var bestItemSize = Math.Min(9.0, 16.0 / Content.Count); // we are optimizing for 16 * 9 layout

        for (var rowCount = 2; rowCount <= Content.Count; rowCount++)
        {
            var itemsPerRow = (int)Math.Ceiling((double)Content.Count / rowCount);
            var itemSize = Math.Min(9.0 / rowCount, 16.0 / itemsPerRow);

            if (itemSize > bestItemSize)
            {
                bestItemSize = itemSize;
                bestRowCount = rowCount;
            }
        }

        RowCount = bestRowCount;
        Weight *= RowCount;
    }
}
