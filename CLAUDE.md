# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Unity project from the **Universal 2D (URP)** template. `WordDrop_Game_Design_Document.md` (repo root) is the design source of truth; check it before changing rules, scoring or screens. Built so far: startup flow, a 10-level campaign (world 1) with a level map, Endless, Daily Challenge, How to Play, boosters, achievements + statistics, word meanings, local leaderboards, local profile. Online leaderboards are not built (the `ILeaderboardService` interface is ready for one).

- **Unity version:** `6000.3.1f1` (Unity 6.3). Editor binary: `~/Unity/Hub/Editor/6000.3.1f1/Editor/Unity`.
- **Installed build targets:** Linux Standalone and WebGL only. The Android module must be added through Unity Hub before Android builds. iOS builds need a Mac.
- **Key packages** (`Packages/manifest.json`): URP 17.3, Input System 1.17 (the new Input System is the active handler; UI uses `InputSystemUIInputModule`), uGUI with TextMeshPro (essential resources imported into `Assets/TextMesh Pro/`), 2D Sprite, Tilemap and Animation, and Test Framework 1.6.
- **Player settings** (set by `ProjectSetup`): company `Cruze`, product `Word Drop`, bundle id `com.cruze.worddrop`, portrait only, Android IL2CPP + ARM64, Unity splash off.
- **Language:** all player-facing text is Brazilian Portuguese and lives in `Assets/Scripts/Core/Strings.cs`.
- **Version control:** `.gitignore` excludes generated folders (`Library/`, `Logs/`, `Temp/`, `UserSettings/`, `TestResults/`, `Snapshots/`) and IDE files (`*.slnx`, `*.csproj`). Only `Assets/`, `Packages/` and `ProjectSettings/` are source. Always commit `.meta` files together with their assets.

## Code layout

- `Assets/Scripts/` → assembly `WordDrop` (namespace `WordDrop.*`). `AssemblyInfo.cs` exposes `internal` members to the editor and test assemblies.
  - `Core/`: `SceneNames` (build order), `SceneLoader` (go to a scene via the Loading screen), `PlayerData` (the local profile in PlayerPrefs: player id, consent, unlocked level, stars/bests, Endless best, music/sound/vibration, content version), `Strings`, `AppBootstrap` (60 fps).
  - `Gameplay/`: pure, unit-tested rules: `Board` (grid, gravity, push-up rows), `Piece` (2–4 tile shapes plus the 1-tile booster piece, rotation, wall kicks, hint colors), `WordDictionary` (NFC + upper case, accents kept, definitions, wildcard matching), `WordFinder`, `Resolver` (the GDD 2.1 resolution loop), `Scoring`, `Difficulty` (shape sets + hint share), `PieceGenerator` (curated sequence, then weighted random, hint words split across pieces, Shuffle reroll), `SeededRandom` (xorshift, deterministic Daily), `Objectives` (tracker + pt-BR texts), `LevelDefinition` (ScriptableObject per level/mode), `GameSession` (mode Campaign/Endless/Daily + level chosen in the menu).
  - `Progress/`: `Boosters` (stock), `PlayerStats` (lifetime stats, JSON in PlayerPrefs), `Achievements` (list + unlock + rewards, pt-BR content inline), `DailyChallenge` (date seed, best result, once-a-day reward), `Leaderboards` (`ILeaderboardService` + `LocalLeaderboardService`, top 10 per board). `GameController` runs a level/run with the GDD 8.5 state machine and replays `Resolver` results on screen. Keep rules in the logic classes, not the controller.
  - `Gameplay/View/`: uGUI side: `BoardView` (tiles, ghost), `TileView` (hint colors, bomb/star icons), `BoardInput` (gestures), `GameHud`, `NextPiecePreview`, `BoosterBar`, `WordFoundOverlay` (clear banner + meaning), `GlossaryPanel` (meanings), `LevelBriefingPanel`, `LevelCompletePanel`, `GameOverPanel`.
  - `Screens/` (scene controllers: startup screens, `MainMenuScreen`, `LevelMapScreen`; menu panels `DailyChallengePanel`, `AchievementsPanel`/`AchievementRow`, `LeaderboardPanel`), `UI/` (`SafeArea`, `LegalTextPanel`, `ConfirmDialog`, `Toast`, `ButtonFeedback` press squash, `PopIn` panel entrance, `UiParticles` pooled uGUI bursts, `FloatingText` rising labels, `Shaker`), `Audio/` (`MusicPlayer` singleton, `SoundEffects` synthesized at runtime, `Haptics`).
