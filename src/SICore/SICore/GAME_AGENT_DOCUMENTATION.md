# SICore Game Agent Developer Guide

## Overview

This document describes the messaging protocol used by the SICore game agent for communication between the game server and clients (players, showman, viewers). It is intended to help developers create custom game clients that can interact with the SICore game engine.

**SICore** is the core game management system that orchestrates quiz game sessions. It handles:
- Game state management and flow control
- Player, showman, and viewer connections
- Question playback coordination
- Scoring and validation
- Network message distribution

## Architecture

### Components

- **Game Server** - The central coordinator running SICore game logic
- **Game Agent** - The server-side component that manages game state and sends messages
- **Clients** - Connected participants:
  - **Showman** - Game host who validates answers and controls flow
  - **Players** - Participants who answer questions
  - **Viewers** - Spectators who observe the game

### Message Flow

Messages flow from the Game Agent to clients via a network layer. Messages are text-based with arguments separated by the `\n` character (referred to as `ArgsSeparator`).

```
Game Agent --> Network Layer --> Clients (Showman/Players/Viewers)
```

### Message Format

Messages follow this structure:
```
MESSAGE_TYPE\nARG1\nARG2\n...\nARGN
```

Example:
```
STAGE\nRound 1
SUMS\n0\n0\n0
```

## Message Recipients

Messages can be sent to different recipients:

- **Broadcast** (`NetworkConstants.Everybody`) - All connected clients
- **Specific person** - Individual player, showman, or viewer by name
- **Role-based** - All players, all viewers, etc.

## Core Message Categories

Messages are organized into categories based on their purpose:

### 1. Game Initialization Messages
### 2. Round and Theme Messages
### 3. Question Flow Messages
### 4. Answer and Validation Messages
### 5. Scoring Messages
### 6. Control Messages
### 7. Content Display Messages
### 8. Timer Messages
### 9. Error and User Feedback Messages

---

## Complete Game Flow

### Game Start Sequence

When a game begins, the following message sequence is sent to all clients:

#### 1. Connection and Setup
```
CONNECTED\n<player_name>\n<role>
INFO2\n<game_state_data>
```

**Recipients:** All clients
**Purpose:** Notifies clients about new connections and provides initial game state

#### 2. Game Metadata
```
GAMEMETADATA\n<game_name>\n<package_name>\n<contact_uri>
```

**Recipients:** All clients
**Purpose:** Provides information about the game session and package

#### 3. Game Options
```
OPTIONS2\n<key1>\n<value1>\n<key2>\n<value2>\n...
```

**Recipients:** All clients
**Purpose:** Transmits game configuration options (button blocking time, false starts, etc.)

**Common options:**
- `falseStart` - Whether false starts are enabled (+/-)
- `buttonBlockingTime` - Time in 1/10 seconds to block button after wrong answer
- `useApellations` - Whether answer appeals are enabled (+/-)
- `displaySources` - Whether to show question sources (+/-)
- `displayAnswerOptions` - Whether to show answer options (multiple choice)

#### 4. Package Information

```
STAGE\nBeforeGame
PACKAGEID\n<package_id>
ROUNDSNAMES\n<round1>\n<round2>\n...\n<roundN>
PACKAGE\n<package_name>
```

**Recipients:** All clients
**Purpose:** Informs clients about the package structure

**Details:**
- `STAGE` indicates game phase (BeforeGame, Round, After)
- `PACKAGEID` is deprecated but still sent for compatibility
- `ROUNDSNAMES` lists all round names in the package
- `PACKAGE` announces the package display name

Optional package metadata (if available):
```
PACKAGE_AUTHORS\n<author1>\n<author2>\n...
PACKAGE_DATE\n<date>
PACKAGE_SOURCES\n<source1>\n<source2>\n...
PACKAGE_COMMENTS\n<comments>
```

#### 5. Game Themes (Optional)
```
GAMETHEMES\n<theme1>\n<theme2>\n...\n<themeN>
```

**Recipients:** All clients (if enabled in game rules)
**Purpose:** Shows all themes across all rounds before game starts

#### 6. Initial Player Sums
```
SUMS\n<player1_sum>\n<player2_sum>\n...\n<playerN_sum>
```

**Recipients:** All clients
**Purpose:** Displays starting scores (typically all zeros)

---

### Round Start Sequence

When a new round begins:

#### 1. Stage Transition
```
STAGE\n<round_name>
STAGE_INFO\nRound\n<round_name>\n<round_index>
```

**Recipients:** All clients
**Purpose:** Announces round start

**Details:**
- `STAGE` is the main announcement (triggers voice/visual effects)
- `STAGE_INFO` is the lightweight version with detailed information

