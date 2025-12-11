using SIPackages.Core;

namespace SIPackages.Tests;

/// <summary>
/// Tests for content item placement and timing attributes.
/// </summary>
public sealed class ContentPlacementTests
{
    [Test]
    public void LoadXml_ScreenPlacement_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[0];
        var contentItem = question.Parameters[QuestionParameterNames.Question].ContentValue![0];
        
        Assert.Multiple(() =>
        {
            Assert.That(contentItem.Placement, Is.EqualTo(ContentPlacements.Screen));
            Assert.That(contentItem.Value, Is.EqualTo("Text on screen"));
        });
    }

    [Test]
    public void LoadXml_BackgroundPlacement_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[1];
        var contentItem = question.Parameters[QuestionParameterNames.Question].ContentValue![0];
        
        Assert.Multiple(() =>
        {
            Assert.That(contentItem.Placement, Is.EqualTo(ContentPlacements.Background));
            Assert.That(contentItem.Value, Is.EqualTo("Background text"));
        });
    }

    [Test]
    public void LoadXml_ReplicPlacement_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[2];
        var contentItem = question.Parameters[QuestionParameterNames.Question].ContentValue![0];
        
        Assert.Multiple(() =>
        {
            Assert.That(contentItem.Placement, Is.EqualTo(ContentPlacements.Replic));
            Assert.That(contentItem.Value, Does.Contain("Replic text"));
        });
    }

    [Test]
    public void LoadXml_Duration_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[3];
        var contentItem = question.Parameters[QuestionParameterNames.Question].ContentValue![0];
        
        Assert.That(contentItem.Duration, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void LoadXml_WaitForFinish_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[4];
        var contentItems = question.Parameters[QuestionParameterNames.Question].ContentValue!;
        
        Assert.Multiple(() =>
        {
            Assert.That(contentItems[0].WaitForFinish, Is.True);
            Assert.That(contentItems[1].WaitForFinish, Is.False);
        });
    }

    [Test]
    public void LoadXml_AllContentAttributes_CanBeCombined()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("ContentPlacementTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert - Verify we can read all content attributes from all questions
        var theme = document.Package.Rounds[0].Themes[0];
        
        Assert.That(theme.Questions, Has.Count.EqualTo(5));
        
        foreach (var question in theme.Questions)
        {
            Assert.That(question.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
            Assert.That(question.Parameters[QuestionParameterNames.Question].ContentValue, Is.Not.Null);
            Assert.That(question.Parameters[QuestionParameterNames.Question].ContentValue, Has.Count.GreaterThan(0));
        }
    }
}
