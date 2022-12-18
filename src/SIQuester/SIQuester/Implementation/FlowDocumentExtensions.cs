using System.Windows.Documents;

namespace SIQuester.Implementation;

internal static class FlowDocumentExtensions
{
    internal static Paragraph Append(this Paragraph paragraph, object value)
    {
        if (paragraph.Inlines.LastInline is Run run)
        {
            run.Text += value.ToString();
        }
        else
        {
            paragraph.Inlines.Add(value.ToString());
        }

        return paragraph;
    }

    internal static Paragraph AppendText(this Paragraph paragraph, string text)
    {
        if (paragraph.Inlines.LastInline is Run run)
        {
            run.Text += text;
        }
        else
        {
            paragraph.Inlines.Add(text);
        }

        return paragraph;
    }

    internal static Paragraph AppendLine(this Paragraph paragraph)
    {
        paragraph.Inlines.Add(new LineBreak());
        return paragraph;
    }

    internal static Paragraph AppendLine(this Paragraph paragraph, string text)
    {
        if (paragraph.Inlines.LastInline is Run run)
        {
            run.Text += text;
        }
        else
        {
            paragraph.Inlines.Add(text);
        }

        paragraph.Inlines.Add(new LineBreak());
        return paragraph;
    }

    internal static Paragraph AppendFormat(this Paragraph paragraph, string format, params object[] args)
    {
        if (paragraph.Inlines.LastInline is Run run)
        {
            run.Text += string.Format(format, args);
        }
        else
        {
            paragraph.Inlines.Add(string.Format(format, args));
        }

        return paragraph;
    }
}