Optional round metadata:
```
ROUND_AUTHORS\n<author1>\n<author2>\n...
ROUND_SOURCES\n<source1>\n<source2>\n...
ROUND_COMMENTS\n<comments>
```

#### 2. Round Themes
```
ROUNDTHEMES\n<theme1>\n<theme2>\n...\n<themeN>
ROUND_THEMES2\n<theme1_description>\n<theme2_description>\n...
ROUND_THEMES_COMMENTS\n<theme1_comment>\n<theme2_comment>\n...
```

**Recipients:** All clients
**Purpose:** Displays the round's themes

**Note:** `ROUNDTHEMES` and `ROUND_THEMES2` are deprecated but still sent for backward compatibility.

#### 3. Round Table
```
TABLO2\n<theme_count>\n<question_count>\n<data>
```

**Recipients:** All clients
**Purpose:** Defines the question table structure

**Format:** The table data is serialized with theme names and question prices.

Example structure:
- First line: theme count and questions per theme
- Following lines: theme names and question info (price, state)

#### 4. Theme Presentation (Sequential)

For each theme in the round:
```
THEME2\n<theme_name>
```

**Recipients:** All clients
**Purpose:** Announces each theme one by one

**Timing:** Sent with delays between themes for dramatic effect

---

### Question Selection

#### Player-Selected Mode (Classic Game)

When a player must choose a question:

```
SETCHOOSER\n<player_index>
SUMS\n<sum1>\n<sum2>\n...
SHOWTABLO
```

**Recipients:** All clients (SETCHOOSER, SUMS), Chooser + Showman (SHOWTABLO for some cases)
**Purpose:** Designates who chooses and shows the table

Then to the showman and chooser:
```
ASK_SELECT_PLAYER\n<selection_type>\n<min_index>\n<max_index>
```

**Recipients:** Showman (and sometimes the current chooser)
**Purpose:** Requests question selection

**Parameters:**
- `selection_type`: Type of selection (e.g., question choice, player selection)
- `min_index`, `max_index`: Valid selection range

When selection is made:
```
CHOICE\n<theme_index>\n<question_index>
```

**Recipients:** All clients
**Purpose:** Announces which question was selected

#### Sequential Mode

Questions are played automatically:
```
CHOICE\n<theme_index>\n<question_index>
```

**Recipients:** All clients
**Purpose:** Announces auto-selected question

---

### Question Playback Sequence

#### 1. Question Start
```
QUESTION\n<price>
QTYPE\n<question_type>\n<is_default>\n<is_no_risk>
```

**Recipients:** All clients
**Purpose:** Announces question price and type

**Question Types:**
- `simple` - Standard question (default)
- `cat` - Question with secret (player selection required)
- `bagcat` - Secret question with all players
- `auction` - Auction-style question
- `sponsored` - Sponsored question (no risk)
- Custom types defined in packages

**Parameters:**
- `is_default`: "1" if standard type, "0" if special
- `is_no_risk`: "1" if wrong answer doesn't lose points

#### 2. Question Theme/Caption (if different from round theme)
```
QUESTIONCAPTION\n<theme_name>
```

**Recipients:** All clients
**Purpose:** Displays question-specific theme

#### 3. Content Display

Text content:
```
CONTENT_SHAPE\n<placement>\n<layout_id>\n<type>\n<shape>
CONTENT\n<placement>\n<layout_id>\n<type>\n<text_or_ref>
```

**Recipients:** All clients
**Purpose:** Displays question content

**Placements:**
- `screen` - Main display area
- `replic` - Speech/commentary area
- `background` - Background layer

**Content Types:**
- `text` - Text content
- `image` - Image reference
- `audio` - Audio reference
- `video` - Video reference
- `html` - HTML content

**Shape format:** Describes content layout (e.g., `&&&&&&` for 6 content items)

Additional content:
```
CONTENT_APPEND\n<placement>\n<layout_id>\n<type>\n<additional_content>
```

**Recipients:** All clients
**Purpose:** Appends more content to existing display

Content state updates:
```
CONTENT_STATE\n<placement>\n<item_index>\n<state>
```

**Recipients:** All clients
**Purpose:** Updates visual state of content items

**States:**
- `normal` - Default state
- `active` - Highlighted/current
- `right` - Correct answer
- `wrong` - Incorrect answer

#### 4. Answer Phase Initiation

For button-press questions:
```
TRY
```

**Recipients:** All clients
**Purpose:** Enables answer buttons

To specific player who pressed:
```
YOUTRY
```

**Recipients:** Specific player
**Purpose:** Confirms their button press was registered

When button time ends:
```
ENDTRY\n<mode>
```

**Recipients:** All clients
**Purpose:** Disables answer buttons

