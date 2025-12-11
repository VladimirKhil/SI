using SIPackages.Core;

namespace SIPackages.Tests;

/// <summary>
/// Tests for advanced question types and NumberSet parameters.
/// </summary>
public sealed class AdvancedQuestionTypesTests
{
    [Test]
    public void LoadXml_SecretQuestion_WithNumberSet_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[0];
        
        Assert.Multiple(() =>
        {
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.Secret));
            Assert.That(question.Parameters.ContainsKey(QuestionParameterNames.Theme), Is.True);
            Assert.That(question.Parameters.ContainsKey(QuestionParameterNames.Price), Is.True);
        });

        var themeParam = question.Parameters[QuestionParameterNames.Theme];
        Assert.That(themeParam.SimpleValue, Is.EqualTo("Secret Theme"));

        var priceParam = question.Parameters[QuestionParameterNames.Price];
        Assert.That(priceParam.Type, Is.EqualTo(StepParameterTypes.NumberSet));
        Assert.That(priceParam.NumberSetValue, Is.Not.Null);
    }

    [Test]
    public void LoadXml_NumberSet_PropertiesAreDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[0];
        var numberSet = question.Parameters[QuestionParameterNames.Price].NumberSetValue!;
        
        Assert.Multiple(() =>
        {
            Assert.That(numberSet.Minimum, Is.EqualTo(100));
            Assert.That(numberSet.Maximum, Is.EqualTo(500));
            Assert.That(numberSet.Step, Is.EqualTo(100));
        });
    }

    [Test]
    public void LoadXml_SecretPublicPrice_TypeIsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[1];
        Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.SecretPublicPrice));
    }

    [Test]
    public void LoadXml_SecretNoQuestion_TypeIsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[0].Questions[2];
        Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.SecretNoQuestion));
    }

    [Test]
    public void LoadXml_NoRiskQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[1].Questions[0];
        
        Assert.Multiple(() =>
        {
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.NoRisk));
            Assert.That(question.Price, Is.EqualTo(100));
            Assert.That(question.Right[0], Is.EqualTo("NoRisk Answer"));
        });
    }

    [Test]
    public void LoadXml_StakeAllQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[1].Questions[1];
        
        Assert.Multiple(() =>
        {
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.StakeAll));
            Assert.That(question.Price, Is.EqualTo(200));
            Assert.That(question.Right[0], Is.EqualTo("StakeAll Answer"));
        });
    }

    [Test]
    public void LoadXml_ForAllQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question = document.Package.Rounds[0].Themes[1].Questions[2];
        
        Assert.Multiple(() =>
        {
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.ForAll));
            Assert.That(question.Price, Is.EqualTo(300));
            Assert.That(question.Right[0], Is.EqualTo("ForAll Answer"));
        });
    }

    [Test]
    public void LoadXml_SelectionMode_IsDeserialized()
    {
        // Arrange & Act
        using var xmlStream = File.OpenRead("AdvancedTypesTest.xml");
        using var document = SIDocument.LoadXml(xmlStream);

        // Assert
        var question1 = document.Package.Rounds[0].Themes[0].Questions[0];
        Assert.That(question1.Parameters[QuestionParameterNames.SelectionMode].SimpleValue, Is.EqualTo("any"));

        var question2 = document.Package.Rounds[0].Themes[0].Questions[1];
        Assert.That(question2.Parameters[QuestionParameterNames.SelectionMode].SimpleValue, Is.EqualTo("exceptCurrent"));

        var question3 = document.Package.Rounds[0].Themes[0].Questions[2];
        Assert.That(question3.Parameters[QuestionParameterNames.SelectionMode].SimpleValue, Is.EqualTo("current"));
    }
}
