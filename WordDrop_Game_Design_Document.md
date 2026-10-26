# WordDrop — Game Design Document (GDD)

**Version:** 1.0  
**Date:** October 2026  
**Engine:** Unity 6.3 LTS  
**Platform:** Android first; architecture should support iOS  
**Orientation:** Portrait  
**Initial language:** Brazilian Portuguese (pt-BR)  
**Status:** Design baseline for the first playable version

> Values described as proposed are starting points to validate through playtesting, not immutable requirements.

---

## Document overview

WordDrop is a mobile puzzle game combining falling letter groups, crossword-style word formation, and Tetris-inspired board strategy. Players position groups of letters to form valid Brazilian Portuguese words horizontally and vertically. Words clear from the board; full rows can also clear, and gravity may create chain reactions.

The game combines a level-based campaign with an endless high-score mode.

### Design goals

- **Easy to learn:** explain the core interaction in about one minute.
- **Strategic depth:** reward placement, intersections, long words, row clears, and cascades.
- **Short sessions:** target roughly 1–3 minutes per campaign level after the tutorial.
- **Fair progression:** introduce mechanics gradually and avoid difficulty spikes.
- **Offline-first:** core gameplay and campaign saves work without an account or network connection.

### Product snapshot

| Item | Decision / baseline |
|---|---|
| Genre | Casual word puzzle + falling-block strategy |
| Platform | Android first; architecture should support iOS |
| Orientation | Portrait |
| Engine | Unity 6.3 LTS, Universal 2D (Core), C# |
| Language | Brazilian Portuguese (pt-BR) |
| Game modes | Campaign + Endless |
| Board | Proposed 10 columns × 20 rows |
| Falling pieces | Connected groups of 2–4 letter tiles |
| Word rules | Horizontal and vertical; minimum 3 letters |
| Clearing | Word clears + full-row clears + gravity chains |

## 1. Core gameplay

### 1.1 Board and tiles

The board is a discrete grid. The proposed baseline is **8 columns × 14 rows**, tuned for portrait phones. Each occupied cell stores one letter tile. Grid logic—not 2D physics—controls movement and collision for predictable placement.

| Property | Rule |
|---|---|
| Dimensions | 10 × 20 cells (initial proposal) |
| Gravity | Downward |
| Active piece | Connected group of 2–4 letters |
| Preview | Show one upcoming piece |
| Controls | Move left/right, rotate, soft drop, hard drop |
| Loss condition | A new piece cannot enter the board, unless a level rule overrides it |

### 1.2 Falling letter groups

Pieces are connected shapes containing 2–4 letter tiles; the group itself does not need to be a valid word.

- Players can move the active piece left/right, rotate when the placement is valid, soft-drop, and hard-drop.
- A ghost preview shows the landing position.
- A short lock delay may permit a final adjustment after contact with the stack.
- Campaign levels use curated piece sequences initially; Endless uses weighted random selection.
- If a move would place any tile outside the board or overlap a locked tile, reject the move.

### 1.3 Word detection

After a piece locks, scan horizontal and vertical occupied sequences. A valid word must be present in the approved dictionary and meet the configured minimum length of three letters.

- Each distinct word created by a placement scores once.
- When words intersect, their shared tile may contribute to both words but is removed only once.
- Invalid sequences remain on the board.
- A piece that cannot spawn triggers game over unless a level-specific rule overrides it.

#### Brazilian Portuguese rules

- Normalize strings consistently using Unicode NFC and uppercase.
- Preserve accents and `Ç`; do not silently remove diacritics.
- Define whether conjugations, plurals, abbreviations, and proper nouns are accepted.
- Use a licensed word list and document its source and license.
- Keep the dictionary version identifiable so results are reproducible.

For example, `AVÓ` should remain distinct from `AVO` unless the accepted dictionary explicitly includes both forms.

## 2. Hybrid clearing and combos

### 2.1 Resolution sequence

Each time a piece locks, the game follows this deterministic sequence:

1. Lock the active piece into the board grid.
2. Detect all newly formed valid horizontal and vertical words.
3. Award points for each distinct word and any applicable multiword bonus.
4. Clear all tiles belonging to valid words; shared tiles are removed once.
5. Check for completely occupied rows and clear them with a row bonus.
6. Apply gravity so unsupported tiles fall into empty cells.
7. Scan again; resolve new words and full rows until the board is stable.
8. Update objectives, win/loss state, score, and next-piece state.

