# SImulator Migration Plan: UseSIGameEngine

## Executive Summary

This document outlines the plan for migrating SImulator from the current SIEngine-based approach to the new SICore-based approach using UseSIGameEngine. The new approach uses a person controller pattern instead of engine handlers, eliminating logic duplication with SICore while maintaining the user experience.

## Current Architecture (Old Approach)

### Components
- **SIEngine (`GameEngine`)**: High-level engine managing complete packages
- **SIEngine.Core (`QuestionEngine`)**: Low-level engine for individual questions
- **GameEngineController**: Implements `ISIEnginePlayHandler` and `IQuestionEnginePlayHandler`
- **GameActions**: Direct control over engine via `MoveNext()`, `MoveBack()`, etc.
- **PresentationController**: Handles UI presentation

### Key Characteristics
1. **Direct Control**: Host directly controls game flow via engine methods
2. **Client-Side Timers**: Application manages its own timers
3. **Client-Side Table**: Application tracks question table state
4. **Synchronous Flow**: Engine progresses step-by-step via `MoveNext()`
5. **Handler Callbacks**: Game logic responds to engine events via handler interfaces

### Features in Current Implementation
- Forward/backward navigation through questions
- Round skipping (forward/backward)
- Jump to answer
- Direct timer control (start/stop/pause)
- Direct table manipulation
- Complete game state control
- Manual player management

## New Architecture (SICore Approach)

### Components
- **SICore GameRunner**: Creates and runs complete game sessions
- **GameController**: Implements `IPersonController` interface
- **ViewerActions**: Sends commands to game server
- **ViewerData**: Receives game state updates
- **NewGameActions**: Wrapper providing simplified action interface
- **PresentationController**: Unchanged UI presentation layer

### Key Characteristics
1. **Managed Mode**: Game server controls flow; client sends requests
2. **Server-Side Timers**: Game server manages all timers via messages
3. **Server-Side Table**: Game server tracks question table state
4. **Event-Driven Flow**: Client responds to server messages
5. **Person Controller**: Implements `IPersonController` to receive events
6. **Message Protocol**: Communication via structured messages (STAGE, TIMER, CONTENT, etc.)

### Available in New Implementation
- Automatic game flow via server
- Timer events via `OnTimerChanged()`
- Table updates via `TableLoaded()`
- Content display via `OnContent()`
- Game stage transitions via `OnStage()`
- Player management via `ViewerActions`
- Replics (commentary) via `OnReplic()` and `SHOWMAN_REPLIC` messages

### Missing/Different in New Implementation
- No direct backward navigation
- No round skipping controls
- No direct timer manipulation (timers are server-controlled)
- No direct table manipulation (table is server-controlled)
- Limited manual control flow

## Comparison Matrix