**Modes:**
- `all` - All players' buttons disabled
- `<player_index>` - Specific player only

For direct answer questions (no button press):
```
ANSWER\n<answer_type>
```

**Recipients:** Specific player (or all for ForAll type)
**Purpose:** Requests answer input

**Answer Types:**
- `text` - Free text answer
- `select` - Multiple choice (sent with options earlier)
- `number` - Numeric answer

Optional for oral answers:
```
ORAL_ANSWER
```

**Recipients:** Specific player
**Purpose:** Indicates answer should be spoken (not typed)

#### 5. Answer Collection

When player provides answer:
```
ANSWER_VERSION\n<player_index>\n<preliminary_answer>
```

**Recipients:** All clients
**Purpose:** Shows player's preliminary typed answer (if visible mode enabled)

Final answer display:
```
PLAYER_ANSWER\n<player_index>\n<answer_text>
```

**Recipients:** All clients
**Purpose:** Displays player's submitted answer

#### 6. Answer Validation

To showman:
```
ASK_VALIDATE\n<player_index>\n<answer>\n<is_right_by_ai>
```

**Recipients:** Showman
**Purpose:** Requests answer validation

**Parameters:**
- `player_index`: Which player answered
- `answer`: The answer text
- `is_right_by_ai`: AI suggestion (0=wrong, 1=right)

Showman responds with validation decision (client sends VALIDATE message).

Validation result broadcast:
```
PERSON\n<+/->\n<player_index>\n<points_change>
```

**Recipients:** All clients
**Purpose:** Shows validation result and score change

**Format:**
- `+` for correct, `-` for incorrect
- `player_index`: Which player
- `points_change`: Points added/subtracted

Alternative for pass (no answer):
```
PASS\n<player_index>
```

**Recipients:** All clients
**Purpose:** Indicates player passed or ran out of time

#### 7. Right Answer Display

```
RIGHT_ANSWER_START
RIGHTANSWER\n<answer_text>
```

**Recipients:** All clients
**Purpose:** Shows the correct answer

Or for complex answers with content:
```
RIGHT_ANSWER_START
CONTENT\n<placement>\n<layout_id>\n<type>\n<content>
```

For select-type questions:
```
CONTENT_STATE\n<placement>\n<correct_option_index>\n<state_right>
```

**Recipients:** All clients
**Purpose:** Highlights the correct option

#### 8. Question Metadata

Question sources and comments (if available):
```
QUESTION_AUTHORS\n<author1>\n<author2>\n...
QUESTION_SOURCES\n<source1>\n<source2>\n...
QUESTION_COMMENTS\n<comments>
```

**Recipients:** All clients
**Purpose:** Displays question attribution and notes

#### 9. Question End
```
QUESTION_END
```

**Recipients:** All clients
**Purpose:** Marks question completion

#### 10. Score Update
```
SUMS\n<sum1>\n<sum2>\n...\n<sumN>
```

**Recipients:** All clients
**Purpose:** Updates all player scores

---

### Special Question Types

#### Cat/Secret Questions

When a question requires player selection:

```
QTYPE\ncat\n0\n0
ASK_SELECT_PLAYER\nstake\n0\n<player_count-1>
```

**Recipients:** Showman (and current player if chooser)
**Purpose:** Requests player selection for secret question

Response from client:
```
SELECT_PLAYER\n<player_index>
```

After selection:
```
SETCHOOSER\n<selected_player_index>
```

**Recipients:** All clients
**Purpose:** Announces selected player

#### Stake/Auction Questions

Stake requests:
```
ASK_STAKE\n<stake_type>\n<min_stake>\n<max_stake>\n<step>
```

**Recipients:** Current staking player and showman
**Purpose:** Requests stake amount

**Stake Types:**
- `nominal` - Standard question price
- `stake` - Custom stake
- `pass` - Can pass
- `allIn` - All-in option available

Client responds:
```
SET_STAKE\n<stake_type>\n<amount>
```

Stake broadcast:
```
PERSONSTAKE\n<player_index>\n<stake_type>\n<amount>
```

**Recipients:** All clients
**Purpose:** Announces player's stake

**Stake Type Values:**
- `0` - Pass
- `1` - Nominal
- `2` - Custom stake
- `3` - All-in

#### Final Round Stakes

In final round, players make hidden stakes:
```
ASK_STAKE\nfinal\n1\n<player_sum>\n1
```

**Recipients:** Each player individually (sequentially)
**Purpose:** Requests final round stake

When player makes stake:
```
PERSONFINALSTAKE\n<player_index>
```

**Recipients:** All clients
**Purpose:** Indicates player has made their stake (amount hidden)

Final round answer phase:
```
FINALTHINK\n<time_limit>
```

