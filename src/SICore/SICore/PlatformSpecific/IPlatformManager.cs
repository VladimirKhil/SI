namespace SICore.PlatformSpecific;

public interface IPlatformManager
{
    Stream CreateLog(string userName, out string logUri);
}