| Feature | Old Approach (SIEngine) | New Approach (SICore) | Migration Strategy |
|---------|-------------------------|------------------------|-------------------|
| **Game Flow Control** |
| Move forward | `engine.MoveNext()` | `viewerActions.Move()` | Direct mapping |
| Move backward | `engine.MoveBack()` | Not available | Remove or use game snapshots |
| Skip to next round | `engine.MoveNextRound()` | Not available | Remove or use server control |
| Skip to previous round | `engine.MoveBackRound()` | Not available | Remove feature |
| Jump to answer | `engine.MoveToAnswer()` | Not available | Server auto-handles |
| **Timers** |
| Round timer | Client manages | Server controls via messages | Listen to `OnTimerChanged(0, ...)` |
| Question timer | Client manages | Server controls via messages | Listen to `OnTimerChanged(1, ...)` |
| Thinking timer | Client manages | Server controls via messages | Listen to `OnTimerChanged(2, ...)` |
| Start timer | Direct call | Automatic | No action needed |
| Stop timer | Direct call | Automatic | No action needed |
| Pause timer | Direct call | Via pause message | Use server pause if needed |
| **Question Table** |
| Display table | Via `OnRoundThemes()` | Via `TableLoaded()` | Map handler |
| Update table state | Client-side tracking | Server sends updates | Rely on server |
| Remove question | Client marks as played | Server auto-removes | Listen to updates |
| Restore question | `tableController.RestoreQuestion()` | Not available | Remove or request server |
| **Content Display** |
| Show question content | `OnQuestionContent()` | `OnContent()` | Map to new handler |
| Show answer | `OnSimpleRightAnswerStart()` | `OnRightAnswer()` / `OnRightAnswerStart()` | Map handlers |
| Content progression | Manual via callbacks | Automatic | Let server control |
| **Player Management** |
| Add player | Direct | `viewerActions.AddTable()` | Use ViewerActions |
| Remove player | Direct | `viewerActions.RemoveTable()` | Use ViewerActions |
| Set chooser | Direct tracking | Via `SETCHOOSER` message | Listen to server |
| Player scores | Direct tracking | Via `SUMS` message | Listen to server |
| **Question Selection** |
| Ask for selection | `AskForQuestionSelection()` | `SelectQuestion()` + `ShowTablo()` | Map to new flow |
| Select question | Callback invoke | `viewerActions.SelectQuestion()` | Use ViewerActions |
| **Special Questions** |
| Set answerer | `OnSetAnswerer()` | Via `OnSelectPlayer()` | Map handler |
| Stakes | `OnSetPrice()` | Via stake messages | Server handles |
| Theme deletion | `AskForThemeDelete()` | Via deletion messages | Server handles |
| **Commentary/Messages** |
| Simple messages | Via handler methods | `OnReplic()` | Use replics |
| Localized messages | N/A | `SHOWMAN_REPLIC` with codes | New feature |
| **Game Metadata** |
| Package info | `OnPackage()` | `OnPackage()` | Direct map |
| Round info | `OnRound()` | `OnStage()` | Map handler |
| Theme info | `OnTheme()` | `OnTheme()` | Direct map |
| **Answer Validation** |
| Button press | `OnButtonPressStart()` | `Try()` | Map handler |
| Answer submission | Direct | Via answer messages | Server handles |
| Validation | Manual | Via validation messages | Server handles |

## Migration Strategy

### Phase 1: Preparation
1. ✅ Create this migration plan document
2. Review all current GameEngineController handlers
3. Map each handler to equivalent IPersonController method
4. Identify features that cannot be preserved
5. Propose UX alternatives for removed features

### Phase 2: Core Implementation
1. **Update GameController**
   - Complete all stub implementations in `GameController.cs`
   - Map all game events to GameViewModel methods
   - Handle all timer events properly
   - Implement missing handlers

2. **Update NewGameActions**
   - Complete stub implementations in `NewGameActions.cs`
   - Remove unsupported operations or provide alternatives
   - Add error handling for missing features

3. **Update MainViewModel**
   - Uncomment UseSIGameEngine code path
   - Update game creation logic
   - Test both old and new modes side-by-side

### Phase 3: UX Preservation

#### Timer Synchronization
**Challenge**: Old approach has direct timer control; new approach receives timer events.

**Solution**:
```csharp
public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null)
{
    switch (timerIndex)
    {
        case 0: // Round timer
            if (timerCommand == "GO" && int.TryParse(arg, out var roundTime))
            {
                GameViewModel.RoundTime = 0;
                GameViewModel.RoundTimeMax = roundTime / 10;
                GameViewModel.RunRoundTimer.Execute(0);
            }
            else if (timerCommand == "STOP")
            {
                GameViewModel.StopRoundTimer.Execute(0);
            }
            else if (timerCommand == "PAUSE" && int.TryParse(arg, out var pauseTime))
            {
                GameViewModel.RoundTime = pauseTime / 10;
                GameViewModel.StopRoundTimer.Execute(0);
            }
            else if (timerCommand == "RESUME")
            {
                GameViewModel.RunRoundTimer.Execute(0);
            }
            break;

        case 1: // Question timer
            // Similar logic for question timer
            break;

        case 2: // Thinking/Decision timer
            if (timerCommand == "GO" && int.TryParse(arg, out var thinkTime))
            {
                GameViewModel.RunThinkingTimer(thinkTime / 10);
            }
            break;
    }
}
```

#### Table State Synchronization
**Challenge**: Old approach tracks table state locally; new approach receives updates.

**Solution**:
```csharp
public void TableLoaded(List<ThemeInfo> table)
{
    // Update local table representation
    GameViewModel.LoadTable(table);
    
    // Update presentation layer
    GameViewModel.PresentationController.UpdateTable(table);
}

// Listen to table updates via TOGGLE messages
public void OnQuestionToggled(int themeIndex, int questionIndex, int price)
{
    if (price == -1)
    {
        // Question removed
        GameViewModel.LocalInfo.RoundInfo[themeIndex]
            .Questions[questionIndex].Price = -1;
    }
    else
    {
        // Question restored
        GameViewModel.LocalInfo.RoundInfo[themeIndex]
            .Questions[questionIndex].Price = price;
    }
}
```

