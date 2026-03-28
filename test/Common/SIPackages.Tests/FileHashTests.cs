using System.Security.Cryptography;

namespace SIPackages.Tests;

public sealed class FileHashTests
{
    [Test]
    public async Task Save_ComputesAndPersistsFileHashes()
    {
        var stream = new MemoryStream();
        var data = new byte[] { 1, 2, 3, 4 };
        var expectedHash = Convert.ToHexString(SHA256.HashData(data));

        using (var document = SIDocument.Create("Test Package", "Test Author", stream, true))
        {
            using var fileStream = new MemoryStream(data);
            await document.Images.AddFileAsync("test.png", fileStream);

            Assert.That(document.FileHashes, Is.Empty);

            document.Save();

            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out var hash), Is.True);
            Assert.That(hash, Is.EqualTo(expectedHash));
        }

        stream.Position = 0;

        using var loaded = SIDocument.Load(stream);
        Assert.That(loaded.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out var loadedHash), Is.True);
        Assert.That(loadedHash, Is.EqualTo(expectedHash));
    }

    [Test]
    public async Task RemoveFile_RemovesHash()
    {
        var stream = new MemoryStream();

        using (var document = SIDocument.Create("Test Package", "Test Author", stream, true))
        {
            using (var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 }))
            {
                await document.Images.AddFileAsync("test.png", fileStream);
            }

            document.Save();
            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out _), Is.True);

            document.Images.RemoveFile("test.png");

            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out _), Is.False);

            document.Save();
        }

        stream.Position = 0;

        using var loaded = SIDocument.Load(stream);
        Assert.That(loaded.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out _), Is.False);
    }

    [Test]
    public async Task RenameFile_MovesHashToNewName()
    {
        var stream = new MemoryStream();

        using (var document = SIDocument.Create("Test Package", "Test Author", stream, true))
        {
            using (var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 }))
            {
                await document.Images.AddFileAsync("test.png", fileStream);
            }

            document.Save();
            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out var oldHash), Is.True);

            await document.Images.RenameFileAsync("test.png", "renamed.png");

            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out _), Is.False);
            Assert.That(document.TryGetFileHash(CollectionNames.ImagesStorageName, "renamed.png", out var newHash), Is.True);
            Assert.That(newHash, Is.EqualTo(oldHash));

            document.Save();
        }

        stream.Position = 0;

        using var loaded = SIDocument.Load(stream);
        Assert.That(loaded.TryGetFileHash(CollectionNames.ImagesStorageName, "test.png", out _), Is.False);
        Assert.That(loaded.TryGetFileHash(CollectionNames.ImagesStorageName, "renamed.png", out _), Is.True);
    }
}