- `Assets/Editor/` → assembly `WordDrop.Editor`. `ProjectSetup` (menu **Word Drop**) applies player/import settings, runs `LevelCatalog.CreateMissing()` (level assets from GDD 5.3, plus `Endless.asset` and `Daily.asset`) and `SceneBuilder` (`SceneBuilder.cs` startup scenes + helpers, `SceneBuilder.Menu.cs` MainMenu + LevelMap, `SceneBuilder.Game.cs` Game, `SceneBuilder.Icons.cs` generated icons, `SceneBuilder.Style.cs` the style kit), which regenerates all six scenes, generated sprites and the tile prefab. **Regenerating overwrites the scenes**, so once a scene is edited by hand in the Editor, change it there instead. Level assets are never overwritten; delete one to get its catalog defaults back. `SceneSnapshots` renders scenes and panels to `Snapshots/*.png`.
- Scene flow: `Splash` → `Consent` (first launch, or when `PlayerData.CurrentLegalVersion` is bumped) → `Loading` → `MainMenu`. "Jogar" opens `LevelMap` (unlocked levels playable, current one highlighted) → `Loading` → `Game`. "Modo Infinito" and the Daily panel's "Jogar" go straight to `Game`. Leaving a campaign level returns to the map; Endless/Daily return to the menu. How to Play, Daily Challenge, Conquistas, Recordes and Configurações are panels inside MainMenu.
- Tests: `Assets/Tests/EditMode` (`WordDrop.Tests.EditMode`: rules in `GameplayLogicTests`, progress systems in `ProgressTests`) and `Assets/Tests/PlayMode` (`WordDrop.Tests.PlayMode`: a bot plays level 1, Endless and the Daily Challenge; checks row clear, level 3/5 tutorials, Bomb, Star, Daily determinism, level map locks and menu panels through `internal` hooks on `GameController`). Tests that touch PlayerPrefs save and restore every key they change; keep it that way. Run PlayMode tests without `-nographics` to also get `Snapshots/*.png` (`*_Clear`, `*_Burst`, `*_Play`, `*_End`). The test window is landscape, so the board is smaller than on a phone: size effects by `BoardView.CellSize`, not fixed canvas units.

## Gameplay rules (implemented from the GDD)