#### Navigation Controls
**Challenge**: Old approach allows backward navigation; new approach doesn't support it.

**Options**:
1. **Remove backward navigation** (simplest)
   - Disable Back button in UI
   - Show message: "Not available in managed mode"
   
2. **Use game state snapshots** (complex)
   - Server could implement snapshotting
   - Requires SICore changes
   - Out of scope for initial migration

3. **Hybrid mode** (recommended)
   - Keep UseSIGameEngine as optional setting
   - Default to new mode, allow switching to old mode if needed
   - Provides fallback for power users

**Recommended**: Remove backward navigation initially, add as feature request for SICore if needed.

#### Content Flow
**Challenge**: Old approach manually progresses through content; new approach is automatic.

**Solution**:
```csharp
public void OnContent(string placement, List<ContentInfo> content)
{
    GameViewModel.OnContentStart();
    GameViewModel.PresentationController.OnContentStart();

    switch (placement)
    {
        case ContentPlacements.Screen:
            var contentItems = content.Select(ci => new ContentItem
            {
                Type = ci.Type,
                Value = ci.Uri,
                IsRef = true,
                Placement = ContentPlacements.Screen,
                // Server controls timing, so WaitForFinish may not be needed
                WaitForFinish = false
            }).ToList();

            GameViewModel.ContentItems = contentItems;
            GameViewModel.PresentationController.OnQuestionContent(
                contentItems, 
                TryGetMediaUri, 
                ""
            );
            break;

        case ContentPlacements.Replic:
            // Handle replica content
            break;
    }
    
    // Auto-progress after content shown (server controls timing)
    // No need for manual MoveNext() calls
}
```

#### Chooser Management
**Challenge**: Old approach tracks chooser manually; new approach receives via messages.

**Solution**:
```csharp
public void OnSelectPlayer(SelectPlayerReason reason)
{
    switch (reason)
    {
        case SelectPlayerReason.Chooser:
            // Server will send SETCHOOSER message separately
            GameViewModel.OnSelectChooser();
            break;
        
        case SelectPlayerReason.Staker:
            GameViewModel.OnSelectStaker();
            break;
        
        case SelectPlayerReason.Answerer:
            GameViewModel.OnSelectAnswerer();
            break;
    }
}

// Separate handler for actual chooser assignment
public void OnChooserChanged(int playerIndex)
{
    GameViewModel.ChooserIndex = playerIndex;
    GameViewModel.PresentationController.SetChooser(playerIndex);
}
```

#### Special Question Types
**Challenge**: Old approach handles stakes/answerer selection directly; new approach uses messages.

**Solution**:
```csharp
// Stakes are handled by server, we just display
public void OnPlayerStake(int playerIndex, StakeType stakeType, int amount)
{
    var player = GameViewModel.Players[playerIndex];
    
    switch (stakeType)
    {
        case StakeType.Nominal:
            GameViewModel.ShowmanReplic = $"{player.Name}: {Resources.Nominal}";
            break;
        
        case StakeType.Sum:
            GameViewModel.ShowmanReplic = $"{player.Name}: {amount}";
            break;
        
        case StakeType.AllIn:
            GameViewModel.ShowmanReplic = $"{player.Name}: {Resources.AllIn}";
            break;
        
        case StakeType.Pass:
            GameViewModel.ShowmanReplic = $"{player.Name}: {Resources.Pass}";
            break;
    }
}

// Answer validation is handled by server
public void ValidateAnswer(int playerIndex, string answer)
{
    // Display validation request
    GameViewModel.ShowmanReplic = $"{Resources.AnswerValidation}: {answer}";
    
    // Server will send validation result via PERSON message
}

// Process validation result
public void OnPlayerOutcome(int playerIndex, bool isRight)
{
    var player = GameViewModel.Players[playerIndex];
    
    if (isRight)
    {
        player.State = PlayerState.Right;
        GameViewModel.PresentationController.ShowPlayerOutcome(playerIndex, true);
    }
    else
    {
        player.State = PlayerState.Wrong;
        GameViewModel.PresentationController.ShowPlayerOutcome(playerIndex, false);
    }
}
```

