using SIPackages.Core;

namespace SIPackages.Tests;

/// <summary>
/// Tests for Question deserialization from .siq files with various content types.
/// </summary>
public sealed class QuestionDeserializationTests
{
    [Test]
    public void Load_OldFormat_SimpleTextQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var question = document.Package.Rounds[0].Themes[0].Questions[0];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(question.Price, Is.EqualTo(100));
            Assert.That(question.Right, Has.Count.EqualTo(1));
            Assert.That(question.Right[0], Is.EqualTo("А это правильный ответ"));
        });
    }

    [Test]
    public void Load_NewFormat_SimpleTextQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var question = document.Package.Rounds[0].Themes[1].Questions[0];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(question.Price, Is.EqualTo(100));
            Assert.That(question.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
            Assert.That(question.Right, Has.Count.EqualTo(1));
            Assert.That(question.Right[0], Is.EqualTo("А это обычный ответ"));
        });
    }

    [Test]
    public void Load_OldFormat_ImageQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var imageQuestion = contentTheme.Questions[2]; // Price 300

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(imageQuestion.Price, Is.EqualTo(300));
            Assert.That(imageQuestion.Right[0], Is.EqualTo("Изображение"));
        });

        var content = imageQuestion.GetContent().ToList();
        Assert.That(content, Has.Count.GreaterThan(0));
        
        var imageContent = content.FirstOrDefault(c => c.Type == ContentTypes.Image);
        Assert.That(imageContent, Is.Not.Null);
        Assert.That(imageContent.Value, Does.Contain("sample-boat-400x300.png"));
    }

    [Test]
    public void Load_NewFormat_ImageQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var imageQuestion = contentTheme.Questions[2]; // Price 300

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(imageQuestion.Price, Is.EqualTo(300));
            Assert.That(imageQuestion.Right[0], Is.EqualTo("Изображение"));
            Assert.That(imageQuestion.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
        });

        var questionParam = imageQuestion.Parameters[QuestionParameterNames.Question];
        Assert.That(questionParam.ContentValue, Is.Not.Null);
        Assert.That(questionParam.ContentValue, Has.Count.GreaterThan(0));

        var imageContent = questionParam.ContentValue.FirstOrDefault(c => c.Type == ContentTypes.Image);
        Assert.That(imageContent, Is.Not.Null);
        Assert.That(imageContent.Value, Is.EqualTo("sample-boat-400x300.png"));
        Assert.That(imageContent.IsRef, Is.True);
    }

    [Test]
    public void Load_OldFormat_AudioQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var audioQuestion = contentTheme.Questions[3]; // Price 400

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(audioQuestion.Price, Is.EqualTo(400));
            Assert.That(audioQuestion.Right[0], Is.EqualTo("Аудио"));
        });

        var content = audioQuestion.GetContent().ToList();
        var audioContent = content.FirstOrDefault(c => c.Type == ContentTypes.Audio);
        Assert.That(audioContent, Is.Not.Null);
        Assert.That(audioContent.Value, Does.Contain("sample-3s.mp3"));
    }

    [Test]
    public void Load_NewFormat_AudioQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var audioQuestion = contentTheme.Questions[3]; // Price 400

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(audioQuestion.Price, Is.EqualTo(400));
            Assert.That(audioQuestion.Right[0], Is.EqualTo("Аудио"));
            Assert.That(audioQuestion.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
        });

        var questionParam = audioQuestion.Parameters[QuestionParameterNames.Question];
        var audioContent = questionParam.ContentValue?.FirstOrDefault(c => c.Type == ContentTypes.Audio);
        Assert.That(audioContent, Is.Not.Null);
        Assert.That(audioContent.Value, Is.EqualTo("sample-3s.mp3"));
        Assert.That(audioContent.IsRef, Is.True);
        Assert.That(audioContent.Placement, Is.EqualTo(ContentPlacements.Background));
    }

    [Test]
    public void Load_OldFormat_VideoQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var videoQuestion = contentTheme.Questions[4]; // Price 500

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(videoQuestion.Price, Is.EqualTo(500));
            Assert.That(videoQuestion.Right[0], Is.EqualTo("Видео"));
        });

        var content = videoQuestion.GetContent().ToList();
        var videoContent = content.FirstOrDefault(c => c.Type == ContentTypes.Video);
        Assert.That(videoContent, Is.Not.Null);
        Assert.That(videoContent.Value, Does.Contain("Big_Buck_Bunny"));
    }

    [Test]
    public void Load_NewFormat_VideoQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var videoQuestion = contentTheme.Questions[4]; // Price 500

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(videoQuestion.Price, Is.EqualTo(500));
            Assert.That(videoQuestion.Right[0], Is.EqualTo("Видео"));
            Assert.That(videoQuestion.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
        });

        var questionParam = videoQuestion.Parameters[QuestionParameterNames.Question];
        var videoContent = questionParam.ContentValue?.FirstOrDefault(c => c.Type == ContentTypes.Video);
        Assert.That(videoContent, Is.Not.Null);
        Assert.That(videoContent.Value, Does.Contain("Big_Buck_Bunny"));
        Assert.That(videoContent.IsRef, Is.True);
    }

    [Test]
    public void Load_NewFormat_HtmlQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var htmlQuestion = contentTheme.Questions[13]; // Price 1500 - last question

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(htmlQuestion.Price, Is.EqualTo(1500));
            Assert.That(htmlQuestion.Right[0], Is.EqualTo("Ответ"));
            Assert.That(htmlQuestion.Parameters.ContainsKey(QuestionParameterNames.Question), Is.True);
        });

        var questionParam = htmlQuestion.Parameters[QuestionParameterNames.Question];
        var htmlContent = questionParam.ContentValue?.FirstOrDefault(c => c.Type == ContentTypes.Html);
        Assert.That(htmlContent, Is.Not.Null);
        Assert.That(htmlContent.Value, Is.EqualTo("test.html"));
        Assert.That(htmlContent.IsRef, Is.True);
    }

    [Test]
    public void Load_NewFormat_MultipleImagesQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var multiImageQuestion = contentTheme.Questions[10]; // Price 1200

        // Assert
        Assert.That(multiImageQuestion.Price, Is.EqualTo(1200));

        var questionParam = multiImageQuestion.Parameters[QuestionParameterNames.Question];
        var imageContents = questionParam.ContentValue?.Where(c => c.Type == ContentTypes.Image).ToList();
        
        Assert.That(imageContents, Has.Count.EqualTo(4));
        Assert.That(imageContents![0].WaitForFinish, Is.False);
        Assert.That(imageContents[1].WaitForFinish, Is.False);
        Assert.That(imageContents[2].WaitForFinish, Is.False);
        Assert.That(imageContents[3].WaitForFinish, Is.True); // Last image should wait
    }

    [Test]
    public void Load_OldFormat_QuestionWithAnswer_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var questionWithAnswer = contentTheme.Questions[8]; // Price 900

        // Assert
        Assert.That(questionWithAnswer.Price, Is.EqualTo(900));
        Assert.That(questionWithAnswer.Right[0], Does.Contain("Текстовый ответ"));

        var content = questionWithAnswer.GetContent().ToList();
        var answerImage = content.FirstOrDefault(c => c.Type == ContentTypes.Image);
        Assert.That(answerImage, Is.Not.Null);
    }

    [Test]
    public void Load_NewFormat_QuestionWithImageAnswer_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var questionWithAnswer = contentTheme.Questions[7]; // Price 900

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(questionWithAnswer.Price, Is.EqualTo(900));
            Assert.That(questionWithAnswer.Right[0], Does.Contain("Текстовый ответ"));
            Assert.That(questionWithAnswer.Parameters.ContainsKey(QuestionParameterNames.Answer), Is.True);
        });

        var answerParam = questionWithAnswer.Parameters[QuestionParameterNames.Answer];
        Assert.That(answerParam.Type, Is.EqualTo(StepParameterTypes.Content));
        
        var imageContent = answerParam.ContentValue?.FirstOrDefault(c => c.Type == ContentTypes.Image);
        Assert.That(imageContent, Is.Not.Null);
        Assert.That(imageContent.Value, Is.EqualTo("sample-boat-400x300.png"));
    }

    [Test]
    public void Load_OldFormat_AuctionQuestion_TypeIsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTest.siq");
        using var document = SIDocument.Load(fs);
        var question = document.Package.Rounds[0].Themes[0].Questions[1]; // Price 200

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(question.Price, Is.EqualTo(200));
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.Stake));
            Assert.That(question.Right, Has.Count.GreaterThan(0));
            Assert.That(question.Wrong, Has.Count.GreaterThan(0));
        });
    }

    [Test]
    public void Load_NewFormat_StakeQuestion_TypeIsCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var question = document.Package.Rounds[0].Themes[0].Questions[1]; // Price 200

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(question.Price, Is.EqualTo(200));
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.Stake));
            Assert.That(question.Right, Has.Count.GreaterThan(0));
            Assert.That(question.Wrong, Has.Count.GreaterThan(0));
        });
    }

    [Test]
    public void Load_NewFormat_SecretQuestion_TypeAndParametersAreCorrect()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var question = document.Package.Rounds[0].Themes[0].Questions[2]; // Price 300

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(question.Price, Is.EqualTo(300));
            Assert.That(question.TypeName, Is.EqualTo(QuestionTypes.Secret));
            Assert.That(question.Parameters.ContainsKey(QuestionParameterNames.Theme), Is.True);
            Assert.That(question.Parameters[QuestionParameterNames.Theme].SimpleValue, Is.EqualTo("Новая тема"));
        });
    }

    [Test]
    public void Load_NewFormat_SelectAnswerQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var additionalTheme = document.Package.Rounds[0].Themes[2]; // "Дополнительно"
        var selectQuestion = additionalTheme.Questions[0]; // Price 100

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(selectQuestion.Price, Is.EqualTo(100));
            Assert.That(selectQuestion.Parameters.ContainsKey("answerType"), Is.True);
            Assert.That(selectQuestion.Parameters["answerType"].SimpleValue, Is.EqualTo("select"));
            Assert.That(selectQuestion.Parameters.ContainsKey("answerOptions"), Is.True);
            Assert.That(selectQuestion.Right[0], Is.EqualTo("C"));
        });

        var answerOptions = selectQuestion.Parameters["answerOptions"];
        Assert.That(answerOptions.Type, Is.EqualTo(StepParameterTypes.Group));
        Assert.That(answerOptions.GroupValue, Is.Not.Null);
        Assert.That(answerOptions.GroupValue, Does.ContainKey("A"));
        Assert.That(answerOptions.GroupValue, Does.ContainKey("B"));
        Assert.That(answerOptions.GroupValue, Does.ContainKey("C"));
        Assert.That(answerOptions.GroupValue, Does.ContainKey("D"));
    }

    [Test]
    public void Load_NewFormat_NumberAnswerQuestion_IsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var additionalTheme = document.Package.Rounds[0].Themes[2]; // "Дополнительно"
        var numberQuestion = additionalTheme.Questions[1]; // Price 200

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(numberQuestion.Price, Is.EqualTo(200));
            Assert.That(numberQuestion.Parameters.ContainsKey("answerType"), Is.True);
            Assert.That(numberQuestion.Parameters["answerType"].SimpleValue, Is.EqualTo("number"));
            Assert.That(numberQuestion.Parameters.ContainsKey("answerDeviation"), Is.True);
            Assert.That(numberQuestion.Right[0], Is.EqualTo("100"));
        });
    }

    [Test]
    public void Load_NewFormat_TextAndAudioQuestion_ContentIsDeserialized()
    {
        // Arrange & Act
        using var fs = File.OpenRead("SIGameTestNew.siq");
        using var document = SIDocument.Load(fs);
        var contentTheme = document.Package.Rounds[0].Themes[1];
        var question = contentTheme.Questions[5]; // Price 600

        // Assert
        Assert.That(question.Price, Is.EqualTo(600));

        var questionParam = question.Parameters[QuestionParameterNames.Question];
        Assert.That(questionParam.ContentValue, Has.Count.EqualTo(2));

        var textContent = questionParam.ContentValue![0];
        Assert.That(textContent.Type, Is.EqualTo(ContentTypes.Text));
        Assert.That(textContent.Value, Is.EqualTo("Текст"));
        Assert.That(textContent.WaitForFinish, Is.False);

        var audioContent = questionParam.ContentValue[1];
        Assert.That(audioContent.Type, Is.EqualTo(ContentTypes.Audio));
        Assert.That(audioContent.IsRef, Is.True);
        Assert.That(audioContent.Placement, Is.EqualTo(ContentPlacements.Background));
    }
}
