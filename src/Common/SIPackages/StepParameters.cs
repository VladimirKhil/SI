using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Models;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a collection of step parameters.
/// </summary>
public sealed class StepParameters : Dictionary<string, StepParameter>, IEquatable<StepParameters>
{
    /// <summary>
    /// Does any of parameters contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public bool ContainsQuery(string value) => Values.Any(parameter => parameter.ContainsQuery(value));

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public IEnumerable<SearchData> Search(string value)
    {
        foreach (var parameter in Values)
        {
            foreach (var item in parameter.Search(value))
            {
                yield return item;
            }
        }
    }

    /// <inheritdoc />
    public bool Equals(StepParameters? other) => other is not null && this.SequenceEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as StepParameters);

    /// <inheritdoc />
    public override int GetHashCode() => this.GetCollectionHashCode();

    /// <inheritdoc />
    public void ReadXml(XmlReader reader, PackageLimits? limits)
    {
        var read = true;
        var parentTagName = reader.LocalName;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "param":
                            if (!reader.IsEmptyElement && (limits == null || Count < limits.ParameterCount))
                            {
                                var name = "";

                                if (reader.MoveToAttribute("name"))
                                {
                                    name = reader.Value.LimitLengthBy(limits?.TextLength);
                                }

                                var parameter = new StepParameter();
                                parameter.ReadXml(reader, limits);
                                this[name] = parameter;
                            }
                            else
                            {
                                reader.Skip();
                            }

                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == parentTagName)
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        foreach (var item in this)
        {
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", item.Key);
            item.Value.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    internal StepParameters Clone()
    {
        var result = new StepParameters();

        foreach (var parameter in this)
        {
            result[parameter.Key] = parameter.Value.Clone();
        }

        return result;
    }
}
