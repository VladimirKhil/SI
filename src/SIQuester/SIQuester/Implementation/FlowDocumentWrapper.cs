using SIQuester.ViewModel.PlatformSpecific;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using System.Xml;

namespace SIQuester.Implementation;

internal sealed class FlowDocumentWrapper : IFlowDocumentWrapper
{
    internal FlowDocument _document;

    public FlowDocumentWrapper(FlowDocument document) => _document = document;

    public object GetDocument() => _document;

    public void ExportXps(string filename)
    {
        using var package = System.IO.Packaging.Package.Open(filename, FileMode.Create);
        using var xpsDocument = new XpsDocument(package);
        using var manager = new XpsSerializationManager(new XpsPackagingPolicy(xpsDocument), false);
        
        var paginator = ((IDocumentPaginatorSource)_document).DocumentPaginator;
        manager.SaveAsXaml(paginator);
        manager.Commit();
    }

    public void ExportDocx(string filename)
    {
        var docxPackage = System.IO.Packaging.Package.Open(filename, FileMode.Create, FileAccess.ReadWrite);

        var docUri = System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri("word/document.xml", UriKind.Relative));
        var docPart = docxPackage.CreatePart(docUri, "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml");

        using (var docStream = new StreamWriter(docPart.GetStream(FileMode.Create, FileAccess.Write)))
        {
            var wNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using var writer = XmlWriter.Create(docStream);
            
            writer.WriteStartDocument();
            writer.WriteStartElement("document", wNamespace);
            writer.WriteStartElement("body", wNamespace);

            writer.WriteStartElement("p", wNamespace);
            writer.WriteStartElement("r", wNamespace);

            writer.WriteStartElement("rPr", wNamespace);

            writer.WriteStartElement("rFonts", wNamespace);
            writer.WriteAttributeString("ascii", wNamespace, "Times New Roman");
            writer.WriteAttributeString("hAnsi", wNamespace, "Times New Roman");
            writer.WriteEndElement();

            writer.WriteStartElement("sz", wNamespace);
            writer.WriteAttributeString("val", wNamespace, "24");
            writer.WriteEndElement();

            foreach (var paragraph in _document.Blocks.OfType<Paragraph>())
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is LineBreak)
                    {
                        writer.WriteElementString("br", wNamespace, string.Empty);
                    }
                    else
                    {
                        var text = ((Run)inline).Text;

                        writer.WriteStartElement("t", wNamespace);

                        if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1])))
                        {
                            writer.WriteAttributeString("xml", "space", null, "preserve");
                        }

                        writer.WriteValue(text);
                        writer.WriteEndElement();
                    }
                }
            }

            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        docxPackage.Flush();

        docxPackage.CreateRelationship(docUri, System.IO.Packaging.TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "rId1");
        docxPackage.Flush();
        docxPackage.Close();
    }

    public void WalkAndSave(string filename, Encoding encoding, Action<StreamWriter> onLineBreak, Action<StreamWriter, string> onText, Action<StreamWriter> onHeader = null, Action<StreamWriter> onFooter = null)
    {
        using var sr = new StreamWriter(filename, false, encoding);
        
        onHeader?.Invoke(sr);

        foreach (var paragraph in _document.Blocks.OfType<Paragraph>())
        {
            foreach (var inline in paragraph.Inlines)
            {
                if (inline is LineBreak)
                {
                    onLineBreak(sr);
                }
                else
                {
                    onText(sr, (inline as Run).Text);
                }
            }
        }

        onFooter?.Invoke(sr);
    }

    public bool Print()
    {
        var printDialog = new PrintDialog();

        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintDocument(((IDocumentPaginatorSource)_document).DocumentPaginator, "Идёт печать");
            return true;
        }

        return false;
    }
}
