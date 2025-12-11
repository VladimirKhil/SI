# SIEngine Developer Guide

## Overview

`SIEngine` is a high-level engine for playing complete SIGame packages (quiz games). It orchestrates the entire game flow from start to finish, managing rounds, themes, question selection, and integrating with the lower-level `SIEngine.Core` question engine for individual question playback.

## Architecture

### Core Components

- **`GameEngine`** - The main engine class that plays an entire SIGame package
- **`ISIEnginePlayHandler`** - Interface for handling game-level events (to be implemented by game UI/logic)
- **`IQuestionEnginePlayHandler`** - Interface for handling question-level events (from `SIEngine.Core`)
- **`GameRules`** - Defines rules for different game modes (Classic, Simple, Quiz, etc.)
- **`EngineFactory`** - Factory for creating configured game engines

### Integration with SIEngine.Core

`SIEngine` builds on top of `SIEngine.Core`:
- `SIEngine` manages package-level flow (rounds, themes, question selection)
- `SIEngine.Core.QuestionEngine` handles individual question playback
- `GameEngine` creates and delegates to `QuestionEngine` instances for each question

## Game Flow and Finite Execution

The engine guarantees that **every package will complete in a finite number of steps**. This is achieved through:

1. **Fixed structure**: Packages have a finite number of rounds, themes, and questions
2. **State machine design**: Each stage transitions deterministically to the next
3. **Selection strategies**: All strategies (SelectByPlayer, Sequential, RemoveOtherThemes) eventually exhaust available questions
4. **No infinite loops**: The engine never repeats stages without making progress

### Game Stages

The engine progresses through these stages in order:

```
Begin → [GameThemes] → Round → SelectingQuestion → QuestionType → Question → EndRound → ... → EndGame → None
```

Each stage corresponds to a `GameStage` enum value:

| Stage | Description | Next Stage |
|-------|-------------|------------|
| `Begin` | Initial setup | `GameThemes` or `Round` |
| `GameThemes` | Show all themes across rounds (optional) | `Round` |
| `Round` | Start a new round | `SelectingQuestion` or next `Round` |
| `SelectingQuestion` | Choose which question to play | `QuestionType` or `EndRound` |
| `QuestionType` | Announce question type | `Question` |
| `Question` | Play the question | `SelectingQuestion` or `EndRound` |
| `EndRound` | Finish current round | Next `Round` or `EndGame` |
| `EndGame` | Game completion | `None` |
| `None` | Final state (engine stopped) | - |

## Handler Method Call Order

The `ISIEnginePlayHandler` interface defines callbacks that the engine invokes during gameplay. Here's the typical call sequence for a complete game:

### Complete Game Flow

```
1. OnPackage(package)                    // Game starts
2. [OnGameThemes(themes)]                 // Optional: show all themes
3. OnRound(round, strategyType)          // Round 1 starts
4. OnRoundThemes(themes, controller)     // Show round table
5. [AskForQuestionSelection(...)]         // Request question choice (player-select mode)
6. OnQuestionSelected(themeIdx, qIdx)    // Question selected
7. OnQuestion(question)                  // Question starts (delegates to QuestionEngine)
8. OnQuestionType(typeName, isDefault)   // Question type announced
9. [QuestionEngine handlers...]           // Question content/answer flow (see SIEngine.Core docs)
10. OnQuestionEnd(comments)              // Question completes
11. (repeat 5-10 for more questions)
12. OnRoundEnd(reason)                   // Round ends
13. (repeat 3-12 for more rounds)
14. OnPackageEnd()                       // Game ends
```

### Handler Method Groups

#### Package-Level Handlers
- **`OnPackage(Package package)`** - Called once at game start
- **`OnPackageEnd()`** - Called once at game end
- **`OnGameThemes(IEnumerable<string> themes)`** - Optional; shows all unique themes

#### Round-Level Handlers
- **`OnRound(Round round, QuestionSelectionStrategyType strategyType)`** - Called when starting a round
- **`OnRoundSkip(QuestionSelectionStrategyType strategyType)`** - Called when a round is skipped
- **`OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController controller)`** - Shows the round's themes/questions table
- **`OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)`** - Special handling for final rounds
- **`OnRoundEnd(RoundEndReason reason)`** - Called when round finishes