**Recipients:** All players
**Purpose:** Begins hidden answer phase with time limit

---

### Round End Sequence

```
STOP
ROUND_END\n<reason>
```

**Recipients:** All clients
**Purpose:** Marks round completion

**Reasons:**
- `empty` - All questions played
- `timeout` - Time ran out
- `manual` - Manually ended by host

Before next round:
```
SUMS\n<sum1>\n<sum2>\n...
```

**Recipients:** All clients
**Purpose:** Shows updated scores

---

### Game End Sequence

```
STOP
WINNER\n<player_index>
```

**Recipients:** All clients
**Purpose:** Announces winner(s)

**Note:** Multiple WINNER messages can be sent if there's a tie. `-1` indicates no winner.

```
STAGE\nAfter
GAME_STATISTICS\n<stats_data>
```

**Recipients:** All clients
**Purpose:** Shows final game statistics

Optional review request:
```
ASK_REVIEW\n<package_source_url>
```

**Recipients:** Human players
**Purpose:** Requests game/package review

---

## Timer Messages

Timers control various time-limited phases:

### Timer Start
```
TIMER\n<timer_index>\n<GO>\n<duration>\n<timer_type>
```

**Recipients:** All clients
**Purpose:** Starts a countdown timer

**Timer Indexes:**
- `0` - Round timer (overall round time)
- `1` - Thinking timer (answer time)
- `2` - Decision timer (choice/stake time)

**Duration:** Time in 1/10 seconds (e.g., 200 = 20 seconds)

**Timer Types:**
- `-2` - Wait/pause timer
- `-1` - Standard countdown
- `1` - Question thinking timer
- `2` - Additional time indicator

### Timer Pause
```
TIMER\n<timer_index>\n<PAUSE>\n<remaining_time>
```

**Recipients:** All clients
**Purpose:** Pauses timer, showing remaining time

### Timer Resume
```
TIMER\n<timer_index>\n<RESUME>
```

**Recipients:** All clients
**Purpose:** Resumes paused timer

### Timer Stop
```
TIMER\n<timer_index>\n<STOP>
```

**Recipients:** All clients
**Purpose:** Stops and hides timer

---

## Control Messages

### Pause/Resume Game
```
PAUSE\n<+/->\n<timer0_remaining>\n<timer1_remaining>\n<timer2_remaining>
```

**Recipients:** All clients
**Purpose:** Pauses or resumes game

**Parameters:**
- `+` to pause, `-` to resume
- Remaining times for each timer (in 1/10 seconds)

### Cancel Decision
```
CANCEL\n<person_name>
```

**Recipients:** Specific person
**Purpose:** Cancels current decision request (choice, stake, validation)

### Media Control
```
RESUME
```

**Recipients:** All clients
**Purpose:** Resumes media playback (after pause/validation)

```
STOP_PLAY
```

**Recipients:** All clients
**Purpose:** Stops current question playback immediately

### Table Modifications

Remove question from table:
```
TOGGLE\n<theme_index>\n<question_index>\n<price>
```

**Recipients:** All clients
**Purpose:** Marks question as played (price = -1 removes it)

Restore question to table:
```
TOGGLE\n<theme_index>\n<question_index>\n<price>
```

**Recipients:** All clients
**Purpose:** Restores previously removed question (with valid price)

### Game Configuration Changes
```
SET_OPTIONS\n<key1>\n<value1>\n<key2>\n<value2>\n...
```

**Recipients:** All clients
**Purpose:** Updates game options during play

---

## Player State Messages

### Chooser Assignment
```
SETCHOOSER\n<player_index>
```

**Recipients:** All clients
**Purpose:** Designates current chooser

Optional modifiers:
```
SETCHOOSER\n<player_index>\n<+/->\n<reason>
```

**Modifiers:**
- `+` - Gained chooser status
- `-` - Lost chooser status

**Reasons:**
- `INITIAL` - Initial selection
- Other custom reasons

### Player State Updates
```
PLAYER_STATE\n<player_index>\n<state>\n<value>
```

**Recipients:** All clients
**Purpose:** Updates player's state

**States:**
- `ready` - Ready status for game start
- `apellating` - Currently appealing answer
- `right` - Answered correctly
- `wrong` - Answered incorrectly

### Score Changes
```
PLAYER_SCORE_CHANGED\n<player_index>\n<new_sum>\n<change_reason>
```

**Recipients:** All clients
**Purpose:** Notifies about score change with reason

**Reasons:**
- `answer` - From answering question
- `penalty` - Penalty applied
- `bonus` - Bonus awarded
- `correction` - Manual correction

### Change Sum (Manual Adjustment)
```
CHANGE\n<player_index>\n<new_sum>
```

