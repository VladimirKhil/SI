using SIPackages.Core;
using SIPackages.Helpers;

namespace SIPackages;

/// <summary>
/// Defines a question scenario.
/// </summary>
/// <inheritdoc cref="List{T}" />
public sealed class Scenario : List<Atom>, IEquatable<Scenario>
{
    /// <inheritdoc />
    public override string ToString() => string.Join(Environment.NewLine, this.Select(atom => atom.ToString()).ToArray());

    /// <summary>
    /// Does the atom text contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public bool ContainsQuery(string value) => this.Any(item => item.Contains(value));

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public IEnumerable<SearchData> Search(string value)
    {
        for (var i = 0; i < Count; i++)
        {
            foreach (var item in this[i].Search(value))
            {
                item.ItemIndex = i;
                yield return item;
            }
        }
    }

    /// <summary>
    /// Adds new atom to this scenario.
    /// </summary>
    /// <param name="type">Atom type.</param>
    /// <param name="text">Atom value.</param>
    /// <param name="time">Atom time.</param>
    public Atom Add(string type, string text, int time = 0)
    {
        var atom = new Atom
        {
            Type = type,
            Text = text,
            AtomTime = time
        };

        Add(atom);
        return atom;
    }

    /// <summary>
    /// Adds new text atom to this scenario.
    /// </summary>
    /// <param name="text">Atom text.</param>
    public Atom Add(string text) => Add(AtomTypes.Text, text);

    /// <inheritdoc />
    public bool Equals(Scenario? other) => other is not null && this.SequenceEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Scenario);

    /// <inheritdoc />
    public override int GetHashCode() => this.GetCollectionHashCode();
}
