using System.IO.Compression;

namespace ZipUtils;

// TODO: Update when moving to .NET vNext

/// <remarks>
/// Patches <see cref="ZipFileExtensions" />
/// so it is using <see cref="MaxLengthStream" /> and limits maximum extracted file length (protecting from zip bombs).
/// </remarks>
/// <see href="https://github.com/dotnet/runtime/blob/release/6.0/src/libraries/System.IO.Compression.ZipFile/src/System/IO/Compression/ZipFileExtensions.ZipArchiveEntry.Extract.cs" />
internal static class ZipFileExtensionsPatched
{
    internal static async Task ExtractToFileAsync(
        ZipArchiveEntry source,
        string destinationFileName,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationFileName);

        // Rely on FileStream's ctor for further checking destinationFileName parameter
        var fMode = overwrite ? FileMode.Create : FileMode.CreateNew;

        using (var fs = new FileStream(destinationFileName, fMode, FileAccess.Write, FileShare.None, bufferSize: 0x1000, useAsync: false))
        {
            // Use MaxLengthStream to ensure we don't read more than the declared length
            using (Stream es = source.Open())
            using (var maxLengthStream = new MaxLengthStream(es, source.Length)) // This line is added to the original code
            {
                await maxLengthStream.CopyToAsync(fs, cancellationToken);
            }

            ExtractExternalAttributes(fs, source);
        }

        try
        {
            File.SetLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
        }
        catch
        {
            // some OSes like Android (#35374) might not support setting the last write time, the extraction should not fail because of that
        }
    }

    private static void ExtractExternalAttributes(FileStream fs, ZipArchiveEntry entry)
    {
        // Only extract USR, GRP, and OTH file permissions, and ignore
        // S_ISUID, S_ISGID, and S_ISVTX bits. This matches unzip's default behavior.
        // It is off by default because of this comment:

        // "It's possible that a file in an archive could have one of these bits set
        // and, unknown to the person unzipping, could allow others to execute the
        // file as the user or group. The new option -K bypasses this check."
        const int ExtractPermissionMask = 0x1FF;
        int permissions = (entry.ExternalAttributes >> 16) & ExtractPermissionMask;

        // If the permissions weren't set at all, don't write the file's permissions,
        // since the .zip could have been made using a previous version of .NET, which didn't
        // include the permissions, or was made on Windows.
        if (permissions != 0)
        {
            // TODO: Interop is internal in .NET runtime
            // Try to find a public way of settings file permissions

            // Interop.CheckIo(Interop.Sys.FChMod(fs.SafeFileHandle, permissions), fs.Name);
        }
    }
}
