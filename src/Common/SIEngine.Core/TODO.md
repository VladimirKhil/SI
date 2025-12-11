# SIEngine.Core TODOs

This document lists identified issues and future improvements for the SIEngine.Core project.

## Known Issues

### 1. MoveToAnswer() Implementation
**Status:** Known limitation  
**Priority:** Medium  
**Description:**  
The current `MoveToAnswer()` implementation simply jumps to the first step after the last `AskAnswer` step. This can cause issues if there are important content items that need to be processed before displaying the answer.

**Current Behavior:**
```csharp
// Jumps directly to answer display step
_stepIndex = askAnswerStepIndex + 1;
```

**Desired Behavior:**
MoveToAnswer() should play all remaining steps in "fast mode" rather than skipping them. This would:
- Process all content items to ensure proper state
- For content with the same placement/layout, only show the final state
- Guarantee that all necessary question parts are ready when displaying the answer
- Maintain consistency between normal play and fast-forward

**Impact:**
- Low for most questions (simple text/image answers work fine)
- Medium for complex questions with multiple answer content items
- Could cause visual glitches if answer depends on previous content state

**Suggested Fix:**
Add a "fast mode" flag to PlayNext() or create a PlayNextFast() internal method that:
1. Processes all steps normally
2. Aggregates content by placement
3. Only invokes handlers for final state of each placement
4. Ensures all callbacks and state updates happen

### 2. QuestionTypeName Property
**Status:** Deprecated  
**Priority:** Low  
**Description:**  
The `QuestionTypeName` property in `IQuestionEngine` is obsolete and should be removed in a future major version.

**Current Usage:**
```csharp
public string QuestionTypeName { get; } = "";
```

**Reason for Deprecation:**
- Legacy property from older format
- Not used by modern question scripts
- Adds confusion to the API

**Migration Path:**
1. Mark as `[Obsolete]` with appropriate message (✅ DONE)
2. Add analyzer warning in next minor version
3. Remove in next major version
4. Update all consumers to use Script-based approach

**Breaking Change:** Yes - will require major version bump

### 3. False Start Edge Cases
**Status:** Needs investigation  
**Priority:** Low  
**Description:**  
False start handling (`FalseStartHelper.GetAskAnswerStartIndex`) may not handle all edge cases correctly, particularly with:
- Questions with no multimedia content but FalseStartMode.TextContentOnly
- Questions with mixed content types in unexpected orders
- Questions where content is added dynamically via parameters

**Tests Needed:**
- ✅ Text-only with TextContentOnly mode
- ⚠️ Image-only with TextContentOnly mode
- ⚠️ Mixed content with delays
- ⚠️ Content added via script parameters

### 4. Parameter Reference Resolution
**Status:** Working, but could be improved  
**Priority:** Low  
**Description:**  
Current parameter reference resolution (`TryGetParameter`) is basic and doesn't handle:
- Circular references (could cause infinite loop)
- Deep reference chains (ref -> ref -> ref)
- Missing reference validation

**Example Issue:**
```csharp
// This would fail silently
step.Parameters["content"] = new StepParameter { IsRef = true, SimpleValue = "param1" };
question.Parameters["param1"] = new StepParameter { IsRef = true, SimpleValue = "param2" };
// param2 doesn't exist
```

**Suggested Improvement:**
- Add reference depth limit
- Detect circular references
- Better error reporting for missing refs
- Consider caching resolved parameters

## Future Enhancements

### 1. Content Aggregation
**Priority:** Medium  
**Description:**  
Implement intelligent content aggregation for better UX:
- Group consecutive content items with same placement
- Optimize transitions between content types
- Support content preloading for smoother playback

### 2. State Persistence
**Priority:** Low  
**Description:**  
Add ability to serialize/deserialize engine state for:
- Pause/resume functionality
- Save game state
- Network synchronization
- Replay/review functionality

**Suggested API:**
```csharp
public interface IQuestionEngine
{
    QuestionEngineState GetState();
    void RestoreState(QuestionEngineState state);
}
```

