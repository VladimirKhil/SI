using System.IO;

namespace SICore.PlatformSpecific
{
    public sealed class DesktopCoreManager: CoreManager
    {
        public override byte[] GetData(string filename)
        {
            if (!File.Exists(filename))
                return null;

            return File.ReadAllBytes(filename);
        }

        public override bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public override Stream GetFile(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
