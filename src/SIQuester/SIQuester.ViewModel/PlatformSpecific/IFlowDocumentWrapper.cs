using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIQuester.ViewModel.PlatformSpecific
{
    public interface IFlowDocumentWrapper
    {
        object GetDocument();
        void ExportXps(string filename);
        void ExportDocx(string filename);
        void WalkAndSave(string filename, Encoding encoding, Action<StreamWriter> onLineBreak, Action<StreamWriter, string> onText, Action<StreamWriter> onHeader = null, Action<StreamWriter> onFooter = null);
        bool Print();
    }
}
