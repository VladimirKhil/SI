namespace ZipUtils.Test;

public sealed class ZipExtractorTests
{
    [Test]
    public async Task ExtractArchiveFileToFolderAsync_Ok()
    {
        var targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var extractedFiles = await ZipExtractor.ExtractArchiveFileToFolderAsync("SIGameTest.siq", targetFolder);

            Assert.That(extractedFiles.Values.Count, Is.EqualTo(8));
            Assert.That(extractedFiles.Values, Contains.Item("content.xml"));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }

    [Test]
    public async Task ExtractArchiveFileToFolderAsync_WithFilterAndSelector_Ok()
    {
        var targetFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        static bool fileFilter(string filePath)
        {
            if (filePath == "content.xml")
            {
                return true;
            }

            var folderName = Path.GetDirectoryName(filePath);

            return folderName switch
            {
                "Images" or "Audio" or "Video" or "Texts" => true,
                _ => false
            };
        }

        try
        {
            var extractedFiles = await ZipExtractor.ExtractArchiveFileToFolderAsync(
                "SIGameTest.siq",
                targetFolder,
                new ExtractionOptions(name => name switch
                {
                    "content.xml" or "Texts/authors.xml" or "Texts/sources.xml" => UnzipNamingMode.KeepOriginal,
                    _ => UnzipNamingMode.Hash
                })
                {
                    FileFilter = fileFilter
                });

            Assert.That(extractedFiles.Count, Is.EqualTo(7));
            Assert.That(extractedFiles, Contains.Item("content.xml"));
            Assert.That(extractedFiles, Contains.Item(Path.Combine("Images", "549C3F433C5FBA8C.png")));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }
}