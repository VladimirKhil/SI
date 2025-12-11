# SIEngine.Core Developer Guide

## Overview

`SIEngine.Core` provides a state machine-based engine for playing SI (SIGame) quiz questions. The engine processes questions step-by-step, invoking handler methods to display content, manage answerers, and control game flow.

## Architecture

### Core Components

- **`IQuestionEngine`** - The main interface for question playback control
- **`QuestionEngine`** - Implementation of the question engine state machine
- **`IQuestionEnginePlayHandler`** - Interface for handling engine events (to be implemented by game UI/logic)
- **`QuestionEngineOptions`** - Configuration options for engine behavior

### State Machine Model

The engine operates as a deterministic state machine that:
1. Processes a question's script (sequence of steps) in order
2. Invokes handler methods at each step
3. Pauses when handler returns `true` (waiting for external events)
4. Continues automatically when handler returns `false`
5. Guarantees termination in finite time

## Question Structure

Questions in SI packages consist of:

- **Parameters** - Dictionary of named parameters (question text, answer, etc.)
- **Script** - Sequence of steps defining how to play the question
- **Right answers** - Collection of correct answer strings
- **Wrong answers** - Collection of incorrect answer strings (optional)

### Content Items

Content is represented by `ContentItem` objects with properties:

```csharp
public class ContentItem
{
    string Type;           // text, image, audio, video, html
    string Value;          // Content value or filename
    bool IsRef;            // True if Value is a file reference
    string Placement;      // screen, background, etc.
    TimeSpan? Duration;    // Playback duration (for media)
    bool WaitForFinish;    // Wait before showing next content
}
```

### Question Parameters

Common parameter names (from `QuestionParameterNames`):
- `question` - The question content to display
- `answer` - The answer content to display
- Custom parameters can be defined in scripts

## The Four Major Stages

Every question progresses through these stages:

### Stage 1: Preambula
Setting up the question context:
- **Set Theme** (`OnSetTheme`) - Display theme/category name
- **Set Answerer** (`OnSetAnswerer`) - Choose who can answer (single player, all, etc.)
- **Announce Price** (`OnAnnouncePrice`) - Announce available price ranges
- **Set Price** (`OnSetPrice`) - Set question price/stake
- **Set Answer Type** (`OnAnswerOptions`, `OnNumericAnswerType`) - Define answer format

### Stage 2: Displaying Question
Showing the question content:
- **Show Content** (`OnQuestionContent`) - Display text, images, audio, video
- **Content Start** (`OnContentStart`) - Notifies about upcoming content sequence
- Multiple content items may be shown sequentially
- Content can have different placements (screen, background)

### Stage 3: Asking Answer(s)
Accepting player responses:
- **Button Press Start** (`OnButtonPressStart`) - Enable answer buttons
- **Ask Answer** (`OnAskAnswer`) - Request answer from player(s)
- **Answer Start** (`OnAnswerStart`) - Marks the beginning of answer processing

### Stage 4: Displaying Right Answer
Showing the correct answer:
- **Show Content** (answer) - Display answer content
- **Right Answer Option** (`OnRightAnswerOption`) - Show correct option for select-type questions
- **Simple Right Answer Start** (`OnSimpleRightAnswerStart`) - Notify about simple text answer

## Implementing a Custom Handler

To use the question engine, you must implement `IQuestionEnginePlayHandler`:

```csharp
public class MyQuestionHandler : IQuestionEnginePlayHandler
{
    public void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback)
    {
        // Initialize question UI
        // Store right answers for validation
        // Store skip callback for later use
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        // Display content items on screen
        foreach (var item in content)
        {
            switch (item.Type)
            {
                case ContentTypes.Text:
                    ShowText(item.Value);
                    break;
                case ContentTypes.Image:
                    ShowImage(item.Value, item.IsRef);
                    break;
                // Handle other types...
            }
        }
    }

    public void OnAskAnswer(string mode)
    {
        // Enable answer input based on mode
        if (mode == StepParameterValues.AskAnswerMode_Button)
        {
            EnableAnswerButton();
        }
    }

    public bool OnButtonPressStart()
    {
        // Enable the answer button
        // Return true to pause engine until button is pressed
        EnableButton();
        return true;
    }

    // Implement other required methods...
}
```

### Handler Return Values

Methods return `bool` to control state machine flow:
- **`true`** - Pause the engine (waiting for user action, content playback, etc.)
- **`false`** - Continue immediately to next step

Examples:
- `OnButtonPressStart()` returns `true` - wait for button press
- `OnSetTheme()` might return `false` - immediately continue
- `OnAnswerOptions()` might return `true` - wait for options to display

### Creating and Using the Engine

```csharp
// Create a question
var question = new Question { Price = 100 };
question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Text, Value = "What is 2+2?" }
    }
};
question.Right.Add("4");

// Create options
var options = new QuestionEngineOptions
{
    FalseStarts = FalseStartMode.Enabled,
    ShowSimpleRightAnswers = true,
    DefaultTypeName = QuestionTypes.Simple
};

// Create handler
var handler = new MyQuestionHandler();

// Create and run engine
var engine = new QuestionEngine(question, options, handler);

// Play question step by step
while (engine.PlayNext())
{
    // Wait for appropriate event (button press, timer, etc.)
    await WaitForNextStep();
}
```