#### Replic System
**New Feature**: Use replics for enhanced commentary.

**Implementation**:
```csharp
public void OnReplic(string personCode, string text)
{
    if (personCode == "s")
    {
        // Showman replic
        GameViewModel.ShowmanReplic = text;
        GameViewModel.PresentationController.ShowReplic(text);
    }
    else if (personCode.StartsWith("p") && int.TryParse(personCode.Substring(1), out var playerIndex))
    {
        // Player replic
        if (playerIndex >= 0 && playerIndex < GameViewModel.Players.Count)
        {
            var player = GameViewModel.Players[playerIndex];
            // Could show player speech bubble or similar
        }
    }
}

// Use SHOWMAN_REPLIC for localized messages
public void OnShowmanReplic(int randomSeed, string messageCode, string[] args)
{
    // Map message codes to localized strings
    var message = LocalizeReplicCode(messageCode, args);
    GameViewModel.ShowmanReplic = message;
}

private string LocalizeReplicCode(string code, string[] args)
{
    switch (code)
    {
        case "RightAnswer":
            return Resources.RightAnswer;
        case "WrongAnswer":
            return Resources.WrongAnswer;
        case "QuestionPriceInfo":
            return string.Format(Resources.QuestionPrice, args[0]);
        case "PlayerChooses":
            return string.Format(Resources.PlayerChooses, args[0]);
        // Add more as needed
        default:
            return string.Join(" ", args);
    }
}
```

### Phase 4: Settings and UI Updates

#### Update Settings UI
1. Keep `UseSIGameEngine` checkbox visible in settings
2. Add tooltip explaining managed mode limitations
3. Default to `true` (new mode) for new installations
4. Preserve existing user setting for upgrades

#### Update Command Window
```xml
<!-- CommandWindow.xaml -->
<CheckBox 
    Margin="5,5,0,0" 
    IsChecked="{Binding UseSIGameEngine}" 
    Content="{x:Static lp:Resources.UseSIGameEngine}"
    ToolTip="{x:Static lp:Resources.UseSIGameEngine_Tooltip}" />
```

Add to resources:
```
UseSIGameEngine_Tooltip=Uses SIGame engine (managed mode). 
Some manual controls like backward navigation are not available.
```

#### Update Game Controls
- Disable/hide Back button when `UseSIGameEngine` is true
- Disable/hide Round navigation buttons when `UseSIGameEngine` is true
- Show appropriate tooltips explaining why controls are disabled

### Phase 5: Testing Strategy

#### Test Scenarios

##### 1. Classic (TV) Mode
- [ ] Package loads correctly
- [ ] Round themes display properly
- [ ] Question selection works
- [ ] Content displays (text, images, audio, video)
- [ ] Button press timing is correct
- [ ] Answer validation works
- [ ] Score tracking is accurate
- [ ] Timers sync correctly
- [ ] Round completion works
- [ ] Game completion works

##### 2. Simple (Sport) Mode
- [ ] Sequential question play works
- [ ] No question selection needed
- [ ] Content auto-progresses
- [ ] Timers work correctly

##### 3. Special Question Types
- [ ] Secret (cat) questions work
- [ ] Stake questions work
- [ ] Auction questions work
- [ ] No-risk questions work
- [ ] Final round works
- [ ] Theme deletion works

##### 4. Player Management
- [ ] Add player works
- [ ] Remove player works
- [ ] Player scores update
- [ ] Chooser assignment works
- [ ] Player states sync

##### 5. Timer Synchronization
- [ ] Round timer starts/stops correctly
- [ ] Question timer starts/stops correctly
- [ ] Thinking timer works correctly
- [ ] Timer values are accurate
- [ ] Multiple timers can run simultaneously

##### 6. Edge Cases
- [ ] Empty package handling
- [ ] Single round package
- [ ] Single theme round
- [ ] Single question theme
- [ ] Network message delays (simulate with Task.Delay)
- [ ] Rapid player actions
- [ ] Server pause/resume

### Phase 6: Documentation Updates

#### Update Files
1. **README.md**: Add notes about UseSIGameEngine mode
2. **AGENTS.md**: Update architecture description
3. **Release Notes**: Document migration and changes
4. **User Documentation**: Explain managed mode limitations

#### Migration Guide for Users
Create `MIGRATION_GUIDE.md`:
- Explain what changed
- List removed features
- Explain benefits (reduced bugs, better server sync)
- Provide troubleshotion tips
- Show how to switch back to old mode if needed

