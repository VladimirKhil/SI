using System.Text;

namespace SIQuester.ViewModel.PlatformSpecific;

public interface IFlowDocumentWrapper
{
    object GetDocument();

    void ExportXps(string filename);

    void ExportDocx(string filename);

    void WalkAndSave(
        string filename,
        Encoding encoding,
        Action<StreamWriter> onLineBreak,
        Action<StreamWriter, string> onText,
        Action<StreamWriter>? onHeader = null,
        Action<StreamWriter>? onFooter = null);

    bool Print();
}