**Recipients:** All clients
**Purpose:** Manually changes player's score

---

## Error and User Feedback

### User Errors
```
USER_ERROR\n<error_code>\n<param1>\n<param2>\n...
```

**Recipients:** Specific user who caused error
**Purpose:** Informs user of invalid action

**Common Error Codes:**
- `CannotKickYourSelf` - Cannot kick yourself
- `CannotKickBots` - Cannot kick bot players
- `CannotSetHostToYourself` - Cannot transfer host to yourself
- `CannotSetHostToBots` - Cannot make bot a host
- `OversizedFile` - Content file too large
- `AppellationFailedTooFewPlayers` - Not enough players for appeal

### Game Errors
```
GAME_ERROR
```

**Recipients:** All clients
**Purpose:** Indicates critical game error occurred

### Replic Messages (Legacy)
```
REPLIC\n<source>\n<text>
```

**Recipients:** All clients
**Purpose:** Chat/commentary message

**Sources:**
- `0` - System message
- `1` - Showman
- `p0`, `p1`, ... - Player by index
- `s` - Special message

**Note:** Being replaced by structured messages like SHOWMAN_REPLIC

### Showman Replic (Localized)
```
SHOWMAN_REPLIC\n<random_seed>\n<message_code>\n<arg1>\n<arg2>\n...
```

**Recipients:** All clients
**Purpose:** Showman commentary with localization support

**Message Codes:**
- `RightAnswer` - Acknowledging correct answer
- `WrongAnswer` - Acknowledging wrong answer
- `QuestionPriceInfo` - Question price information
- `PlayerChooses` - Player is choosing
- `RoundSkippedNoPlayers` - Round skipped due to no players
- Many others for different game events

---

## Connection and Lobby Messages

### Player Connection
```
CONNECTED\n<client_name>\n<role>\n<is_male>\n<index>
```

**Recipients:** All clients
**Purpose:** Notifies about new connection

**Roles:**
- `showman`
- `player`
- `viewer`

### Player Disconnection
```
DISCONNECTED\n<client_name>
```

**Recipients:** All clients
**Purpose:** Notifies about disconnection

### Player Readiness
```
READY
```

**Recipients:** From client to server
**Purpose:** Indicates player is ready to start

Server may respond with updated INFO2 showing ready states.

### Game Join Mode
```
SETJOINMODE\n<mode>
```

**Recipients:** All clients
**Purpose:** Sets who can join the game

**Modes:**
- `0` - Free join
- `1` - Approval required
- `2` - Forbidden

### Avatar Updates
```
AVATAR\n<person_name>\n<content_type>\n<uri>
```

**Recipients:** All clients
**Purpose:** Updates person's avatar

**Content Types:**
- `image/png`
- `image/jpeg`
- `video/mpeg` (for video avatars)

### Kicking and Banning
```
YOU_ARE_KICKED
```

**Recipients:** Kicked person
**Purpose:** Notifies about being kicked

```
BANNED\n<client_id>\n<client_name>
```

**Recipients:** All clients
**Purpose:** Notifies about banned player

```
UNBANNED\n<client_id>
```

**Recipients:** All clients
**Purpose:** Notifies about unbanned player

---

## Advanced Features

### Appellations (Answer Appeals)

When appellations are enabled, players can appeal wrong answer decisions:

```
APPELLATION\n<mode>\n<player_index>
```

**Recipients:** All clients
**Purpose:** Manages appellation process

**Modes:**
- `start` - Appellation process started
- `vote` - Voting phase
- `end` - Appellation ended

Appeal vote from showman:
```
VALIDATE\n<answer>\n<+/->\n<revalidate>
```

**Parameters:**
- `answer`: Original answer text
- `+/-`: New validation decision
- `revalidate`: 1 if revalidating, 0 otherwise

### Complex Answer Validation

For questions with multiple acceptable answers:

```
QUESTION_ANSWERS\n<right_count>\n<right1>\n<right2>\n...\n<wrong_count>\n<wrong1>\n<wrong2>\n...
```

**Recipients:** Showman
**Purpose:** Provides all right/wrong answers for validation assistance

### Media Preloading

For smooth playback, clients can preload media:

```
ROUNDCONTENT\n<uri1>\n<uri2>\n...\n<uriN>
```

**Recipients:** All clients
**Purpose:** Lists media URIs to preload

Client reports progress:
```
MEDIA_PRELOAD_PROGRESS\n<percentage>
```

**Recipients:** From client to server
**Purpose:** Reports preload percentage (0-100)

Server notifies showman:
```
MEDIALOADED\n<client_name>
```

**Recipients:** Showman
**Purpose:** Indicates client has loaded media

### Answer Options (Multiple Choice)