## Implementation Checklist

### Core Implementation
- [ ] Complete all `GameController` handler implementations
- [ ] Complete all `NewGameActions` implementations
- [ ] Update `MainViewModel.CreateGameNewAsync()` to properly initialize
- [ ] Remove commented-out code once new mode is verified
- [ ] Add error handling for all message handlers
- [ ] Implement timer synchronization logic
- [ ] Implement table synchronization logic
- [ ] Implement player state synchronization

### UX Features
- [ ] Map all essential game events to GameViewModel
- [ ] Ensure timers display correctly
- [ ] Ensure scores update correctly
- [ ] Ensure chooser highlights correctly
- [ ] Ensure content displays correctly
- [ ] Add replic display support
- [ ] Handle all special question types

### Settings and Controls
- [ ] Update settings UI with tooltip
- [ ] Disable unavailable controls in managed mode
- [ ] Add mode indicator in UI
- [ ] Preserve backward compatibility with old mode

### Testing
- [ ] Test Classic mode end-to-end
- [ ] Test Simple mode end-to-end
- [ ] Test all special question types
- [ ] Test player management
- [ ] Test timer synchronization
- [ ] Test edge cases
- [ ] Performance testing
- [ ] Compare UX with old mode

### Documentation
- [ ] Update README
- [ ] Update AGENTS.md
- [ ] Create MIGRATION_GUIDE.md
- [ ] Update inline code comments
- [ ] Update XML documentation
- [ ] Create release notes

## Benefits of Migration

### For Developers
1. **Reduced Code Duplication**: Game logic exists only in SICore
2. **Easier Maintenance**: Single source of truth for game rules
3. **Better Testing**: Can test with same tools as SIGame
4. **Consistent Behavior**: SImulator and SIGame work identically
5. **Future Features**: Automatically get new SICore features

### For Users
1. **More Reliable**: Fewer bugs from duplicated logic
2. **Better Compatibility**: Packages play same as in SIGame
3. **Future Features**: Automatic access to new game modes
4. **Better Server Integration**: Could enable online features later

## Risks and Mitigation

### Risk: Feature Removal (Backward Navigation)
**Impact**: Power users lose manual control
**Mitigation**: 
- Keep old mode available via setting
- Document limitation clearly
- Propose feature addition to SICore if users request it

### Risk: Timer Synchronization Issues
**Impact**: Timers might not match expected behavior
**Mitigation**:
- Thorough testing of all timer scenarios
- Add logging for timer events
- Allow manual timer adjustment if needed

### Risk: Message Processing Delays
**Impact**: UI updates might lag
**Mitigation**:
- Optimize message handlers
- Use async processing where appropriate
- Test with simulated network delays

### Risk: Breaking Changes
**Impact**: Existing saved games might not work
**Mitigation**:
- Keep old mode available
- Version check on game load
- Clear migration path in UI

## Timeline Estimate

- **Phase 1** (Preparation): 1-2 days
- **Phase 2** (Core Implementation): 3-5 days
- **Phase 3** (UX Preservation): 3-4 days
- **Phase 4** (Settings/UI): 1-2 days
- **Phase 5** (Testing): 3-5 days
- **Phase 6** (Documentation): 1-2 days

**Total**: 12-20 days (2-4 weeks)

## Success Criteria

1. All game modes work correctly in managed mode
2. User experience is comparable to old mode
3. No critical features are lost
4. Performance is acceptable
5. All tests pass
6. Documentation is complete
7. Users can switch between modes if needed

## Future Enhancements

### Short Term
1. Add better visual feedback for server-controlled events
2. Improve replic display system
3. Add animation for timer sync
4. Better error messages for managed mode limitations

### Long Term
1. Request backward navigation feature in SICore
2. Add game state snapshot/restore to SICore
3. Enable online multiplayer via SICore
4. Add spectator mode
5. Add game replay functionality

## Conclusion

This migration plan provides a comprehensive path from the current SIEngine-based implementation to the new SICore-based managed mode. While some manual control features will be lost, the benefits of reduced code duplication, improved reliability, and better compatibility with SIGame make this migration worthwhile.

The phased approach allows for iterative development and testing, while the hybrid mode strategy provides a safety net for users who need the old functionality. With thorough implementation of timer and table synchronization, the user experience should remain very similar to the current implementation.
