# Codebase Navigability Report

## Summary

The repository is **moderately easy to navigate**, with a generally logical project and folder structure, and its documentation support is now noticeably better after the addition of repository-level and area-level navigation guides. There are still important weaknesses that slow down both human contributors and LLM-based agents.

The strongest parts are:

- clear top-level separation between shared libraries, core logic, and app-specific code
- meaningful project names
- mostly sensible folder grouping inside projects
- several useful technical documentation files for engine and protocol behavior

The main weaknesses are:

- a few very large handwritten core files that concentrate too much behavior
- some broad folder names that are less helpful for discovery
- several key runtime areas still rely on giant files rather than smaller workflow-oriented units
- documentation now helps substantially, but code-level ownership is still uneven in the largest hotspots

## High-level structure

Top-level repository folders:

- `.github`
- `assets`
- `bin`
- `deploy`
- `obj`
- `src`
- `test`
- `tools`
- `web`

Top-level source folders:

- `src/Common`
- `src/SICore`
- `src/SIGame`
- `src/SImulator`
- `src/SIQuester`

This is a good start. The structure gives a reasonable mental model:

- `Common` contains reusable/shared libraries
- `SICore` contains core game/session logic
- `SIGame` contains a client application
- `SImulator` and `SIQuester` are separate app areas
- `test` mirrors product areas well enough to be useful

## Project-level navigability

The solution is split into many focused projects, which helps navigation. Relevant examples:

- `src/Common/SIEngine.Core/SIEngine.Core.csproj`
- `src/Common/SIEngine/SIEngine.csproj`
- `src/SICore/SICore/SICore.csproj`
- `src/SIGame/SIGame/SIGame.csproj`
- multiple corresponding test projects under `test/...`

This makes it fairly easy to answer questions such as:

- Where is the question engine? → `SIEngine.Core`
- Where is package/game orchestration? → `SIEngine`
- Where is game session logic and networking? → `SICore`
- Where is WPF app code? → `SIGame`

Verdict: **project structure is good and supports navigation well**.

## Folder structure quality

### Good examples

Inside `src/SICore/SICore`, the structure is mostly understandable:

- `Attributes`
- `Clients`
- `Contracts`
- `Data`
- `Extensions`
- `Helpers`
- `Models`
- `PlatformSpecific`
- `Properties`
- `Results`
- `Services`
- `Special`
- `Utils`

Inside `Clients`, the role-based split is useful:

- `Clients/Game`
- `Clients/Player`
- `Clients/Viewer`
- `Clients/Showman`
- `Clients/Other`

This is a sensible layout for domain discovery.

### Weaker areas

Some folder names are too broad to be strong navigation aids:

- `Helpers`
- `Utils`
- `Special`
- `Other`

These names are common, but they do not tell contributors much about responsibility boundaries. They usually mean that people need to open files before understanding ownership.

Verdict: **folder structure is mostly logical, but some areas are overly generic**.

## File size and local discoverability

### Good

Many files are compact and focused. For example, the current file:

- `src/SICore/SICore/Clients/Game/QuestionPlayState.cs`

is small enough to understand quickly and has a clear responsibility.

This pattern is good for both humans and LLMs:

- small state containers
- focused models
- dedicated contracts
- narrow utility classes

### Main problem: oversized core files

Largest handwritten logic files found include:

- `src/SICore/SICore/Clients/Game/GameController.cs` — 4184 lines
- `src/SICore/SICore/Clients/Game/Game.cs` — 3022 lines
- `src/SICore/SICore/Clients/Viewer/Viewer.cs` — 1837 lines
- `src/SICore/SICore/Services/Intelligence.cs` — 1390 lines

Across the wider repository, there are also several very large app/viewmodel/XAML files, but the most serious navigability issue is the concentration of important runtime logic in very large handwritten files.

### Impact of large files

These files make it harder to:

- locate the right edit point quickly
- understand ownership of behavior
- predict side effects of changes
- review code safely
- give LLMs enough context without pulling in huge file segments

A large file can still be maintainable if it is sharply structured, but files above roughly 1500–2000 lines usually become expensive to navigate. Files above 3000–4000 lines are especially costly.

Verdict: **file size is the main navigability problem in the repository**.

## Documentation usefulness

Helpful documentation files found:

- `README.md`
- `ARCHITECTURE.md`
- `src/SICore/SICore/README.md`
- `src/SIGame/README.md`
- `src/SIQuester/README.md`
- `src/SImulator/README.md`
- `src/Common/SIEngine.Core/DOCUMENTATION.md`
- `src/Common/SIEngine/DOCUMENTATION.md`
- `src/SICore/SICore/GAME_AGENT_DOCUMENTATION.md`
- `src/Common/SIPackages/README.md`

### Strengths of current docs

The existing technical docs are useful and better than average. They explain:

- engine responsibilities
- state machine behavior
- handler sequences
- messaging/protocol flow
- repository structure and app/library ownership
- major entry points
- per-area folder maps
- common "where to change X" scenarios

This is valuable because it gives readers a conceptual model instead of only API details.

### Remaining gaps in current docs

The docs are now much better at explaining **where relevant code lives**. The remaining gap is that some of the most important code paths still terminate in oversized implementation files, so documentation can point to the area accurately but cannot fully remove the cost of navigating within those files.

What still seems missing:

