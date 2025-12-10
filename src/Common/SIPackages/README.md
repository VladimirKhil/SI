# SIPackages

A .NET library for working with SIQ quiz package files used in SIGame.

## Overview

SIPackages is a library that provides APIs to create, read, modify, and save SIQ package files. SIQ is a ZIP-based format containing quiz questions with support for text, images, audio, video, and HTML content.

### SIQ File Format

SIQ files are ZIP archives with the following structure:

```
package.siq (ZIP)
├── content.xml          # Package structure and questions (required)
├── Images/             # Image files directory
├── Audio/              # Audio files directory
├── Video/              # Video files directory
├── Html/               # HTML files directory
└── [Content_Types].xml # MIME types mapping
```

The format is defined by the XML schema in `assets/siq_5.xsd`.

## Installation

```bash
dotnet add package SIPackages
```

## Core Classes

### SIDocument

The main entry point for working with SIQ files. Represents a complete package document with media collections.

**Key Properties:**
- `Package` - The package object containing all game content
- `Images` - Collection of image files
- `Audio` - Collection of audio files
- `Video` - Collection of video files
- `Html` - Collection of HTML files
- `Authors` - List of package authors (for version 5.0+, same as Package.Global.Authors)
- `Sources` - List of package sources (for version 5.0+, same as Package.Global.Sources)

### Package

Represents the quiz package with metadata and game structure.

**Key Properties:**
- `Name` - Package name
- `ID` - Unique package identifier
- `Version` - Package format version (current: 5.0)
- `Date` - Creation date
- `Difficulty` - Difficulty level (0-10)
- `Publisher` - Package publisher
- `Language` - Package language
- `Tags` - List of tags
- `Rounds` - List of game rounds

### Round

Represents a game round containing themes.

**Key Properties:**
- `Name` - Round name
- `Type` - Round type (e.g., "standard", "final")
- `Themes` - List of themes

### Theme

Represents a theme (category) within a round.

**Key Properties:**
- `Name` - Theme name
- `Questions` - List of questions

### Question

Represents a single quiz question.

**Key Properties:**
- `Price` - Question price/points
- `TypeName` - Question type (default: "standard")
- `Parameters` - Question parameters (question text, answer, etc.)
- `Right` - List of correct answers
- `Wrong` - List of wrong answers

### ContentItem

Represents a piece of content (text, image, audio, video, or HTML).

**Key Properties:**
- `Type` - Content type (use `ContentTypes` constants)
- `Value` - Content value (text or filename)
- `IsRef` - True if Value is a reference to a file in the package
- `Placement` - Where to display content (screen/background)
- `Duration` - Playback duration
- `WaitForFinish` - Wait for content to finish before continuing

### DataCollection

Manages files within a specific media category (Images, Audio, Video, Html).

**Key Methods:**
- `GetFile(fileName)` - Get file stream
- `AddFileAsync(fileName, stream)` - Add file to collection
- `RemoveFile(fileName)` - Remove file from collection
- `Contains(fileName)` - Check if file exists

## Usage Examples

### Creating a New Package

```csharp
using SIPackages;
using SIPackages.Core;

// Create a new package
using var doc = SIDocument.Create("My Quiz", "Author Name");

var package = doc.Package;
package.Date = DateTime.UtcNow.ToString("dd.MM.yyyy");
package.Difficulty = 5;
package.Language = "en";
package.Tags.Add("General Knowledge");

// Add a round
var round = new Round { Name = "Round 1" };
package.Rounds.Add(round);

// Add a theme
var theme = new Theme { Name = "Science" };
round.Themes.Add(theme);

// Add a question with text
var question = new Question { Price = 100 };

// Add question text using parameters
question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Text, Value = "What is H2O?" }
    }
};

// Add correct answer
question.Right.Add("Water");

theme.Questions.Add(question);

// Save to file
using var fs = File.Create("my-quiz.siq");
doc.SaveAs(fs, true);
```

### Loading and Reading a Package

```csharp
using SIPackages;

// Load from file
using var fs = File.OpenRead("quiz.siq");
using var doc = SIDocument.Load(fs);

var package = doc.Package;
Console.WriteLine($"Package: {package.Name}");
Console.WriteLine($"Version: {package.Version:F1}");
Console.WriteLine($"Difficulty: {package.Difficulty}");

// Iterate through content
foreach (var round in package.Rounds)
{
    Console.WriteLine($"Round: {round.Name}");
    
    foreach (var theme in round.Themes)
    {
        Console.WriteLine($"  Theme: {theme.Name}");
        
        foreach (var question in theme.Questions)
        {
            Console.WriteLine($"    Question (${question.Price})");
            
            // Get question text
            if (question.Parameters.TryGetValue(
                QuestionParameterNames.Question, 
                out var questionParam))
            {
                foreach (var item in questionParam.ContentValue)
                {
                    Console.WriteLine($"      Content: {item.Type} - {item.Value}");
                }
            }
            
            // Get answers
            foreach (var answer in question.Right)
            {
                Console.WriteLine($"      Answer: {answer}");
            }
        }
    }
}
```