#### Question Selection Handlers
- **`AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)`** - Requests player to choose question
- **`OnQuestionSelected(int themeIndex, int questionIndex)`** - Notifies that question was selected
- **`AskForThemeDelete(Action<int> deleteCallback)`** - Requests theme deletion (final rounds)
- **`OnThemeDeleted(int themeIndex)`** - Notifies that theme was deleted
- **`OnThemeSelected(int themeIndex, int questionIndex)`** - Final theme selection

#### Question-Level Handlers
- **`OnTheme(Theme theme)`** - Called before playing a theme's question
- **`OnQuestion(Question question)`** - Called when starting question playback
- **`OnQuestionType(string typeName, bool isDefault)`** - Announces question type
- **`OnQuestionRestored(int themeIndex, int questionIndex, int price)`** - Question put back on table
- **`OnQuestionEnd(string comments)`** - Called when question completes; returns `true` if round should timeout

## Question Selection Strategies

The engine uses different strategies based on game rules and round type:

### 1. SelectByPlayer (Classic Game)

In this mode, a player (or host) manually selects which question to play next.

**Handler Call Sequence:**
```
OnRound(round, SelectByPlayer)
OnRoundThemes(themes, tableController)
AskForQuestionSelection(options, selectCallback)
  [Player makes selection]
  selectCallback(themeIndex, questionIndex)
OnQuestionSelected(themeIndex, questionIndex)
OnTheme(theme)
OnQuestion(question)
OnQuestionType(typeName, isDefault)
[Question plays via QuestionEngine]
OnQuestionEnd(comments)
  [Repeat for next question]
OnRoundEnd(reason)
```

**Key Points:**
- Handler must store `selectCallback` and invoke it when player makes a choice
- `options` contains valid (themeIndex, questionIndex) pairs
- Questions are removed from table as they're played

### 2. Sequential

Questions are played automatically in order (theme-by-theme, question-by-question).

**Handler Call Sequence:**
```
OnRound(round, Sequential)
OnRoundThemes(themes, tableController)
OnQuestionSelected(0, 0)          // Auto-selected
OnTheme(theme)
OnQuestion(question)
OnQuestionType(typeName, isDefault)
[Question plays]
OnQuestionEnd(comments)
OnQuestionSelected(0, 1)          // Next question auto-selected
[Continue until all questions played]
OnRoundEnd(reason)
```

**Key Points:**
- No `AskForQuestionSelection` call - selection is automatic
- Questions play in order: theme 0 question 0, theme 0 question 1, ..., theme 1 question 0, ...
- No manual intervention needed

### 3. RemoveOtherThemes (Final Round)

Themes are eliminated one by one until one remains, then its first question is played.

**Handler Call Sequence:**
```
OnRound(round, RemoveOtherThemes)
OnFinalThemes(themes, willPlayAllThemes=false, isFirstPlay=true)
AskForThemeDelete(deleteCallback)
  [Player/logic selects theme to delete]
  deleteCallback(themeIndex)
OnThemeDeleted(themeIndex)
OnFinalThemes(remaining themes, willPlayAllThemes=false, isFirstPlay=false)
  [Repeat theme deletion]
OnThemeSelected(lastThemeIndex, 0)
OnTheme(theme)
OnQuestion(question)
OnQuestionType(typeName, isDefault)
[Question plays]
OnQuestionEnd(comments)
OnRoundEnd(reason)
```

**Key Points:**
- Themes are deleted until one remains
- Only the first question of the final theme is played
- `OnFinalThemes` can be called multiple times as themes are eliminated

## Creating and Using the Engine

### Basic Setup

```csharp
using SIEngine;
using SIEngine.Core;
using SIEngine.Rules;
using SIPackages;

// Load a package
using var document = SIDocument.Load(File.OpenRead("quiz.siq"));

// Create handlers
var gameHandler = new MyGameHandler();
var questionHandler = new MyQuestionHandler();

// Create engine with Classic rules
var engine = EngineFactory.CreateEngine(
    WellKnownGameRules.Classic,
    document,
    () => new EngineOptions
    {
        IsPressMode = true,
        IsMultimediaPressMode = true,
        ShowRight = true,
        PlaySpecials = true
    },
    gameHandler,
    questionHandler);

// Start the game
engine.MoveNext(); // Calls OnPackage()

// Continue playing
while (engine.CanNext())
{
    engine.MoveNext();
    
    // Wait for events (button press, content finish, etc.)
    await WaitForNextStep();
}
```