Before question content, for select-type questions:

```
LAYOUT\n<option_count>\n<column_count>
```

**Recipients:** All clients
**Purpose:** Defines layout for answer options

Then content items for each option:
```
CONTENT\nscreen\n0\nimage\n<option1_image>
CONTENT\nscreen\n1\nimage\n<option2_image>
...
```

Player selects via button press corresponding to option index.

### Numeric Answers

For questions with numeric answers:

```
ANSWER_DEVIATION\n<deviation>
ANSWER\nnumber
```

**Recipients:** Answering player(s)
**Purpose:** Requests numeric answer with acceptable deviation

**Example:** If correct answer is 100 with deviation 5, any answer from 95-105 is accepted.

---

## Message Ordering Guarantees

The SICore game agent guarantees the following message ordering:

### 1. Initialization Phase (Before Game Start)
Order is strictly enforced:
1. CONNECTED messages (for each joining client)
2. INFO2 (game state)
3. GAMEMETADATA
4. OPTIONS2
5. STAGE (BeforeGame)
6. PACKAGEID
7. ROUNDSNAMES
8. PACKAGE (+ optional metadata)
9. GAMETHEMES (optional)
10. SUMS (initial zeros)

### 2. Round Start Phase
Order is strictly enforced:
1. STAGE (round name)
2. STAGE_INFO (round info)
3. Optional round metadata (AUTHORS, SOURCES, COMMENTS)
4. ROUNDTHEMES / ROUND_THEMES2 (deprecated)
5. ROUND_THEMES_COMMENTS
6. THEME2 for each theme (sequentially)
7. TABLO2 (table structure)

### 3. Question Selection Phase
Order varies by selection mode:

**Player-Selected:**
1. SETCHOOSER (if chooser changed)
2. SUMS
3. SHOWTABLO
4. ASK_SELECT_PLAYER (to showman/chooser)
5. CHOICE (after selection made)

**Sequential:**
1. CHOICE (auto-selected)

### 4. Question Playback Phase
Strict order:
1. QUESTION (price)
2. QTYPE (type info)
3. QUESTIONCAPTION (if theme differs)
4. CONTENT_SHAPE (for content layout)
5. CONTENT messages (for each content item, in order)
6. CONTENT_APPEND (for partial reveals, multiple possible)
7. TRY / ANSWER message (depending on question type)
8. Button press handling:
   - YOUTRY (to presser)
   - ENDTRY (when time expires)
9. Answer collection:
   - ANSWER_VERSION (preliminary, optional)
   - PLAYER_ANSWER (final)
10. Validation:
    - ASK_VALIDATE (to showman)
    - PERSON (validation result)
11. Right answer:
    - RIGHT_ANSWER_START
    - RIGHTANSWER or CONTENT (answer display)
    - CONTENT_STATE (for select-type)
12. Metadata:
    - QUESTION_AUTHORS (optional)
    - QUESTION_SOURCES (optional)
    - QUESTION_COMMENTS (optional)
13. QUESTION_END
14. SUMS (updated scores)

### 5. Round End Phase
Order is enforced:
1. STOP (timer stop)
2. ROUND_END (with reason)
3. SUMS (final round scores)

### 6. Game End Phase
Order is enforced:
1. STOP (timer stop)
2. WINNER message(s)
3. STAGE (After)
4. GAME_STATISTICS
5. ASK_REVIEW (optional, to players)

### Important Notes on Ordering:

1. **Interleaved Messages:** Some messages can be interleaved:
   - TIMER messages can appear at various points
   - REPLIC / SHOWMAN_REPLIC can appear anytime
   - PLAYER_STATE updates can appear anytime
   - PAUSE messages can interrupt most sequences

2. **Parallel Tracks:** These messages run in parallel to main flow:
   - Timer updates (TIMER)
   - Player state updates (PLAYER_STATE)
   - Chat/commentary (REPLIC)

3. **Idempotent Messages:** Messages marked with `[IdempotencyRequired]` attribute in code can be sent multiple times and should be handled idempotently by clients:
   - INFO2
   - OPTIONS2
   - TABLO2
   - ROUNDSNAMES
   - ROUND_THEMES_COMMENTS
   - AVATAR
   - And others

4. **Visual State Messages:** Messages that affect visual game state are buffered for reconnecting clients:
   - Last visual message is stored in `GameData.LastVisualMessage`
   - Sent to newly connecting clients to restore state

---

## Creating a Custom Client

To create a custom game client that interacts with SICore:

### 1. Connection

Establish a connection to the SICore server via the network layer (typically using the SICore.Network components or a compatible protocol).

### 2. Message Parsing