- **Difficulty** (`LevelDefinition.difficulty`; levels 1–3 Easy, 4–7 Medium, 8–10 Advanced; Daily Medium; Endless rises with speed): Easy deals I, O + small pieces (`I2`, `I3`, `V3`) and gives every piece a color hint; Medium adds T, L, J and hints half; Advanced deals all 7 tetrominoes, no small pieces, no hints.
- **Color hints**: a hint word's letters are written on the long row of one or more consecutive pieces in one of 5 colors (`Piece.Hints`); same-color tiles placed side by side in order spell the word. Unhinted tiles are a neutral slate color; color never means anything else.
- **Board** 10×20. **Pieces** are 2–4 tiles, with a letter on every tile. Spawn at the top center; the HUD shows the next piece. Tap rotates 90° clockwise (wall kicks), drag moves sideways, drag down soft-drops, a quick flick down hard-drops. A ghost shows the landing cells and a 0.5 s lock delay (max 15 resets) allows a last adjustment. Keyboard: ←/→, ↑ rotate, ↓ soft drop, space drop, Esc pause.
- **Dictionary**: `Assets/Content/Words/palavras.txt`, NFC upper case with accents and Ç kept (`AVÓ` ≠ `AVO`; a tile `Ã` is not `A`). The header documents source, license, acceptance policy (no conjugations, plurals, abbreviations or proper nouns) and a `#! versao:` line; bump it when the list changes. The version is saved in the profile. An EditMode test checks every entry.
- **Resolution** (`Resolver`, max 20 passes): find words (3–10 letters, left→right and top→bottom) → clear them (shared tiles once) → clear full rows → gravity → repeat until stable. Gravity is per level: `Independent` (default, GDD 12.2: every unsupported tile falls, also right after a piece locks) or `AboveClearedOnly`.
- **Scoring** (`Scoring`, integers): word = 10·L²; row = +150; each pass = (words + rows) × chain multiplier (×1, ×1.5, ×2, max ×2.5); then once per placement a multiword bonus (+100 for 2 words, +250 for 3+), not multiplied.
- **Campaign**: `LevelDefinition` holds objectives (score, words, rows, chains; all must be met), an optional move limit, star thresholds (completing gives at least 1 star), fall speed, word-piece chance, piece weights, an optional curated opening (`"SHAPE LETTERS [ROTATION]"`), pre-filled rows (bottom first, `.` = empty), gravity mode and a briefing tip. Completing unlocks the next level. Fails when a piece can't spawn or moves run out.
- **Endless** (`Endless.asset`): no objectives; speeds up every 10 pieces (×0.9, min 0.12 s); from piece 30, every 15 pieces a row of letters with one hole rises from the bottom. Ends when a piece can't spawn or a row can't rise; saves last score and personal best.
- **Boosters** (bottom bar; start with 3 each; earned from first 3-star level clears, the Daily reward and achievements; never sold, GDD 3.3; one per piece; off in the Daily): **Bomba** sets the piece aside and drops a bomb tile that clears 3×3 where it lands (no points); **Estrela** sets the piece aside and drops a wildcard tile (`*`, matches any letter in `WordDictionary.Match`); **Embaralhar** rerolls the piece's letters, with a word on its row when it fits. Keyboard 1/2/3.
- **Daily Challenge** (`Daily.asset`): form 12 words in 40 pieces; pieces come from `SeededRandom(yyyymmdd)` so everyone gets the same game; best result per day (completed, words, time) and the dictionary version are saved; completing allows claiming +1 of each booster once.
- **Achievements**: 15 in `Achievements.All`, checked after every piece and at the end of a run; rewards are boosters; unlocks show a toast. **Statistics** (`PlayerStats`) feed them and the achievements screen.
- **Word meanings**: every entry in `palavras.txt` has `WORD | definição`; the clear banner shows the first word's meaning, and result panels have "Significados" listing the run's words. An EditMode test requires a meaning for every word.
- **Leaderboards**: local top 10 per board (Endless, each day's Daily, each level) through `Leaderboards.Service`; swap in an online `ILeaderboardService` later (needs anti-cheat and a privacy-policy update).
- **Feedback**: clear banner (springs in; words, rows, "Combo ×1,5!", multiword bonus, meaning), floating +points over cleared tiles, tile-burst particles, board shake (clears, bombs, hard drops; sized by cell), landing squash, score count-up and star punch in the HUD, result stars landing one by one, toasts (achievements, rewards), synthesized sound effects (setting "Sons"), vibration on clears (setting "Vibração", mobile only). Pause asks for confirmation before restarting or leaving.

## Commands

Unity runs in batch mode from the CLI. The project must not be open in the Editor while these run.

```bash
UNITY=~/Unity/Hub/Editor/6000.3.1f1/Editor/Unity

# Run EditMode tests (use -testPlatform PlayMode for play mode tests)
$UNITY -batchmode -nographics -projectPath . -runTests -testPlatform EditMode \
  -testResults ./TestResults/editmode.xml -logFile -

# Run a single test or fixture (full name, regex or semicolon-separated list)
$UNITY -batchmode -nographics -projectPath . -runTests -testPlatform EditMode \
  -testFilter "Namespace.ClassName.TestName" -testResults ./TestResults/one.xml -logFile -

# Compile-check scripts (opens the project, imports, compiles, exits)
$UNITY -batchmode -nographics -projectPath . -quit -logFile -

# Regenerate project settings + scenes, then render screenshots (needs a GPU, so no -nographics)
$UNITY -batchmode -nographics -projectPath . -executeMethod WordDrop.EditorTools.ProjectSetup.RunAll -quit -logFile -
$UNITY -batchmode -projectPath . -executeMethod WordDrop.EditorTools.SceneSnapshots.CaptureAll -quit -logFile -
```


## Visual reference

**Style kit** (`SceneBuilder.Style.cs`): fonts are Fredoka (OFL, `Assets/Art/Fonts`): Medium for body text, Bold with a soft-shadow material for anything `FontStyles.Bold` (the `Label` helper swaps the font instead of faking bold). All art is generated in code: glossy `tile.png`, 3D `button.png` (face over a darker lip; `ButtonSlice`/`ButtonLipHeight` scale it to the button height), `card.png` (lit rim), `well.png` (recessed area: board, next-piece box), `soft_shadow.png`, `glow.png` and the background. Sprites are white with shading baked in, so the Image color is the brightest (gloss/rim) shade. Build UI with the helpers `MakeButton`/`ButtonFace`+`MakeClickable`, `CardPanel`, `TileFace`/`TileLetter`, `Well`, `SoftShadow` and `AddPopIn` rather than flat `RoundedPanel`s. The project renders in linear color space, so low-alpha white overlays come out much brighter than their alpha suggests; tint overlays with a navy shade instead. PlayMode screenshots of panels wait for `PopIn` to finish (`WaitForPopIn`).

`screens_draft.png` (repo root, not imported) is the UI/UX reference for screens and style: dark navy background with jungle foliage accents, rounded colorful letter tiles (purple, green, orange, blue, red), green primary buttons, and a bold playful logotype (`Assets/Art/Branding/worddrop_logo.png`). Gameplay rules in the mockup (6-column board, vertical 3-letter pieces) are outdated; follow the GDD.