### Implementing ISIEnginePlayHandler

```csharp
public class MyGameHandler : ISIEnginePlayHandler
{
    private Action<int, int>? _selectQuestionCallback;
    private Action<int>? _deleteThemeCallback;
    
    public void OnPackage(Package package)
    {
        // Display package info
        Console.WriteLine($"Starting: {package.Name}");
        Console.WriteLine($"Author: {package.Info.Authors.FirstOrDefault()?.Name}");
    }
    
    public void OnGameThemes(IEnumerable<string> themes)
    {
        // Show all themes at game start
        Console.WriteLine("Game Themes:");
        foreach (var theme in themes)
        {
            Console.WriteLine($"  - {theme}");
        }
    }
    
    public void OnRound(Round round, QuestionSelectionStrategyType strategyType)
    {
        // Start a new round
        Console.WriteLine($"\n=== {round.Name} ===");
        Console.WriteLine($"Strategy: {strategyType}");
    }
    
    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController controller)
    {
        // Display round table
        for (int i = 0; i < themes.Count; i++)
        {
            Console.WriteLine($"{i}: {themes[i].Name}");
            for (int j = 0; j < themes[i].Questions.Count; j++)
            {
                Console.WriteLine($"  [{j}] ${themes[i].Questions[j].Price}");
            }
        }
    }
    
    public void AskForQuestionSelection(
        IReadOnlyCollection<(int, int)> options, 
        Action<int, int> selectCallback)
    {
        // Store callback for later use
        _selectQuestionCallback = selectCallback;
        
        // Request player input (async)
        Console.WriteLine("Select a question (theme, question):");
        foreach (var (themeIdx, questionIdx) in options)
        {
            Console.WriteLine($"  ({themeIdx}, {questionIdx})");
        }
    }
    
    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        // Question was selected (manually or automatically)
        Console.WriteLine($"Selected: Theme {themeIndex}, Question {questionIndex}");
    }
    
    public void OnQuestion(Question question)
    {
        // Question is about to play
        Console.WriteLine($"\nQuestion for ${question.Price}");
    }
    
    public void OnQuestionType(string typeName, bool isDefault)
    {
        if (!isDefault)
        {
            Console.WriteLine($"Type: {typeName}");
        }
    }
    
    public bool OnQuestionEnd(string comments)
    {
        // Question finished
        if (!string.IsNullOrEmpty(comments))
        {
            Console.WriteLine($"Comments: {comments}");
        }
        
        // Return true to end round due to timeout
        return false;
    }
    
    public void OnRoundEnd(RoundEndReason reason)
    {
        Console.WriteLine($"Round ended: {reason}");
    }
    
    public void OnPackageEnd()
    {
        Console.WriteLine("\n=== Game Complete ===");
    }
    
    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        // Store callback for theme deletion
        _deleteThemeCallback = deleteCallback;
        Console.WriteLine("Select theme to delete:");
    }
    
    public void OnThemeDeleted(int themeIndex)
    {
        Console.WriteLine($"Theme {themeIndex} deleted");
    }
    
    public void OnThemeSelected(int themeIndex, int questionIndex)
    {
        Console.WriteLine($"Final theme selected: {themeIndex}");
    }
    
    public void OnFinalThemes(
        IReadOnlyList<Theme> themes, 
        bool willPlayAllThemes, 
        bool isFirstPlay)
    {
        Console.WriteLine($"Final themes (play all: {willPlayAllThemes}):");
        for (int i = 0; i < themes.Count; i++)
        {
            Console.WriteLine($"  {i}: {themes[i].Name}");
        }
    }
    
    public bool ShouldPlayRoundWithRemovableThemes()
    {
        // Return true if there are players who can delete themes
        return true;
    }
    
    public void OnRoundSkip(QuestionSelectionStrategyType strategyType)
    {
        Console.WriteLine($"Round skipped (strategy: {strategyType})");
    }
    
    public void OnTheme(Theme theme)
    {
        // Theme about to be played
        Console.WriteLine($"Theme: {theme.Name}");
    }
    
    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        // Question was put back on table
        Console.WriteLine($"Question restored: ({themeIndex}, {questionIndex}) ${price}");
    }
    
    // Method to call when player makes selection
    public void PlayerSelectedQuestion(int themeIndex, int questionIndex)
    {
        _selectQuestionCallback?.Invoke(themeIndex, questionIndex);
    }
    
    // Method to call when deleting a theme
    public void DeleteTheme(int themeIndex)
    {
        _deleteThemeCallback?.Invoke(themeIndex);
    }
}
```