Implement a message parser that:
- Splits messages by `\n` (ArgsSeparatorChar)
- Identifies message type from first argument
- Extracts remaining arguments

```csharp
var parts = messageText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
var messageType = parts[0];
var args = parts.Skip(1).ToArray();
```

### 3. Message Handlers

Implement handlers for each message type relevant to your client role:

**Essential for All Clients:**
- CONNECTED, DISCONNECTED
- INFO2
- STAGE, STAGE_INFO
- PACKAGE, ROUNDSNAMES
- SUMS
- TIMER
- REPLIC, SHOWMAN_REPLIC

**Essential for Players:**
- SETCHOOSER
- ASK_SELECT_PLAYER (if chooser)
- SHOWTABLO, CHOICE
- QUESTION, QTYPE, CONTENT, CONTENT_SHAPE
- TRY, YOUTRY, ENDTRY
- ANSWER (and related answer messages)
- PERSON (validation results)
- RIGHTANSWER, RIGHT_ANSWER_START

**Essential for Showman:**
- All player messages (for display)
- ASK_SELECT_PLAYER (question selection)
- ASK_VALIDATE (answer validation)
- ASK_STAKE (stake approvals)
- MEDIALOADED (client readiness)

**Essential for Viewers:**
- All display messages (CONTENT, QUESTION, SUMS, etc.)
- No action messages (ASK_*, ANSWER, etc.)

### 4. Game State Management

Maintain state for:
- Current game phase (before/during/after)
- Current round and theme
- Player scores and states
- Current question state
- Table state (which questions played)
- Timer states

### 5. Sending Messages

Send messages to the server based on user actions:

**Player Actions:**
- `READY` - When ready to start
- `I\n<press_duration>` - Button press
- `ANSWER\n<text>` - Submit answer
- `CHOICE\n<theme>\n<question>` - Select question (if chooser)
- `DELETE\n<theme>` - Delete theme (final round)

**Showman Actions:**
- `START` - Start game
- `SELECT_PLAYER\n<index>` - Select player
- `VALIDATE\n<answer>\n<+/->\n<score>` - Validate answer
- `SET_STAKE\n<type>\n<amount>` - Confirm/set stake
- `PAUSE\n<+/->` - Pause/resume game
- `MOVE\n<direction>` - Navigation (back/forward in game)

**Configuration Actions:**
- `CONFIG\n<command>` - Various config commands
- `KICK\n<client>` - Kick player
- `BAN\n<client>` - Ban player
- `SETHOST\n<client>` - Transfer host privileges

### 6. UI Updates

Update UI in response to messages:
- Display content (text, images, video)
- Update scores
- Show/hide buttons
- Update timers
- Highlight current player/chooser
- Show question state

### 7. Error Handling

Handle error messages gracefully:
- Display USER_ERROR messages to user
- Reconnect on DISCONNECTED
- Handle GAME_ERROR appropriately

### 8. Testing

Test your client with:
- Different question types
- All game phases
- Edge cases (timeouts, disconnections, appeals)
- Different game rules/options

---

## Example Message Sequences

### Example 1: Simple Question Flow

```
[Game Start]
STAGE\nRound 1
TABLO2\n...(table data)...
SETCHOOSER\n0
ASK_SELECT_PLAYER\nquestion\n0\n2
[Showman selects question]
CHOICE\n0\n0
QUESTION\n100
QTYPE\nsimple\n1\n0
CONTENT_SHAPE\nscreen\n0\ntext\n&
CONTENT\nscreen\n0\ntext\nWhat is 2+2?
TRY
[Player 1 presses button]
YOUTRY\n(to player 1)
ENDTRY\nall
ANSWER\ntext\n(to player 1)
[Player 1 types "4"]
PLAYER_ANSWER\n0\n4
ASK_VALIDATE\n0\n4\n1
[Showman validates correct]
PERSON\n+\n0\n100
SUMS\n100\n0\n0
RIGHT_ANSWER_START
RIGHTANSWER\n4
QUESTION_END
SUMS\n100\n0\n0
```

### Example 2: Multiple Choice Question

```
QUESTION\n200
QTYPE\nsimple\n1\n0
LAYOUT\n4\n2
CONTENT\nscreen\n0\nimage\ncat.jpg
CONTENT\nscreen\n1\nimage\ndog.jpg
CONTENT\nscreen\n2\nimage\nbird.jpg
CONTENT\nscreen\n3\nimage\nfish.jpg
CONTENT\nscreen\n-1\ntext\nWhich is a mammal?
TRY
[Player 0 presses button for option 1]
YOUTRY\n(to player 0)
ENDTRY\nall
PERSON\n+\n0\n200
SUMS\n200\n0\n0
RIGHT_ANSWER_START
CONTENT_STATE\nscreen\n1\nright
QUESTION_END
```