### 2.2 Edge cases

- Evaluate full rows after the current word-clear operation and before gravity.
- After gravity, repeat word and row detection until no new clear is possible.
- Invalid sequences remain on the board.
- A shared tile is physically cleared once, while every distinct valid word is scored.
- The combo resets when a placement creates no word or row clear.
- Use a defensive maximum of 20 resolution passes per placement; log and stop if exceeded. This is an implementation safeguard, not a normal gameplay limit.

### 2.3 Combo multipliers

| Chain event | Proposed multiplier |
|---|---:|
| Initial clear | ×1.0 |
| First chain | ×1.5 |
| Second chain | ×2.0 |
| Third chain and beyond | ×2.5 maximum |

The multiplier applies to points earned in the relevant resolution chain. Multiword bonuses are awarded once per piece placement, not once per resolution pass. Communicate chains with a brief animation, sound cue, and readable label such as **COMBO ×1.5**.

## 3. Scoring and rewards

### 3.1 Word score

Proposed base formula:

**S_word = 10 × L²**, where **L** is the number of letters in the word.

| Word length | Base score |
|---:|---:|
| 3 letters | 90 |
| 4 letters | 160 |
| 5 letters | 250 |
| 6 letters | 360 |
| 7 letters | 490 |
| 8 letters | 640 |

The formula and values are tuning parameters to validate in playtests.

### 3.2 Bonuses

| Action | Proposed bonus |
|---|---:|
| Clear a full row | +150 |
| Create 2 words in one placement | +100 |
| Create 3 or more words in one placement | +250 |
| Chain reaction | Apply the current chain multiplier |
| Complete a level objective | Level-specific reward |

Define a consistent order of operations for base points, bonuses, and multipliers. Use integer arithmetic and test calculations so results are deterministic across devices.

### 3.3 Stars and rewards

- Campaign levels award up to three stars.
- Score-based levels use per-level thresholds for one, two, and three stars.
- For non-score objectives, complete the objective first; stars can then depend on score, remaining moves, or another clearly communicated metric.
- Stars unlock campaign progress. Coins are optional and should not gate core progression.
- Avoid lives, energy timers, paid boosters, and monetization in the initial prototype.

## 4. Game modes

### 4.1 Campaign

- Handcrafted levels with explicit objectives, star thresholds, and optional piece sequences.
- Introduce one new mechanic at a time through short tutorials.
- Unlock the next level after completing the current level's objective.
- Begin with approximately 10 levels; validate the progression model before producing large volumes of content.

### 4.2 Endless

- No fixed level objective; the player aims for the highest score.
- Falling speed and board pressure increase gradually.
- End the run when a new piece cannot spawn.
- Save the current run result and local personal best.
- Reuse the same board and word-resolution engine with a different rules configuration.

### 4.3 Other modes

Daily challenges and online leaderboards can follow after the core game is validated. A fair shared daily challenge needs consistent seeds, dictionary versions, and rules; leaderboards also need an anti-cheat strategy.

## 5. Campaign progression and difficulty

### 5.1 Proposed worlds

| World | Levels | Introduced challenge |
|---|---:|---|
| 1. Primeiras Palavras | 1–10 | Movement, dropping, short words, scoring, row-clear basics |
| 2. Cruzamentos | 11–25 | Vertical words, intersections, multiword placements, first combos |
| 3. Ritmo de Letras | 26–45 | Faster falling, limited moves, planning ahead |
| 4. Desafios de Mestre | 46–70 | Balanced obstacles, restricted spaces, complex objectives |

World names, themes, and ranges are provisional. Difficulty should be tuned along independent dimensions rather than increasing everything at once.

### 5.2 Difficulty dimensions

| Dimension | Beginner | Advanced |
|---|---|---|
| Falling speed | Slow | Faster |
| Objective | Reach a modest score | Specific words/rows or mixed goals |
| Board space | Mostly empty | Pre-filled cells or obstacles |
| Piece selection | Helpful curated sequence | More varied sequence |
| Moves | Generous | Limited |
| Words | Common short words | Longer or specific words |
| Combos | Optional | Helpful or required |