## Engine Control Methods

### MoveNext()

Advances the engine to the next stage. Call this repeatedly to play the game.

```csharp
// Synchronous step-by-step play
while (engine.CanNext())
{
    engine.MoveNext();
}

// Event-driven play
void OnContentFinished()
{
    if (engine.CanNext())
    {
        engine.MoveNext();
    }
}
```

### MoveBack() / MoveNextRound() / MoveBackRound()

Navigate through the game:

```csharp
// Go back one question (if allowed)
if (engine.CanMoveBack)
{
    engine.MoveBack();
}

// Skip to next round
if (engine.CanMoveNextRound)
{
    engine.MoveNextRound();
}

// Go back to previous round
if (engine.CanMoveBackRound)
{
    engine.MoveBackRound();
}
```

### MoveToAnswer()

Skip the rest of the current question and jump to the answer:

```csharp
// Time's up - show answer immediately
engine.MoveToAnswer();
```

### MoveToRound(int roundIndex)

Jump directly to a specific round:

```csharp
// Jump to final round (index 3)
engine.MoveToRound(3);
```

## Game Rules

Use pre-defined rule sets or create custom rules:

### Well-Known Rules

```csharp
// Classic SIGame (player selects questions)
WellKnownGameRules.Classic

// Simple mode (sequential questions)
WellKnownGameRules.Simple

// Quiz mode (all-play questions)
WellKnownGameRules.Quiz

// Turn-taking mode
WellKnownGameRules.TurnTaking
```

### Custom Rules

```csharp
var customRules = new GameRules
{
    ShowGameThemes = true,
    DefaultRoundRules = new RoundRules
    {
        DefaultQuestionType = QuestionTypes.Simple,
        QuestionSelectionStrategyType = QuestionSelectionStrategyType.Sequential
    }
};

// Override rules for specific round types
customRules.RoundRules[RoundTypes.Final] = new RoundRules
{
    DefaultQuestionType = QuestionTypes.StakeAll,
    QuestionSelectionStrategyType = QuestionSelectionStrategyType.RemoveOtherThemes
};

var engine = EngineFactory.CreateEngine(
    customRules,
    document,
    optionsProvider,
    gameHandler,
    questionHandler);
```

## Engine Options

Configure engine behavior dynamically:

```csharp
var options = new EngineOptions
{
    // Enable answer buttons for text questions
    IsPressMode = true,
    
    // Enable answer buttons for multimedia questions
    IsMultimediaPressMode = true,
    
    // Show correct answers at the end
    ShowRight = true,
    
    // Play special question types (stake, auction, etc.)
    PlaySpecials = true,
    
    // In final round, play all questions instead of just one
    PlayAllQuestionsInFinalRound = false
};

// Options can change during gameplay
var engine = EngineFactory.CreateEngine(
    WellKnownGameRules.Classic,
    document,
    () => options, // Provider function
    gameHandler,
    questionHandler);

// Later in the game
options.ShowRight = false; // Disable showing answers
```

## Round Table Controller

The `IRoundTableController` allows dynamic question management:

```csharp
public void OnRoundThemes(
    IReadOnlyList<Theme> themes, 
    IRoundTableController controller)
{
    // Remove a played question from the table
    controller.RemoveQuestion(themeIndex: 0, questionIndex: 2);
    
    // Restore a question (if it was skipped/removed)
    controller.RestoreQuestion(themeIndex: 1, questionIndex: 0);
}
```

## Finite Execution Guarantee

### Why the Engine Always Terminates

1. **Package Structure is Finite:**
   - Fixed number of rounds in package
   - Fixed number of themes per round
   - Fixed number of questions per theme

2. **Monotonic Progress:**
   - Each question is played at most once
   - Rounds are played sequentially
   - Questions are removed from selection pool after play

