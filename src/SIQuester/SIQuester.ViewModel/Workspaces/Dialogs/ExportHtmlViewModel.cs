using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Windows.Input;
using System.Xml;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class ExportHtmlViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;

    public override string Header => $"{_document.Document.Package.Name}: {Resources.ExportToHtml}";

    public ICommand Export { get; private set; }

    public string DocumentFontFamily { get; set; } = "Arial";

    public bool ShowMetaTips { get; set; } = false;

    public string DocHeader { get; set; } = Resources.ExportToHtmlHeader;

    public int PackageFontSize { get; set; } = 24;

    public int RoundFontSize { get; set; } = 24;

    public int ThemeFontSize { get; set; } = 12;

    public string ThemeHeader { get; set; } = Resources.Theme + ": ";

    public bool EmptyStringAfterThemeName { get; set; } = false;

    public int QuestionFontSize { get; set; } = 12;

    public bool PriceNearText { get; set; } = false;

    public Orientation AnswerOrientation { get; set; } = Orientation.Horizontal;

    public Orientation AuthorsOrientation { get; set; } = Orientation.Horizontal;

    public Orientation SourcesOrientation { get; set; } = Orientation.Horizontal;

    public bool ThemeNumbers { get; set; } = false;

    public ExportHtmlViewModel(QDocument document)
    {
        _document = document;
        Export = new SimpleCommand(Export_Executed);
    }

    private void Export_Executed(object? arg)
    {
        try
        {
            var template = new XmlDocument();
            template.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ygpackagesimple3.0.xslt"));

            var manager = new XmlNamespaceManager(template.NameTable);
            manager.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            manager.AddNamespace("yg", "http://ur-quan1986.narod.ru/ygpackage3.0.xsd");
            var node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='/']/html", manager) ?? throw new Exception(Resources.ConversionError);

            node["head"]?["title"]?.InnerText = DocHeader;
            node["body"]?.Attributes["style"]?.Value = string.Format("background-color: #FFFFFF; font-family: {0}", DocumentFontFamily);

            if (!ShowMetaTips)
            {
                var nodeB = node["body"]?["b"];
                nodeB?.TryRemoveChild(nodeB["center"]);
                nodeB?.TryRemoveChild(nodeB["center"]);
                nodeB?.TryRemoveChild(nodeB["br"]);
            }

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:package']", manager) ?? throw new Exception(Resources.ConversionError);
            node["center"]?.Attributes["style"]?.Value = string.Format("font-size: {0}pt", PackageFontSize);

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:round']", manager) ?? throw new Exception(Resources.ConversionError);
            node["center"]?.Attributes["style"]?.Value = string.Format("font-size: {0}pt", RoundFontSize);

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:theme']", manager) ?? throw new Exception(Resources.ConversionError);
            node["span"]?.Attributes["style"]?.Value = string.Format("font-size: {0}pt", ThemeFontSize);

            if (ThemeHeader.Length > 0)
            {
                XmlText text = template.CreateTextNode(ThemeHeader);
                node["span"]?.PrependChild(text);
            }

            if (ThemeNumbers)
            {
                XmlText text = template.CreateTextNode(". ");
                node["span"]?.PrependChild(text);
                XmlNode nodeP = template.CreateElement("xsl", "value-of", "http://www.w3.org/1999/XSL/Transform");
                XmlAttribute attr = template.CreateAttribute("select");
                nodeP.Attributes?.Append(attr);
                nodeP.Attributes?["select"]?.Value = "position()";
                node["span"]?.PrependChild(nodeP);
            }

            if (EmptyStringAfterThemeName)
            {
                node.InsertAfter(template.CreateElement("br"), node["span"]);
            }

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:question']", manager) ?? throw new Exception(Resources.ConversionError);
            node["span"]?.Attributes["style"]?.Value = string.Format("font-size: {0}pt", QuestionFontSize);

            if (PriceNearText)
            {
                var nodeWhen = node["span"]?["xsl:choose"]?["xsl:when"];
                nodeWhen?.InsertBefore(template.CreateTextNode(". "), nodeWhen["br"]);
                if (nodeWhen?["br"] != null)
                {
                    nodeWhen.TryRemoveChild(nodeWhen["br"]);
                }
            }

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:answer']", manager) ?? throw new Exception(Resources.ConversionError);

            if (AnswerOrientation == Orientation.Vertical)
            {
                node.TryRemoveChild(node["xsl:choose"]);
                node.PrependChild(template.CreateElement("br"));
            }

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:author']", manager) ?? throw new Exception(Resources.ConversionError);

            if (AuthorsOrientation == Orientation.Vertical)
            {
                node.TryRemoveChild(node["xsl:choose"]);
                node.PrependChild(template.CreateElement("br"));
            }

            node = template.SelectSingleNode(@"/xsl:stylesheet/xsl:template[@match='yg:source']", manager) ?? throw new Exception(Resources.ConversionError);

            if (SourcesOrientation == Orientation.Vertical)
            {
                node.TryRemoveChild(node["xsl:choose"]);
                node.PrependChild(template.CreateElement("br"));
            }

            var path = Path.GetTempFileName();
            template.Save(path);

            try
            {
                _document.TransformPackage(path);
            }
            finally
            {
                File.Delete(path);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            OnClosed();
        }
    }
}

internal static class XmlNodeExtensions
{
    public static void TryRemoveChild(this XmlNode node, XmlNode? child)
    {
        if (child != null)
        {
            node.RemoveChild(child);
        }
    }
}
