using System.Collections.ObjectModel;

namespace SIQuester.Model;

/// <summary>
/// Defines a list of files that have been opened before.
/// </summary>
public sealed class FileHistory
{
    private const int Capacity = 10;

    /// <summary>
    /// List of opened files paths.
    /// </summary>
    public ObservableCollection<string> Files { get; set; }

    public FileHistory() => Files = new ObservableCollection<string>();

    /// <summary>
    /// Adds new path to the history.
    /// </summary>
    /// <param name="path">File path to add.</param>
    public void Add(string path)
    {
        var index = Files.IndexOf(path);

        if (index > -1)
        {
            Files.Move(index, 0);
            return;
        }

        if (Files.Count == Capacity)
        {
            Files.RemoveAt(Capacity - 1);
        }

        Files.Insert(0, path);
    }

    public void Remove(string path) => Files.Remove(path);
}
