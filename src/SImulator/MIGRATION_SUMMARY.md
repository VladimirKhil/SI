# SImulator Migration to UseSIGameEngine - Quick Summary

This document provides a quick reference for the migration from SIEngine to UseSIGameEngine (SICore).

For the complete detailed plan, see [MIGRATION_PLAN.md](./MIGRATION_PLAN.md).

## What's Changing

### Old Approach (SIEngine)
- **Pattern**: Direct engine control with handler callbacks
- **Control**: Client controls timers, table, and flow
- **Navigation**: Full backward/forward navigation
- **Components**: GameEngine, QuestionEngine, GameEngineController, GameActions

### New Approach (SICore with UseSIGameEngine)
- **Pattern**: Person controller with server messages
- **Control**: Server manages timers, table, and flow (managed mode)
- **Navigation**: Forward only (backward not available)
- **Components**: GameRunner, GameController (IPersonController), ViewerActions, ViewerData

## Key Benefits

✅ **Eliminates code duplication** - Game logic exists only in SICore  
✅ **Better reliability** - Fewer bugs from duplicated logic  
✅ **Consistent behavior** - Same as SIGame  
✅ **Future features** - Automatic access to new SICore features  
✅ **Better testing** - Can use same test infrastructure as SIGame  

## Key Challenges

⚠️ **No backward navigation** - Will be removed or made optional  
⚠️ **Server-controlled timers** - Need event synchronization  
⚠️ **Server-controlled table** - Need state synchronization  
⚠️ **Limited manual control** - Acceptable trade-off for reliability  

## Migration Strategy

### 1. Timer Synchronization
Listen to `OnTimerChanged()` events and map to local timer display:
- Timer 0 = Round timer
- Timer 1 = Question timer  
- Timer 2 = Thinking/Decision timer

### 2. Table Synchronization
Use `TableLoaded()` to receive initial table, listen for updates via messages.

### 3. Content Flow
Map `OnContent()` to existing presentation controller, let server control timing.

### 4. Player Management
Use `ViewerActions.AddTable()` / `RemoveTable()` instead of direct manipulation.

### 5. Navigation Controls
- **Forward**: `viewerActions.Move()` ✅
- **Backward**: Not available ❌ (remove or keep old mode)
- **Round skip**: Not available ❌ (remove or keep old mode)

### 6. Special Questions
Server handles stakes, answerer selection, theme deletion - client just displays.

## Implementation Phases

1. **Preparation** (1-2 days)
   - Review existing code
   - Map all handlers
   - Identify removals

2. **Core Implementation** (3-5 days)
   - Complete GameController
   - Complete NewGameActions
   - Update MainViewModel

3. **UX Preservation** (3-4 days)
   - Timer sync
   - Table sync
   - Content flow
   - Player states

4. **Settings/UI** (1-2 days)
   - Update controls
   - Add tooltips
   - Hybrid mode

5. **Testing** (3-5 days)
   - All game modes
   - Special questions
   - Edge cases
   - Performance

6. **Documentation** (1-2 days)
   - Update docs
   - Migration guide
   - Release notes

**Total**: 12-20 days (2-4 weeks)

## Testing Checklist

### Must Test
- [ ] Classic (TV) mode - player selection
- [ ] Simple (Sport) mode - sequential play
- [ ] Secret (cat) questions
- [ ] Stake/Auction questions
- [ ] Final round with theme deletion
- [ ] Timer synchronization
- [ ] Player score updates
- [ ] Content display (text, image, audio, video)
- [ ] Button press timing
- [ ] Answer validation

### Nice to Test
- [ ] Edge cases (empty rounds, single questions, etc.)
- [ ] Performance with large packages
- [ ] Rapid user actions
- [ ] Network message delays

## Success Criteria

✅ All game modes work in managed mode  
✅ User experience comparable to old mode  
✅ No critical features lost  
✅ All tests pass  
✅ Documentation complete  
✅ Users can switch modes if needed  

## Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| Feature removal | Keep old mode available via setting |
| Timer sync issues | Thorough testing, add logging |
| Message delays | Optimize handlers, test with delays |
| Breaking changes | Version check, clear migration path |

## Hybrid Mode Strategy (Recommended)

Keep both modes available:
- **New mode (UseSIGameEngine=true)**: Default, managed, reliable
- **Old mode (UseSIGameEngine=false)**: Fallback for power users

Benefits:
- Safety net during migration
- Power users keep manual control
- Can compare behavior side-by-side
- Gradual migration path

## Quick Reference: Handler Mapping

| Old (GameEngineController) | New (GameController) | Notes |
|----------------------------|----------------------|-------|
| `OnPackage()` | `OnPackage()` | Direct map |
| `OnRound()` | `OnStage()` | Map stage events |
| `OnRoundThemes()` | `TableLoaded()` | Table from server |
| `OnQuestionContent()` | `OnContent()` | Content from server |
| `OnButtonPressStart()` | `Try()` | Button enabled |
| `OnSimpleRightAnswerStart()` | `OnRightAnswer()` | Answer display |
| `AskForQuestionSelection()` | `SelectQuestion() + ShowTablo()` | Split into two |
| `OnSetAnswerer()` | `OnSelectPlayer()` | Server controls |
| `OnSetPrice()` | Handled by server | Via stake messages |
| Timer start/stop | `OnTimerChanged()` | Listen to events |
| Table updates | `TableLoaded()` | Listen to events |
| Player scores | Listen to SUMS | Via messages |

## Quick Reference: Actions Mapping

| Old (GameActions) | New (NewGameActions) | Notes |
|-------------------|----------------------|-------|
| `MoveNext()` | `viewerActions.Move()` | Forward only |
| `MoveBack()` | Not available | Remove feature |
| `MoveNextRound()` | Not available | Remove feature |
| `MoveBackRound()` | Not available | Remove feature |
| `IsRightAnswer()` | Server handles | Automatic |
| `AddPlayer()` | `viewerActions.AddTable()` | Via server |
| `RemovePlayerAt()` | `viewerActions.RemoveTable()` | Via server |
| `ShowThemes()` | Server handles | Automatic |

## Common Patterns

### Pattern 1: Timer Event Handling
```csharp
public void OnTimerChanged(int timerIndex, string command, string arg, string? person = null)
{
    switch (timerIndex)
    {
        case 0: HandleRoundTimer(command, arg); break;
        case 1: HandleQuestionTimer(command, arg); break;
        case 2: HandleThinkingTimer(command, arg); break;
    }
}
```

### Pattern 2: Content Display
```csharp
public void OnContent(string placement, List<ContentInfo> content)
{
    var contentItems = content.Select(MapToContentItem).ToList();
    GameViewModel.ContentItems = contentItems;
    GameViewModel.PresentationController.OnQuestionContent(contentItems, ...);
}
```

### Pattern 3: Player State Update
```csharp
public void OnPlayerOutcome(int playerIndex, bool isRight)
{
    var player = GameViewModel.Players[playerIndex];
    player.State = isRight ? PlayerState.Right : PlayerState.Wrong;
    GameViewModel.PresentationController.ShowPlayerOutcome(playerIndex, isRight);
}
```

## Next Steps

1. Read [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) for complete details
2. Review existing `GameController.cs` implementation
3. Start with Phase 1: Complete stub implementations
4. Test incrementally as you implement each handler
5. Use hybrid mode during development
6. Switch default to new mode once stable

## Questions?

See the detailed plan in [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) which includes:
- Complete architecture comparison
- Detailed code examples
- Edge case handling
- Testing strategies
- Risk analysis
- Timeline estimates
- Future enhancements

---

**Document Version**: 1.0  
**Last Updated**: 2024-12-18  
**Status**: Planning Complete - Ready for Implementation
