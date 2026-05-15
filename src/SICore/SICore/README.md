# SICore Navigation Guide

`SICore` is the high-level runtime layer that turns package engine events into a full game session with players, showman, viewers, network messages, timers, and application-facing orchestration.

## What lives here

This area is the best starting point when the task is about:

- game session creation
- round and question orchestration
- player/showman/viewer behavior
- answer validation flow at runtime
- timers and in-session state
- runtime messages sent to connected clients

## Primary entry points

### Create and wire a game

- `GameRunner.cs`

This is the main composition point for creating a runtime game. It wires together:

- runtime state (`GameData`)
- play handlers
- the lower-level engines
- network clients
- the main runtime controller

### Main game runtime hotspots

- `Clients/Game/GameController.cs`
- `Clients/Game/Game.cs`
- `Clients/Viewer/Viewer.cs`
- `Services/Intelligence.cs`

These are important files, but they are also the largest navigation hotspots in this area.

## Folder map

- `Attributes` — attributes used by runtime code
- `Clients` — runtime participants and role-specific logic
- `Contracts` — interfaces used across the runtime
- `Data` — supporting runtime data structures
- `Extensions` — extension methods
- `Helpers` — helper classes with mixed responsibilities
- `Models` — small domain and state model types
- `PlatformSpecific` — platform-dependent runtime code
- `Properties` — generated resources and metadata
- `Results` — result/report model types
- `Services` — higher-level service classes
- `Special` — specialized or less common runtime objects
- `Utils` — generic helper utilities

## `Clients` folder guide

### `Clients/Game`

The main session/game orchestration area.

Start here for:

- round flow
- question flow
- chooser logic
- answer handling
- state updates during play

Important files:

- `GameController.cs` — central runtime coordinator
- `Game.cs` — high-level game object/runtime participant
- `GameData.cs` — central session state container
- `GameActions.cs` — outgoing actions and message helpers
- `PlayHandler.cs` — game-level engine callbacks
- `QuestionPlayHandler.cs` — question-level engine callbacks
- `QuestionPlayState.cs` — compact state object for current question

### `Clients/Viewer`

Viewer-side runtime participant behavior.

Start here for:

- viewer interaction/state
- person actions from a viewer perspective
- person controllers used by connected participants

Important files:

- `Viewer.cs`
- `PersonState.cs`
- `PersonActions.cs`
- `PersonComputerController.cs`
- `IPersonController.cs`

### `Clients/Player`

Player-specific runtime participant behavior.

Important files:

- `Player.cs`
- `PlayerComputerController.cs`

### `Clients/Showman`

Showman-specific participant behavior.

Important files:

- `Showman.cs`
- `ShowmanComputerController.cs`

### `Clients/Other`

Shared person/account types and AI helpers.

Look here after checking the role-specific folders.

## Where to change X

### Start or compose a game session

Start with:

- `GameRunner.cs`
- `Clients/Game/GameData.cs`
- `Contracts/IGameHost.cs`

### Runtime question behavior

Start with:

- `Clients/Game/QuestionPlayHandler.cs`
- `Clients/Game/QuestionPlayState.cs`
- `Clients/Game/GameController.cs`

### Runtime round/game flow

Start with:

- `Clients/Game/GameController.cs`
- `Clients/Game/PlayHandler.cs`
- `Clients/Game/Game.cs`

### Messages and message codes

Start with:

- `Messages.cs`
- `ReplicCodes.cs`
- `MessageParams.cs`
- `Models/MessageCode.cs`

Also see:

- `GAME_AGENT_DOCUMENTATION.md`

### AI or computer player behavior

Start with:

- `Services/Intelligence.cs`
- `Clients/Other/AI/*`
- `Clients/Player/PlayerComputerController.cs`
- `Clients/Showman/ShowmanComputerController.cs`

### Validation, answer types, and question state

Start with:

- `Clients/Game/QuestionPlayState.cs`
- `Models/AnswerType.cs`
- `Models/AnswerResult.cs`
- `Models/QuestionStats.cs`

## Useful companion docs

- `GAME_AGENT_DOCUMENTATION.md` — runtime protocol/message documentation
- `../../Common/SIEngine/DOCUMENTATION.md` — package-level engine documentation
- `../../Common/SIEngine.Core/DOCUMENTATION.md` — question-level engine documentation

## Practical navigation advice

- Start with `GameRunner.cs` when you need to understand how pieces are wired together.
- Prefer `GameData`, `QuestionPlayState`, and small model files for understanding state before reading the largest controllers.
- Search `Clients/Game` first for gameplay issues.
- Search `Messages.cs` and the protocol guide first for runtime messaging changes.
- Treat `GameController.cs` and `Game.cs` as hotspots: search targeted terms inside them instead of reading linearly.

## Files usually safe to ignore first

- `Properties/Resources.Designer.cs`
- `obj/`
- `bin/`