## Key Methods

### PlayNext()

Advances the state machine by one step. Call repeatedly to play the question.

**Returns:**
- `true` - Engine is paused, waiting for external event
- `false` - Question has finished, no more steps

**Usage Pattern:**
```csharp
// Method 1: Loop until complete
while (engine.PlayNext())
{
    await WaitForEvent();
}

// Method 2: Event-driven
void OnContentFinished()
{
    if (engine.PlayNext())
    {
        // More steps to process
    }
    else
    {
        // Question complete
    }
}
```

### MoveToAnswer()

Skips to the answer display stage. Call when:
- A player has answered correctly
- All players have passed
- Time has run out

**Current Behavior:**
Jumps to the first step after the last `AskAnswer` step.

**Future Enhancement:**
Will play all remaining steps in "fast mode", ensuring all content is processed but only final states are displayed. This guarantees proper state setup before showing the answer.

## False Start Management

False starts control when players can press the answer button:

- **`FalseStartMode.Enabled`** - Button enabled after all content is shown
- **`FalseStartMode.Disabled`** - Button enabled from the start
- **`FalseStartMode.TextContentOnly`** - Button enabled after text, before multimedia

Configuration:
```csharp
var options = new QuestionEngineOptions
{
    FalseStarts = FalseStartMode.Enabled
};
```

## Script Steps

Questions are defined by scripts with these step types:

| Step Type | Purpose | Handler Method |
|-----------|---------|----------------|
| `SetAnswerer` | Choose answerer(s) | `OnSetAnswerer` |
| `AnnouncePrice` | Announce price options | `OnAnnouncePrice` |
| `SetPrice` | Set question price | `OnSetPrice` |
| `SetTheme` | Set theme name | `OnSetTheme` |
| `SetAnswerType` | Define answer format | `OnAnswerOptions`, `OnNumericAnswerType` |
| `ShowContent` | Display content | `OnQuestionContent` |
| `AskAnswer` | Request answer | `OnAskAnswer` |
| `Accept` | Auto-accept answer | `OnAccept` |

## Answer Types

### Text Answer (Default)
Players type or speak the answer. Handler validates against `question.Right` collection.

### Select Answer
Multiple choice with visual options.

```csharp
// Handler receives:
bool OnAnswerOptions(
    AnswerOption[] answerOptions,
    IReadOnlyList<ContentItem[]> screenContentSequence)
{
    // Display options to player
    // Return true to pause until selection
}
```

### Numeric Answer
Numeric answer with acceptable deviation.

```csharp
bool OnNumericAnswerType(int deviation)
{
    // Configure numeric input
    // Accept answers within ±deviation of correct value
}
```

## Common Patterns

### Simple Text Question
```csharp
var question = new Question();
question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Text, Value = "Question text" }
    }
};
question.Right.Add("Answer");
```

### Question with Image
```csharp
question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Image, Value = "image.jpg", IsRef = true }
    }
};
```

### Multiple Content Items
```csharp
question.Parameters[QuestionParameterNames.Question] = new StepParameter
{
    Type = StepParameterTypes.Content,
    ContentValue = new List<ContentItem>
    {
        new ContentItem { Type = ContentTypes.Text, Value = "Listen to:" },
        new ContentItem 
        { 
            Type = ContentTypes.Audio, 
            Value = "audio.mp3", 
            IsRef = true,
            WaitForFinish = true  // Pause after audio
        },
        new ContentItem { Type = ContentTypes.Text, Value = "What is it?" }
    }
};
```

## Best Practices

1. **Always implement all handler methods** - Even if you return `false` immediately, implement every method in `IQuestionEnginePlayHandler`.

2. **Store callbacks** - Methods like `OnQuestionStart` provide callbacks (e.g., `skipQuestionCallback`). Store these for later use.

3. **Handle content references** - Check `ContentItem.IsRef` to determine if `Value` is a filename or inline content.

4. **Respect WaitForFinish** - When `ContentItem.WaitForFinish` is true, don't call `PlayNext()` until content completes.

5. **Validate answers properly** - Use the `rightAnswers` collection from `OnQuestionStart` to validate player responses.

6. **Test edge cases** - Test with empty content, missing parameters, and unusual step sequences.

## Error Handling

The engine is designed to be robust:
- Missing parameters are skipped
- Invalid step configurations are ignored
- Null values are handled gracefully

However, handlers should:
- Validate input parameters
- Handle missing content gracefully
- Provide fallback UI states

## Performance Considerations

- Content items may reference large media files - implement lazy loading
- Some questions may have many steps - handlers should be efficient
- State machine execution is synchronous - don't block in handlers

## Testing

See `test/Common/SIEngine.Core.Tests/QuestionEngineTests.cs` for comprehensive examples.

## Reference

For more information about question package structure, see:
- `src/Common/SIPackages/README.md` - Package format documentation
- `assets/siq_5.xsd` - XML schema definition
- `IQuestionEnginePlayHandler.cs` - Complete handler interface