### Adding Media Files

```csharp
using SIPackages;
using SIPackages.Core;

using var doc = SIDocument.Create("Quiz with Media", "Author");

// Add an image file
using var imageStream = File.OpenRead("picture.jpg");
await doc.Images.AddFileAsync("picture.jpg", imageStream);

// Create question with image
var question = new Question { Price = 200 };

question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem 
        { 
            Type = ContentTypes.Image, 
            Value = "picture.jpg",
            IsRef = true  // Reference to file in package
        }
    }
};

question.Right.Add("Answer");

// Add to package structure...
var round = new Round { Name = "Round 1" };
doc.Package.Rounds.Add(round);

var theme = new Theme { Name = "Theme" };
round.Themes.Add(theme);
theme.Questions.Add(question);

// Save
using var fs = File.Create("quiz-with-media.siq");
doc.SaveAs(fs, true);
```

### Working with Media Collections

```csharp
using var fs = File.OpenRead("quiz.siq");
using var doc = SIDocument.Load(fs);

// List all images
Console.WriteLine("Images:");
foreach (var imageName in doc.Images)
{
    Console.WriteLine($"  {imageName}");
    
    // Get file info
    var fileInfo = doc.Images.GetFile(imageName);
    if (fileInfo != null)
    {
        using var imageStream = fileInfo.Stream;
        // Process image stream...
    }
}

// List all videos
Console.WriteLine("Videos:");
foreach (var videoName in doc.Video)
{
    Console.WriteLine($"  {videoName}");
}

// Add new audio file
using var audioStream = File.OpenRead("sound.mp3");
await doc.Audio.AddFileAsync("sound.mp3", audioStream);

doc.Save();
```

### Modifying Existing Package

```csharp
using var fs = File.Open("quiz.siq", FileMode.Open);
using var doc = SIDocument.Load(fs, read: false);

// Modify package metadata
doc.Package.Difficulty = 7;
doc.Package.Tags.Add("Updated");

// Add a new question to first theme
var firstTheme = doc.Package.Rounds[0].Themes[0];
var newQuestion = new Question 
{ 
    Price = 500
};

newQuestion.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Text, Value = "New question?" }
    }
};

newQuestion.Right.Add("New answer");
firstTheme.Questions.Add(newQuestion);

// Save changes
doc.Save();
```

### Loading from XML Only

```csharp
using var xmlStream = File.OpenRead("content.xml");
using var doc = SIDocument.LoadXml(xmlStream);

// Access package structure
var package = doc.Package;
// Note: Media files won't be available since only XML was loaded
```

### Getting Question Content

```csharp
using var doc = SIDocument.Load(File.OpenRead("quiz.siq"));

foreach (var round in doc.Package.Rounds)
{
    foreach (var theme in round.Themes)
    {
        foreach (var question in theme.Questions)
        {
            // Get all content items from question
            var contentItems = question.GetContent();
            
            foreach (var item in contentItems)
            {
                if (item.IsRef)
                {
                    // Get actual media file
                    var media = doc.TryGetMedia(item);
                    if (media != null)
                    {
                        Console.WriteLine($"Media: {media.Value.Uri}");
                    }
                }
                else
                {
                    Console.WriteLine($"Inline: {item.Value}");
                }
            }
        }
    }
}
```

## Constants

### ContentTypes
- `ContentTypes.Text` - Text content
- `ContentTypes.Image` - Image content
- `ContentTypes.Audio` - Audio content  
- `ContentTypes.Video` - Video content
- `ContentTypes.Html` - HTML content

### QuestionParameterNames
- `QuestionParameterNames.Question` - Question body
- `QuestionParameterNames.Answer` - Question answer

### StepParameterTypes
- `StepParameterTypes.Content` - Content parameter (text/media)
- `StepParameterTypes.Simple` - Simple text parameter

## Thread Safety

SIDocument is not thread-safe. Use appropriate synchronization when accessing from multiple threads.

## Disposal

Always dispose SIDocument when done to release file handles and resources:

```csharp
using var doc = SIDocument.Load(stream);
// Work with document...
// Automatically disposed at end of using block
```

## License

This library is part of the SI (SIGame) project. See LICENSE file for details.
