namespace SIPackages.Tests;

public sealed class SIDocumentTests
{
    [Test]
    public void TestMediaFileNames()
    {
        using var fs = File.OpenRead("test.siq");
        var document = SIDocument.Load(fs);

        var images = document.Images.ToArray();
        var video = document.Video.ToArray();

        var atomLinks = document.Package.Rounds[0].Themes[0].Questions
            .Select(q => document.GetLink(q.Scenario[0]).Uri)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(video[0], Is.EqualTo(atomLinks[0]));
            Assert.That(images[0], Is.EqualTo(atomLinks[1]));
        });
    }

    [Test]
    public void TestCollectionFiles()
    {
        using var fs = File.OpenRead("test.siq");
        var document = SIDocument.Load(fs);

        var images = document.Images.ToArray();
        var file = document.Images.GetFile(images[0]);

        Assert.That(file, Is.Not.Null);
    }
}