### 3. Event Hooks
**Priority:** Low  
**Description:**  
Add optional event hooks for better debugging and telemetry:
- OnStepStart/OnStepComplete events
- Performance metrics (step duration, total time)
- State change notifications
- Error/warning events

### 4. Async Support
**Priority:** Low  
**Description:**  
Consider async version of PlayNext() for:
- Async content loading
- Network-based content
- Database lookups
- Better cancellation support

**Challenges:**
- Would be breaking change
- Complicates state machine logic
- Need to maintain backward compatibility

## Performance Optimizations

### 1. Script Compilation
**Priority:** Low  
**Description:**  
Pre-compile scripts into optimized execution plan:
- Resolve all parameter references upfront
- Build step index for fast navigation
- Validate script structure once
- Cache frequently accessed data

### 2. Content Caching
**Priority:** Low  
**Description:**  
Cache resolved content items to avoid repeated parameter lookups and conversions.

## Documentation Improvements

### 1. More Examples
**Priority:** Medium  
**Additions needed:**
- ✅ Simple text question (DONE)
- ✅ Multiple content items (DONE)
- ✅ Select answer type (DONE)
- ⚠️ Stake/auction questions
- ⚠️ Complex multi-step questions
- ⚠️ Custom question types

### 2. Handler Implementation Guide
**Priority:** Medium  
**Description:**  
Create comprehensive guide for implementing `IQuestionEnginePlayHandler`:
- ✅ Basic implementation (DONE)
- ⚠️ State management best practices
- ⚠️ Error handling patterns
- ⚠️ Performance considerations
- ⚠️ Testing strategies

### 3. Migration Guide
**Priority:** Low  
**Description:**  
For users migrating from old question format:
- Old format vs new format comparison
- Step-by-step migration process
- Common pitfalls
- Automated migration tools

## Testing Improvements

### 1. Additional Test Coverage
**Tests needed:**
- ✅ Basic flow (DONE)
- ✅ Multiple content (DONE)
- ✅ Answer types (DONE)
- ✅ MoveToAnswer (DONE)
- ✅ Preambula steps (DONE)
- ✅ Edge cases (DONE)
- ⚠️ Concurrent engine instances
- ⚠️ Handler exception handling
- ⚠️ Performance/stress tests
- ⚠️ All question types from ScriptsLibrary

### 2. Integration Tests
**Priority:** Medium  
**Description:**  
Add integration tests with real package files:
- Load actual .siq packages
- Test with production question data
- Verify backward compatibility
- Test with various package versions

### 3. Property-Based Tests
**Priority:** Low  
**Description:**  
Use property-based testing to verify invariants:
- Engine always terminates
- State transitions are valid
- Handler methods called in correct order
- All content is eventually displayed

## Security Considerations

### 1. Resource Limits
**Priority:** Medium  
**Description:**  
Add safeguards against malicious or malformed questions:
- ✅ Maximum script steps (handled by PackageLimits)
- Maximum content items per step
- Maximum parameter depth
- Execution time limits

### 2. Content Sanitization
**Priority:** Low  
**Description:**  
Ensure all content is properly sanitized before passing to handlers:
- HTML content escaping
- Path traversal prevention in file refs
- URL validation for external resources

## Maintainability

### 1. Code Organization
**Priority:** Low  
**Suggestions:**
- Extract step processing into separate classes (Strategy pattern)
- Move parameter resolution to dedicated service
- Separate concerns between state management and handler invocation

### 2. Logging
**Priority:** Low  
**Description:**  
Add structured logging for:
- Step execution trace
- Parameter resolution
- Handler invocations
- Performance metrics
- Error conditions

---

## Legend
- ✅ - Completed or addressed
- ⚠️ - Needs attention
- ❌ - Blocked or cannot fix

## Notes
- This document should be reviewed and updated regularly
- Priority levels are subjective and may change based on user feedback
- Breaking changes should only be made in major version releases
