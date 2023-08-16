using SIPackages.Core;
using SIPackages.Helpers;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a collection of step parameters.
/// </summary>
public sealed class StepParameters : Dictionary<string, StepParameter>, IEquatable<StepParameters>, IXmlSerializable
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
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
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
                            var name = "";

                            if (reader.MoveToAttribute("name"))
                            {
                                name = reader.Value;
                            }

                            var parameter = new StepParameter();
                            parameter.ReadXml(reader);
                            this[name] = parameter;
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
