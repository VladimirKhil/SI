using SICore.Contracts;
using SICore.Models;

namespace SImulator.ViewModel.Services;

internal sealed class AvatarHelper : IAvatarHelper
{
    public void AddFile(string sourceFilePath, string fileName)
    {
        throw new NotImplementedException();
    }

    public (ErrorCode, string)? ExtractAvatarData(string base64data, string fileName)
    {
        throw new NotImplementedException();
    }

    public bool FileExists(string fileName)
    {
        throw new NotImplementedException();
    }
}