3. **No Infinite Loops:**
   - `SelectByPlayer`: Runs out of questions eventually
   - `Sequential`: Linear progression through all questions
   - `RemoveOtherThemes`: Deletes themes until one remains, plays one question

4. **Termination Conditions:**
   - All questions in round played → `OnRoundEnd` → Next round
   - All rounds completed → `OnPackageEnd` → `GameStage.None`
   - Manual skip → Controlled progression
   - Timeout → Round ends immediately

### Example: Maximum Steps for a Package

For a package with:
- R = number of rounds
- T = average themes per round
- Q = average questions per theme

**Maximum steps ≈ R × (T × Q + overhead)**

Where overhead includes:
- Begin (1 step)
- GameThemes (1 step, optional)
- Round start per round (R steps)
- Question type per question (R × T × Q steps)
- Round end per round (R steps)
- EndGame (1 step)

**Total ≈ 1 + [0-1] + R + R×T×Q + R×T×Q + R + 1 ≈ 2×R×T×Q + 2R + 3**

This is clearly finite and deterministic.

## Common Patterns

### Event-Driven Game Loop

```csharp
async Task PlayGameAsync()
{
    var engine = CreateEngine();
    
    engine.MoveNext(); // Start
    
    while (engine.CanNext())
    {
        // Wait for UI events or timers
        await Task.Delay(100);
        
        // Check if ready to advance
        if (ShouldAdvance())
        {
            engine.MoveNext();
        }
    }
    
    Console.WriteLine("Game complete!");
}
```

### Handling Question Selection

```csharp
private Action<int, int>? _questionSelector;

public void AskForQuestionSelection(
    IReadOnlyCollection<(int, int)> options, 
    Action<int, int> selectCallback)
{
    _questionSelector = selectCallback;
    
    // Show UI for selection
    questionPicker.Show(options);
}

void OnPlayerClickedQuestion(int theme, int question)
{
    // Player made choice - notify engine
    _questionSelector?.Invoke(theme, question);
    _questionSelector = null;
    
    // Engine will call OnQuestionSelected next
}
```

### Integrating QuestionEngine Handlers

Since `GameEngine` delegates question playback to `QuestionEngine`, your `IQuestionEnginePlayHandler` receives all question-level events. See `SIEngine.Core/DOCUMENTATION.md` for details on those handlers.

```csharp
public class MyQuestionHandler : IQuestionEnginePlayHandler
{
    // Implement all question-level handlers
    // See SIEngine.Core documentation for complete details
    
    public void OnQuestionStart(bool buttonsRequired, 
        ICollection<string> rightAnswers, 
        Action skipQuestionCallback)
    {
        // Question starting...
    }
    
    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        // Display question content...
    }
    
    // ... other handlers from IQuestionEnginePlayHandler
}
```

## Best Practices

1. **Store Callbacks** - Methods like `AskForQuestionSelection` provide callbacks. Store them and invoke when the player makes a choice.

2. **Check CanNext()** - Before calling `MoveNext()`, verify `engine.CanNext()` returns `true`.

3. **Handle All Callbacks** - Implement every method in `ISIEnginePlayHandler` even if you just return immediately.

4. **Respect Strategy Types** - Different strategies have different flows. Handle `AskForQuestionSelection` vs automatic selection appropriately.

5. **Manage State** - Track current round, theme, and question if needed for your UI.

6. **Dispose Properly** - Always dispose the engine to release the SIDocument resources:
   ```csharp
   using var engine = CreateEngine();
   // ... use engine
   ```

7. **Test with Different Rules** - Test your handler with Classic, Simple, and Quiz rules to ensure it handles all flows.

8. **Handle Skipped Rounds** - Not all rounds may play (e.g., if no players available). Handle `OnRoundSkip`.

## Error Handling

The engine is designed to be robust, but handlers should:

- Validate callback parameters before using them
- Handle unexpected null values gracefully
- Provide fallback UI states
- Log errors rather than crashing

## Testing

See `test/Common/SIEngine.Tests/TvEngineTests.cs` for integration test examples showing complete game playthrough.

## Reference

For more information:
- `src/Common/SIEngine.Core/DOCUMENTATION.md` - Question engine details
- `src/Common/SIPackages/README.md` - Package format documentation
- `ISIEnginePlayHandler.cs` - Complete handler interface definition
- `GameEngine.cs` - Engine implementation
