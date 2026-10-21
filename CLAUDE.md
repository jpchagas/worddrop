# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Unity project from the **Universal 2D (URP)** template. The startup flow and level 1 are playable; power-ups, Daily Challenge, How to Play and more levels are not built yet.

- **Unity version:** `6000.3.1f1` (Unity 6.3). Editor binary: `~/Unity/Hub/Editor/6000.3.1f1/Editor/Unity`.
- **Installed build targets:** Linux Standalone and WebGL only. The Android module must be added through Unity Hub before Android builds. iOS builds need a Mac.
- **Key packages** (`Packages/manifest.json`): URP 17.3, Input System 1.17 (the new Input System is the active handler; UI uses `InputSystemUIInputModule`), uGUI with TextMeshPro (essential resources imported into `Assets/TextMesh Pro/`), 2D Sprite, Tilemap and Animation, and Test Framework 1.6.
- **Player settings** (set by `ProjectSetup`): company `Cruze`, product `Word Drop`, bundle id `com.cruze.worddrop`, portrait only, Android IL2CPP + ARM64, Unity splash off.
- **Language:** all player-facing text is Brazilian Portuguese and lives in `Assets/Scripts/Core/Strings.cs`.
- **Version control:** `.gitignore` excludes generated folders (`Library/`, `Logs/`, `Temp/`, `UserSettings/`, `TestResults/`, `Snapshots/`) and IDE files (`*.slnx`, `*.csproj`). Only `Assets/`, `Packages/` and `ProjectSettings/` are source. Always commit `.meta` files together with their assets.

## Code layout

- `Assets/Scripts/` → assembly `WordDrop` (namespace `WordDrop.*`).
  - `Core/`: `SceneNames` (build order), `SceneLoader` (go to a scene via the Loading screen), `PlayerData` (PlayerPrefs: legal consent version, music on/off), `Strings`, `AppBootstrap` (60 fps).
  - `Screens/`: one controller per scene (`SplashScreen`, `ConsentScreen`, `LoadingScreen`, `MainMenuScreen`).
  - `UI/`: `SafeArea`, `LegalTextPanel`. `Audio/`: `MusicPlayer` (DontDestroyOnLoad singleton placed in MainMenu).
- `Assets/Editor/` → assembly `WordDrop.Editor`. `ProjectSetup` (menu **Word Drop**) applies player settings and import settings, then `SceneBuilder` (split across `SceneBuilder.cs` and `SceneBuilder.Game.cs`) regenerates the five scenes and the tile prefab. **Regenerating overwrites the scenes**, so once a scene is edited by hand in the Editor, change it there instead. `SceneSnapshots` renders each scene to `Snapshots/*.png`.
  - `Gameplay/`: pure logic with no UI (`Board`, `Piece`, `WordDictionary`, `WordFinder`, `PieceGenerator`, `Scoring`, `LevelDefinition` ScriptableObject) plus `GameController`, which runs a level. `Gameplay/View/` holds the uGUI side (`BoardView`, `TileView`, `BoardInput`, `GameHud`, `WordFoundOverlay`, result panels). Keep rules in the logic classes so they stay unit-testable.
- Scene flow: `Splash` → `Consent` (first launch, or when `PlayerData.CurrentLegalVersion` is bumped) → `Loading` → `MainMenu` → `Loading` → `Game`. Daily Challenge and How to Play show "Em breve!" for now.
- Tests: `Assets/Tests/EditMode` (assembly `WordDrop.Tests.EditMode`, logic unit tests) and `Assets/Tests/PlayMode` (`WordDrop.Tests.PlayMode`, a smoke test that plays level 1 through `internal` hooks on `GameController`, exposed via `Assets/Scripts/AssemblyInfo.cs`). Run PlayMode tests without `-nographics` to also get `Snapshots/Game_*.png`.

## Gameplay rules (level 1)

- 10×20 board. Pieces are the 7 Tetris tetrominoes (I, O, T, S, Z, L, J), dealt from a 7-bag, with a letter on every block. A piece spawns at the top center, and the HUD's "Próxima" box (`NextPiecePreview`) shows the one after it. Tap rotates it 90° clockwise, with simple wall kicks (`Piece.TryRotate`). Drag sideways moves it and swipe down drops it. Keyboard: ←/→, ↑ rotate, ↓ soft drop, space drop, Esc pause.
- After a piece lands, `GameController.ResolveBoard` loops until nothing clears, and each extra pass is a combo step:
  1. Words first: `WordFinder` finds dictionary words (3–10 letters) read left→right and top→bottom. They score `10·n·(n−1)` × combo. `Board.DropAbove` lets only the letters above cleared cells fall, so other overhangs stay.
  2. Then full rows (`Board.FullRows`), scored as in Tetris (100/300/500/800 × combo). `Board.CollapseRows` shifts the rows above down intact, holes included.
- Tiles hold plain letters. `Assets/Content/Words/palavras.txt` lists words of 3–10 letters (10 = board width) with accents, which are stripped for matching and kept for display. An EditMode test checks every entry normalizes to `^[A-Z]{3,10}$`.
- `PieceGenerator` puts a word on some I (4 letters), T, L and J (3 letters) pieces along their long row (blocks 0..n-1) with chance `wordPieceChance`, and spawns them at rotation 2 or 3. Rotation 0 reads the word left→right and rotation 1 top→bottom. Other letters are weighted by Portuguese frequency.
- The level ends when the pieces run out or a new piece can't spawn. One star or more passes the level. Tuning is in `Assets/Content/Levels/Level01.asset`, which the builder creates only if missing, so delete it to pick up changed defaults in `LevelDefinition`.

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


## Game design (from README and `screens_draft.png`)

Word Drop is an educational 2D mobile game that mixes a crossword with Tetris, with Candy Crush–like mechanics and look. `screens_draft.png` (repo root, not imported) is the main UI/UX reference, so check it before building screens or game logic.

- **Core loop:** a vertical piece of letter tiles (e.g. T-R-A) falls from the top onto a 6-column letter grid. Tapping rotates the piece, and the player places it on the board.
- **Word formation:** letters that line up horizontally or vertically into a valid word are highlighted ("Word Found!"), scored (e.g. +120) and cleared. The goal is to keep the board clear.
- **HUD:** score, level with a progress bar and a 3-star rating, and a pause button.
- **Power-ups (bottom bar, each with a count):** bomb, shuffle, star.
- **Screens:** splash, main menu (Play, Daily Challenge, How to Play, Settings), gameplay, word-found overlay, Level Complete (stars, score/best, words found, Next Level / Main Menu), Daily Challenge (date, words found x/y, timer, Claim Reward), How to Play (drop letters → form words → clear the board), and a Pause menu (Resume, Restart Level, Settings, Main Menu).
- **Visual style:** dark navy background with jungle foliage accents, rounded colorful letter tiles (purple, green, orange, blue, red), green primary buttons, and a bold playful logotype (`Assets/Art/Branding/worddrop_logo.png`).