- more file-level ownership notes inside the largest gameplay/runtime hotspots
- folder-local guides for especially dense subareas such as `Clients/Game`
- tighter linkage between docs and tests for common workflows
- maintenance discipline to keep navigation docs current as files move or responsibilities split

Examples of unanswered questions a new contributor may still have:

- Which regions inside `GameController.cs` own each stage of the gameplay lifecycle?
- Which parts of `Game.cs` versus `GameController.cs` are authoritative for a given runtime concern?
- Where should new runtime features be placed to avoid making the large files worse?
- Which tests best cover each documented workflow?

Verdict: **documentation is now a real strength of the repository, but the largest files still limit practical discoverability**.

## Ease of finding relevant parts

### For humans

Typical experience is likely:

1. identify the correct project fairly quickly
2. identify a probable folder fairly quickly
3. spend more time than necessary inside one or two large files

So the repository is:

- easy at the project level
- mostly okay at the folder level
- inconsistent at the file level

### For LLMs

The repository is usable, but not optimized.

LLMs navigate best when:

- responsibilities are split into smaller files
- filenames clearly reflect business behavior
- architecture docs explain entry points and ownership
- workflows are documented with domain terms

This repository already has some good domain separation, but giant controller/orchestrator files reduce precision and increase context cost.

Verdict: **easy to orient broadly, harder to pinpoint exact edit locations in core gameplay code**.

## Suggestions to improve navigability for humans and LLMs

## 1. Split giant core orchestration files

Highest-value targets:

- `src/SICore/SICore/Clients/Game/GameController.cs`
- `src/SICore/SICore/Clients/Game/Game.cs`
- `src/SICore/SICore/Clients/Viewer/Viewer.cs`
- `src/SICore/SICore/Services/Intelligence.cs`

Possible extraction boundaries:

- question lifecycle
- round lifecycle
- message formatting/sending
- answer validation
- timers
- player/chooser logic
- final-round logic
- appeals/appellations
- media completion handling

This would be the single biggest improvement.

## 2. Add a root repository architecture document

Recommended file:

- `README.md` or `ARCHITECTURE.md`

Status:

- completed via `README.md` and `ARCHITECTURE.md`

It should briefly answer:

- what each top-level app/library does
- where execution starts in major apps
- where game flow lives
- where question flow lives
- where networking/protocol logic lives
- where tests are located

This would help both onboarding and automated code assistance.

## 3. Add per-project navigation guides

Especially for:

- `SICore`
- `SIGame`
- `SIQuester`

Status:

- completed for `SICore`, `SIGame`, `SIQuester`, and `SImulator`

Recommended content:

- main entry points
- important workflows
- state containers
- main controllers/services
- key extension points
- common change scenarios

## 4. Reduce use of vague folder buckets

Folders like `Helpers`, `Utils`, `Special`, and `Other` should be used sparingly.

Prefer responsibility-oriented names such as:

- `QuestionFlow`
- `Messaging`
- `Validation`
- `Scoring`
- `Selection`
- `Timers`

Even if namespaces stay stable for compatibility, folder layout can still become more explanatory.

## 5. Add “where to change X” sections to docs

Examples:

- question playback
- answer validation
- round selection
- viewer messaging
- score calculation
- media completion

This is low effort and high value for both humans and LLMs.

Status:

- completed at repository and area-guide level

## 6. Preserve and expand small focused state/model files

Files like `QuestionPlayState.cs` are a good pattern:

- clear purpose
- low cognitive load
- easy to search
- easy to reuse in reasoning

The more behavior that can be moved out of giant controllers into focused state/services, the easier future navigation becomes.

## 7. Distinguish generated code from hand-written code in docs and navigation guidance

Generated and resource-heavy files are large but less important for normal feature work.

Examples include:

- `Properties/Resources.Designer.cs`
- generated `obj/...` files
- very large resource XAML dictionaries

Repository docs should explicitly say contributors can usually ignore:

- `bin/`
- `obj/`
- generated designer files

unless they are working on build/resource issues.

## 8. Use workflow-oriented terms in file and doc naming

The repository already has some good names, but broad names such as `Game.cs` or `Viewer.cs` force more inspection.

More specific names improve both human search and LLM retrieval, especially when aligned to domain flows.

## Practical verdict

### Strengths

- strong project-level separation
- mostly logical folder hierarchy
- useful domain/engine documentation
- repository-level architecture and navigation documentation now exists
- per-area navigation guides now exist for major app/runtime areas
- many small focused model/state files
- test projects exist for major areas

### Weaknesses

- very large core handwritten files
- some vague bucket folders
- code ownership remains diffuse inside the largest files
- docs are ahead of the code structure: documentation is clearer than some implementation boundaries

## Final assessment

The codebase is **organized enough to work in productively**, and it is now **better documented for navigation**, but it is **not yet optimized for fast navigation**.

The primary issue is not the top-level structure or the availability of docs anymore. The main remaining problem is that important behavior is concentrated in a few oversized files, which raises the cost of understanding and modifying the system.

If the goal is to improve navigability for both humans and LLMs, the most effective sequence is:

1. split the biggest core orchestration files
2. add folder-local guides for the densest subareas
3. reduce vague folder naming over time
4. connect workflows to relevant tests and examples
5. keep the new navigation docs current as code is refactored