### 5.3 Example first 10 levels

| Level | Objective | Special rule |
|---:|---|---|
| 1 | Score 500 points | Slow falling; guided pieces |
| 2 | Form 3 valid words | Introduce short words |
| 3 | Clear 2 full rows | Row-clear tutorial |
| 4 | Score 1,200 points | More varied pieces |
| 5 | Create a combo chain | Gravity/cascade tutorial |
| 6 | Form 5 words | Both directions count |
| 7 | Score 2,000 in 20 drops | Limited moves |
| 8 | Clear 3 rows and form 5 words | Combined objectives |
| 9 | Score 2,500 points | Faster falling |
| 10 | Complete a mixed objective | First milestone level |

These are illustrative targets; playtesting should establish the final thresholds and difficulty curve.

## 6. Player roles and profile

WordDrop does not need character classes. Roles are functional responsibilities within the game.

| Role/system | Responsibilities | MVP? |
|---|---|---|
| Guest player | Starts immediately, plays campaign and Endless, saves locally | Yes |
| Returning player | Resumes progress and views stars and best scores | Yes |
| Game engine | Manages movement, collision, word detection, clearing, gravity, and game-over rules | Yes |
| Progression system | Unlocks levels, awards stars, records results | Yes |
| Content administrator | Creates and validates dictionaries and level definitions during development | Development only |
| Competitive player | Participates in leaderboards | Later |

### 6.1 Local player profile

Store locally:

- Unique local player ID.
- Current unlocked campaign level.
- Best star rating per completed level.
- Campaign best scores.
- Endless-mode personal best.
- Sound and vibration preferences.
- Dictionary/content version used for the saved game.

The player ID can be generated locally;

## 7. Screen inventory and navigation

### 7.1 Screens

1. **Splash screen:** displays the WordDrop logo and performs lightweight initialization.
2. **Main menu:** continue campaign, choose Endless, open settings, and access future daily challenges.
3. **Gameplay:** board, active piece, next-piece preview, score, objective, progress indicator, and pause button.
4. **Level complete:** result, stars, score, objective status, replay, and next level.
5. **Pause menu:** resume, restart, settings, and return to menu; confirm if progress would be lost.
6. **Game over:** final score, personal best where relevant, retry, and return to menu.

7. Campaign level map.
8. Level introduction/objective briefing.
9. How-to-play/tutorial.
10. Endless results and personal records.
11. Daily challenge.
12. Achievements and player statistics.
13. Optional dictionary word explanation.
14. Accessibility and settings expansion.

### 7.3 Gameplay screen hierarchy

The board should occupy most of the portrait screen. The top area should show the level or mode, score, objective, and pause control. The next-piece preview should be visible without competing with the board. Movement, rotation, and drop controls should be easy to reach with one hand.

The interface should adapt to supported phone aspect ratios, keep text legible, and use touch-friendly controls.



### 8.5 Game states

Use explicit states to prevent input and animations from interfering with clearing and scoring:

`Initializing → Ready → Falling → Locking → Resolving → CheckingObjectives → Ready`

Additional states:

- `Paused`
- `LevelCompleted`
- `GameOver`

During `Resolving`, player movement must be disabled until all word and row-clearing chains finish. Level completion must not accidentally spawn another piece.

### 8.6 Dictionary strategy

- Use an appropriate licensed word list; document source and license.
- Normalize strings consistently with Unicode NFC and uppercase.
- Preserve accents and `Ç`.
- Define handling for conjugations, plurals, abbreviations, and proper nouns.
- Reject words shorter than the configured minimum.
- Test against representative Brazilian Portuguese words.
- Track dictionary version in saved data or run metadata.



## 12. Design decisions to validate early

Three decisions have an outsized impact on how WordDrop feels and should be tested in the prototype:

1. **Piece generation:** curated sequences for campaign, weighted random sequences for Endless, and a one-piece preview.
2. **Gravity model:** independent tile gravity versus connected-cluster gravity. Independent falling creates more cascades; cluster gravity preserves more of the Tetris feel. Test independent falling first.
3. **Word feedback:** automatically clear valid words or ask players to confirm. Automatic clearing is recommended to keep gameplay fast and intuitive.