### Example 3: Stake Question

```
CHOICE\n1\n2
QUESTION\n300
QTYPE\nstake\n0\n0
ASK_STAKE\nstake\n100\n500\n100\n(to current player)
[Player chooses 400]
PERSONSTAKE\n0\n2\n400
CONTENT\nscreen\n0\ntext\nQuestion text here...
TRY
...(continue as normal question)...
```

### Example 4: Final Round

```
STAGE\nFinal Round
[Themes shown, players delete themes until one remains]
THEME_INFO\n0\nLast Theme
[Each player makes hidden stake]
ASK_STAKE\nfinal\n1\n<player_sum>\n1\n(to each player)
PERSONFINALSTAKE\n0
PERSONFINALSTAKE\n1
[Question content shown]
FINALTHINK\n300
[Each player writes answer]
[Showman validates each answer one by one]
PERSON\n+\n0\n<stake_amount>
PERSON\n-\n1\n<negative_stake_amount>
SUMS\n500\n100\n250
RIGHTANSWER\nCorrect answer text
QUESTION_END
STOP
WINNER\n0
STAGE\nAfter
```

---

## Best Practices for Client Development

1. **Handle Missing Messages:** Network issues may cause message loss. Implement state recovery.

2. **Idempotency:** Some messages may be sent multiple times. Handle them idempotently.

3. **Async Processing:** Don't block on message processing. Use async handlers.

4. **State Validation:** Validate game state before acting on messages.

5. **Graceful Degradation:** If an unknown message arrives, log it but don't crash.

6. **Timeouts:** Implement timeouts for expected messages.

7. **Reconnection:** Support reconnecting to ongoing games with state restoration.

8. **Buffering:** Buffer visual messages for smooth playback.

9. **Localization:** Support localized SHOWMAN_REPLIC message codes.

10. **Testing:** Use the SICore.Tests scenarios as reference implementations.

---

## Reference Implementation

See `test/SICore/SICore.Tests/Scenarios/ScenariosTests.cs` for reference test implementation showing complete message flows.

Key files in the codebase:
- `src/SICore/SICore/Messages.cs` - All message type constants
- `src/SICore/SICore/Clients/Game/GameLogic.cs` - Game logic and message sending
- `src/SICore/SICore/Clients/Game/GameActions.cs` - Message sending methods
- `src/SICore/SICore/MessageParams.cs` - Message parameter constants
- `src/SICore/SICore/ReplicCodes.cs` - Replic source codes

---

## Version Compatibility

This documentation describes the message protocol for SICore version 7.x and later.

**Deprecated Messages:** Some messages are marked obsolete but still sent for backward compatibility:
- ROUNDTHEMES, ROUND_THEMES2 (replaced by structured theme info)
- FIRST, NEXT, NEXTDELETE (replaced by ASK_SELECT_PLAYER)
- CAT, CATCOST, STAKE (replaced by ASK_STAKE/SET_STAKE)
- FINALSTAKE (replaced by ASK_STAKE with final mode)
- TIMEOUT (replaced by ROUND_END with timeout reason)
- Several others marked with `[Obsolete]` attribute

**New Clients** should ignore deprecated messages and use their modern replacements.

**Legacy Support:** If supporting older servers, clients may need to handle deprecated messages.

---

## Appendix: Complete Message Type Reference

See `Messages.cs` for the complete list of ~140+ message types with descriptions.

Key message categories:
- **Game Flow:** STAGE, ROUND_END, QUESTION_END, etc.
- **Content:** CONTENT, CONTENT_SHAPE, CONTENT_APPEND, CONTENT_STATE
- **Actions:** ASK_*, ANSWER, VALIDATE, CHOICE, SELECT_PLAYER
- **State:** SUMS, TIMER, PLAYER_STATE, SETCHOOSER
- **Communication:** REPLIC, SHOWMAN_REPLIC, CONNECTED, DISCONNECTED
- **Configuration:** OPTIONS2, SETJOINMODE, CONFIG
- **Control:** PAUSE, CANCEL, RESUME, STOP
- **Special:** APPELLATION, MEDIALOADED, TOGGLE, AVATAR

---

## Support and Contributions

For issues or questions about the message protocol, please refer to:
- SICore repository: https://github.com/VladimirKhil/SI
- SICore documentation in `src/SICore/SICore/`
- SIEngine documentation in `src/Common/SIEngine/DOCUMENTATION.md`

When reporting issues, include:
- Full message sequence leading to issue
- Expected vs. actual behavior
- Client role (player/showman/viewer)
- Game settings and question type

---

*This documentation is maintained alongside the SICore codebase. Last updated: 2025-12-14*
