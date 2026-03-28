using System.Xml;

namespace SIPackages;

internal sealed class FileHashInfoList : List<FileHashInfo>
{
    internal static FileHashInfoList ReadXml(XmlReader reader)
    {
        var hashes = new FileHashInfoList();
        ReadXml(reader, hashes);
        return hashes;
    }

    internal static void ReadXml(XmlReader reader, ICollection<FileHashInfo> hashes)
    {
        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.LocalName == "file")
                    {
                        hashes.Add(new FileHashInfo
                        {
                            Name = reader["name"] ?? "",
                            Hash = reader["hash"] ?? ""
                        });
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "files")
                    {
                        reader.Read();
                        return;
                    }

                    break;
            }
        }
    }

    internal static void ReadXml(XmlReader reader, IDictionary<string, string> fileHashes)
    {
        var hashes = ReadXml(reader);

        foreach (var fileHash in hashes)
        {
            fileHashes[fileHash.Name] = fileHash.Hash;
        }
    }

    internal static void WriteXml(XmlWriter writer, IReadOnlyDictionary<string, string> fileHashes)
    {
        writer.WriteStartElement("files");

        foreach (var fileHash in fileHashes.OrderBy(fileHash => fileHash.Key, StringComparer.Ordinal))
        {
            writer.WriteStartElement("file");
            writer.WriteAttributeString("name", fileHash.Key);
            writer.WriteAttributeString("hash", fileHash.Value);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}

internal sealed class FileHashInfo
{
    public string Name { get; set; } = "";

    public string Hash { get; set; } = "";
}
