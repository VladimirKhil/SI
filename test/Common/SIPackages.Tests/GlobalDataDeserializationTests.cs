namespace SIPackages.Tests;

/// <summary>
/// Tests for GlobalData, Authors and Sources deserialization from XML.
/// </summary>
public sealed class GlobalDataDeserializationTests
{
    [Test]
    public void LoadXml_GlobalData_AuthorsAreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        Assert.That(document.Authors, Has.Count.EqualTo(2));
        Assert.That(document.Authors[0].Name, Is.EqualTo("Author One"));
        Assert.That(document.Authors[1].Name, Is.EqualTo("Author Two"));
    }

    [Test]
    public void LoadXml_GlobalData_SourcesAreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        Assert.That(document.Sources, Has.Count.EqualTo(2));
        Assert.That(document.Sources[0].Title, Is.EqualTo("Source One"));
        Assert.That(document.Sources[1].Title, Is.EqualTo("Source Two"));
    }

    [Test]
    public void LoadXml_GlobalData_AuthorIds_AreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert - IDs come from the id attribute, not from inner <Id> element
        Assert.That(document.Authors[0].Id, Is.EqualTo("auth1"));
        Assert.That(document.Authors[1].Id, Is.EqualTo("auth2"));
    }

    [Test]
    public void LoadXml_GlobalData_SourceIds_AreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert - IDs come from the id attribute, not from inner <Id> element
        Assert.That(document.Sources[0].Id, Is.EqualTo("src1"));
        Assert.That(document.Sources[1].Id, Is.EqualTo("src2"));
    }

    [Test]
    public void LoadXml_PackageMetadata_AllFieldsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);
        var package = document.Package;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(package.Name, Is.EqualTo("Global Data Test"));
            Assert.That(package.Version, Is.EqualTo(5.0));
            Assert.That(package.Publisher, Is.EqualTo("Test Publisher"));
            Assert.That(package.Restriction, Is.EqualTo("18+"));
            Assert.That(package.Logo, Is.EqualTo("testlogo.png"));
            Assert.That(package.Language, Is.EqualTo("ru-RU"));
            Assert.That(package.ContactUri, Is.EqualTo("http://example.com"));
        });
    }

    [Test]
    public void LoadXml_PackageTags_AreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        Assert.That(document.Package.Tags, Has.Count.EqualTo(3));
        Assert.That(document.Package.Tags[0], Is.EqualTo("Test Tag 1"));
        Assert.That(document.Package.Tags[1], Is.EqualTo("Test Tag 2"));
        Assert.That(document.Package.Tags[2], Is.EqualTo("Test Tag 3"));
    }

    [Test]
    public void LoadXml_Version5_AuthorsSameAsGlobalAuthors()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert - In version 5.0+, Authors property should reference Package.Global.Authors
        Assert.That(document.Authors, Is.SameAs(document.Package.Global.Authors));
    }

    [Test]
    public void LoadXml_Version5_SourcesSameAsGlobalSources()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("GlobalDataTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert - In version 5.0+, Sources property should reference Package.Global.Sources
        Assert.That(document.Sources, Is.SameAs(document.Package.Global.Sources));
    }
}
