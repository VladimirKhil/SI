using System.IO;

namespace SICore.PlatformSpecific
{
    public interface IPlatformManager
    {
        Stream CreateLog(string userName, out string logUri);

        string CreateTempFile(string name, byte[] data);
        void ClearTempFile();
    }
}
