using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Gameplay.View;
using WordDrop.Progress;
using WordDrop.UI;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// Runs a campaign level, an Endless run or the Daily Challenge. Follows the
    /// explicit states of GDD 8.5 so input never interferes with clearing and scoring:
    /// Initializing → Ready → Falling ⇄ Locking → Resolving → CheckingObjectives → Ready,
    /// plus Paused, LevelCompleted and GameOver.
    /// Keyboard controls for testing on a computer: ←/→ move, ↑ rotate 90°,
    /// ↓ soft drop, space hard drop, Esc pause, 1/2/3 boosters.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public enum State { Initializing, Ready, Falling, Locking, Resolving, CheckingObjectives, Paused, LevelCompleted, GameOver }

        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private LevelDefinition endlessRules;
        [SerializeField] private LevelDefinition dailyRules;
        [SerializeField] private TextAsset wordList;

        [Header("View")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardInput boardInput;
        [SerializeField] private GameHud hud;
        [SerializeField] private NextPiecePreview nextPiecePreview;
        [SerializeField] private BoosterBar boosterBar;
        [SerializeField] private WordFoundOverlay clearBanner;
        [SerializeField] private LevelBriefingPanel briefingPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private TMP_Text pauseMusicLabel;
        [SerializeField] private TMP_Text pauseSoundLabel;
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField] private LevelCompletePanel levelCompletePanel;
        [SerializeField] private GameOverPanel gameOverPanel;
        [SerializeField] private GlossaryPanel glossaryPanel;
        [SerializeField] private Toast toast;
        [Tooltip("Labels of the buttons that leave the game; they say 'Mapa de níveis' in the campaign.")]
        [SerializeField] private TMP_Text[] exitLabels;
        [SerializeField] private SoundEffects sounds;
        [Tooltip("Shakes the board on clears, bombs and hard drops.")]
        [SerializeField] private Shaker boardShaker;
        [SerializeField] private Color pointsColor = new Color32(0xFF, 0xC8, 0x3A, 0xFF);

        [Header("Timing")]
        [SerializeField] private float softDropMultiplier = 12f;
        [Tooltip("Time a landed piece can still be moved before it locks (GDD 1.2).")]
        [SerializeField] private float lockDelay = 0.5f;
        [Tooltip("How many moves/rotations can restart the lock delay, so a piece can't stall forever.")]
        [SerializeField] private int maxLockResets = 15;
        [SerializeField] private float clearShowSeconds = 1.0f;
        [Tooltip("Extra time on the banner when it shows a word's meaning.")]
        [SerializeField] private float definitionExtraSeconds = 0.8f;
        [SerializeField] private float settleSeconds = 0.25f;

        private LevelDefinition level;
        private GameMode mode;
        private WordDictionary dictionary;
        private PieceGenerator generator;
        private System.Random random;
        private Board board;
        private Piece piece;
        private Piece nextPiece;
        /// <summary>A piece set aside by the Bomb or Star booster; it comes back next.</summary>
        private Piece heldPiece;
        private ObjectiveTracker tracker;

        private State state;
        private State stateBeforePause;
        private float fallInterval;
        private float fallTimer;
        private float lockTimer;
        private float playSeconds;
        private int lockResets;
        private int dragStartColumn;
        private int dragStartRow;
        private int movesUsed;
        private int speedLevel = 1;
        private bool boosterUsedOnPiece;
        private readonly List<string> wordsFound = new List<string>();

        private bool IsEndless => mode == GameMode.Endless;
        private bool IsDaily => mode == GameMode.Daily;
        private bool BoostersAllowed => !IsDaily;
        private bool PieceActive => state == State.Falling || state == State.Locking;

        // ---------------------------------------------------------------- Setup

        private void Start()
        {
            state = State.Initializing;

            mode = GameSession.Mode;
            level = mode == GameMode.Endless ? endlessRules
                : mode == GameMode.Daily ? dailyRules
                : levels[Mathf.Clamp(GameSession.LevelIndex, 0, levels.Length - 1)];
            dictionary = WordDictionary.FromText(wordList.text);
            PlayerData.ContentVersion = dictionary.Version;

            // The Daily Challenge uses the date as seed, so everyone gets the same pieces.
            random = IsDaily ? new SeededRandom(DailyChallenge.Seed(DailyChallenge.Today)) : new System.Random();
            generator = new PieceGenerator(dictionary, level.WordPieceChance, level.Difficulty, level.PieceSequence, random);
            board = new Board(level.BoardWidth, level.BoardHeight);
            board.LoadRows(level.PrefillRows);
            tracker = new ObjectiveTracker(level.Objectives);
            fallInterval = level.FallInterval;

            boardView.Init(board.Width, board.Height);
            boardView.ShowBoard(board);
            boardInput.Tapped += Rotate;
            boardInput.DragStarted += () =>
            {
                dragStartColumn = piece?.X ?? 0;
                dragStartRow = piece?.Y ?? 0;
            };
            boardInput.DraggedColumns += DragToColumn;
            boardInput.DraggedRows += DragDown;
            boardInput.Flicked += HardDrop;

            pausePanel.SetActive(false);
            confirmDialog.gameObject.SetActive(false);
            levelCompletePanel.gameObject.SetActive(false);
            gameOverPanel.gameObject.SetActive(false);
            glossaryPanel.gameObject.SetActive(false);
            foreach (TMP_Text label in exitLabels)
            {
                label.text = mode == GameMode.Campaign ? Strings.LevelMap : Strings.MainMenu;
            }

            string title = IsEndless ? Strings.Endless
                : IsDaily ? Strings.DailyChallenge
                : string.Format(Strings.LevelFormat, level.LevelNumber);
            hud.SetTitle(title);
            boosterBar.SetAvailable(BoostersAllowed);
            RefreshHud();

            nextPiece = generator.Next();
            nextPiecePreview.Show(nextPiece);

            state = State.Ready;
            string objective = IsEndless ? Strings.EndlessGoal
                : IsDaily ? $"{Strings.FormatDate(DailyChallenge.Today)}\n{Strings.DailyGoal}\n{ObjectiveText.DescribeAll(level.Objectives)}"
                : ObjectiveText.DescribeAll(level.Objectives);
            briefingPanel.Show(title, objective, level.Tip);
        }

        /// <summary>"Começar" on the briefing.</summary>
        public void StartLevel()
        {
            if (state != State.Ready || piece != null)
            {
                return;
            }

            briefingPanel.Hide();
            SpawnPiece();
        }

        // ---------------------------------------------------------------- Loop

        private void Update()
        {
            ReadKeyboard();
            if (state == State.Falling || state == State.Locking || state == State.Resolving || state == State.CheckingObjectives)
            {
                playSeconds += Time.deltaTime;
            }

            if (!PieceActive)
            {
                return;
            }

            if (state == State.Falling)
            {
                bool softDrop = Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
                fallTimer += Time.deltaTime * (softDrop ? softDropMultiplier : 1f);
                if (fallTimer >= fallInterval)
                {
                    fallTimer = 0f;
                    if (!TryMove(0, -1))
                    {
                        state = State.Locking;
                        lockTimer = 0f;
                    }
                }
            }
            else if (piece.FitsAt(board, piece.X, piece.Y - 1))
            {
                // Slid off a ledge during the lock delay: fall again.
                state = State.Falling;
            }
            else
            {
                lockTimer += Time.deltaTime;
                if (lockTimer >= lockDelay)
                {
                    Lock();
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Pause();
            }
        }

        // ---------------------------------------------------------------- Piece control

        private void SpawnPiece()
        {
            state = State.Ready;
            Piece next;
            if (heldPiece != null)
            {
                next = heldPiece;
                heldPiece = null;
            }
            else
            {
                next = nextPiece ?? generator.Next();
                nextPiece = generator.Next();
            }

            bool lastMove = level.MoveLimit > 0 && movesUsed + 1 >= level.MoveLimit;
            nextPiecePreview.Show(lastMove ? null : nextPiece);

            next.X = (board.Width - next.BoxSize) / 2;
            next.Y = board.Height - next.BoxSize;
            if (!next.FitsAt(board, next.X, next.Y))
            {
                EndGame(Strings.BoardFull);
                return;
            }

            piece = next;
            boardView.ShowPiece(piece);
            UpdateGhost();
            fallTimer = 0f;
            lockResets = 0;
            boosterUsedOnPiece = false;
            state = State.Falling;
            RefreshBoosters();
        }

        private bool TryMove(int dx, int dy)
        {
            if (!piece.FitsAt(board, piece.X + dx, piece.Y + dy))
            {
                return false;
            }

            piece.X += dx;
            piece.Y += dy;
            boardView.UpdatePiece();
            if (dx != 0)
            {
                UpdateGhost();
                ResetLockDelay();
            }

            return true;
        }

        internal void Rotate()
        {
            if (PieceActive && piece.TryRotate(board))
            {
                boardView.UpdatePiece();
                UpdateGhost();
                ResetLockDelay();
                sounds.Play(Sfx.Rotate);
            }
        }

        internal void HardDrop()
        {
            if (!PieceActive)
            {
                return;
            }

            while (TryMove(0, -1))
            {
            }

            ShakeBoard(7f, 0.15f);
            Lock();
        }

        private void DragToColumn(int columnOffset)
        {
            if (!PieceActive)
            {
                return;
            }

            int target = dragStartColumn + columnOffset;
            while (piece.X != target && TryMove(target > piece.X ? 1 : -1, 0))
            {
            }
        }

        /// <summary>Soft drop by dragging down: the piece follows the finger, row by row.</summary>
        private void DragDown(int rows)
        {
            if (!PieceActive)
            {
                return;
            }

            int target = dragStartRow - rows;
            while (piece.Y > target && TryMove(0, -1))
            {
                fallTimer = 0f;
            }
        }

        private void ResetLockDelay()
        {
            if (state == State.Locking && lockResets < maxLockResets)
            {
                lockTimer = 0f;
                lockResets++;
            }
        }

        private void UpdateGhost()
        {
            int drop = piece.DropDistance(board);
            var cells = new List<Vector2Int>(piece.BlockCount);
            for (int i = 0; i < piece.BlockCount; i++)
            {
                cells.Add(piece.CellOf(i) + Vector2Int.down * drop);
            }

            boardView.ShowGhost(cells);
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (state == State.Paused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

            if (!PieceActive)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame) TryMove(-1, 0);
            if (keyboard.rightArrowKey.wasPressedThisFrame) TryMove(1, 0);
            if (keyboard.upArrowKey.wasPressedThisFrame) Rotate();
            if (keyboard.spaceKey.wasPressedThisFrame) HardDrop();
            if (keyboard.digit1Key.wasPressedThisFrame) UseBomb();
            if (keyboard.digit2Key.wasPressedThisFrame) UseShuffle();
            if (keyboard.digit3Key.wasPressedThisFrame) UseStar();
        }

        // ---------------------------------------------------------------- Boosters

        /// <summary>The current piece is set aside and a bomb falls instead; it clears 3×3 where it lands.</summary>
        public void UseBomb() => SwapInSpecialPiece(BoosterType.Bomb, Piece.Bomb);

        /// <summary>The current piece is set aside and a wildcard tile (any letter) falls instead.</summary>
        public void UseStar() => SwapInSpecialPiece(BoosterType.Star, Piece.Wildcard);

        /// <summary>New letters for the current piece, with a word on its row when it fits.</summary>
        public void UseShuffle()
        {
            if (!CanUseBooster(BoosterType.Shuffle))
            {
                return;
            }

            Piece shuffled = generator.Reroll(piece);
            if (!shuffled.FitsAt(board, shuffled.X, shuffled.Y))
            {
                return;
            }

            Boosters.TryUse(BoosterType.Shuffle);
            ReplacePiece(shuffled);
        }

        private void SwapInSpecialPiece(BoosterType type, char tile)
        {
            if (!CanUseBooster(type) || piece.Type == PieceType.Single)
            {
                return;
            }

            var special = new Piece(PieceType.Single, new[] { tile })
            {
                X = piece.X + piece.BoxSize / 2,
                Y = piece.Y + piece.BoxSize - 1,
            };
            if (!special.FitsAt(board, special.X, special.Y))
            {
                return;
            }

            Boosters.TryUse(type);
            heldPiece = new Piece(piece.Type, piece.Letters, piece.Rotation, piece.Hints);
            ReplacePiece(special);
            nextPiecePreview.Show(heldPiece);
        }

        private bool CanUseBooster(BoosterType type) =>
            BoostersAllowed && PieceActive && !boosterUsedOnPiece && Boosters.Count(type) > 0;

        private void ReplacePiece(Piece replacement)
        {
            piece = replacement;
            boardView.ReplacePiece(piece);
            UpdateGhost();
            boosterUsedOnPiece = true;
            if (state == State.Locking)
            {
                state = State.Falling;
            }

            sounds.Play(Sfx.Bonus);
            RefreshBoosters();
        }

        /// <param name="amount">Largest offset for a 70-unit cell; scaled to the board's real size.</param>
        private void ShakeBoard(float amount, float seconds = 0.3f) => boardShaker.Shake(amount * boardView.CellSize / 70f, seconds);

        private void RefreshBoosters() => boosterBar.Refresh(BoostersAllowed && PieceActive && !boosterUsedOnPiece);

        // ---------------------------------------------------------------- Resolution

        private void Lock()
        {
            piece.PlaceOn(board);
            boardView.LockPiece();
            piece = null;
            movesUsed++;
            sounds.Play(Sfx.Lock);
            state = State.Resolving;
            RefreshBoosters();
            StartCoroutine(Resolve());
        }

        /// <summary>Replays the resolver's passes on screen (GDD 2.1), then checks the objectives.</summary>
        private IEnumerator Resolve()
        {
            yield return ExplodeBombs();

            PlacementResult result = Resolver.Resolve(board, dictionary, level.Gravity);
            if (result.HitPassLimit)
            {
                Debug.LogWarning($"[Word Drop] Resolution stopped after {Resolver.MaxPasses} passes.");
            }

            foreach (ResolutionPass pass in result.Passes)
            {
                if (pass.HasClear)
                {
                    var words = pass.Words.Select(w => w.Word).ToList();
                    wordsFound.AddRange(words);
                    foreach (string word in words)
                    {
                        PlayerStats.RecordWord(word);
                    }

                    tracker.AddScore(pass.Points);
                    RefreshHud();

                    string definition = words.Count > 0 ? dictionary.Definition(words[0]) : null;
                    boardView.Highlight(pass.ClearedCells);
                    boardView.FloatText(pass.ClearedCells, "+" + Strings.FormatNumber(pass.Points), pointsColor, 1f + 0.25f * pass.ChainIndex);
                    clearBanner.Show(BannerTitle(pass), BannerBody(words, pass.Rows.Count), pass.Points, definition);
                    sounds.Play(pass.ChainIndex > 0 ? Sfx.Combo : pass.Words.Count > 0 ? Sfx.Word : Sfx.Row);
                    Haptics.Pulse();

                    yield return WaitUnpaused(clearShowSeconds + (definition != null ? definitionExtraSeconds : 0f));

                    clearBanner.Hide();
                    boardView.Remove(pass.ClearedCells);
                    ShakeBoard(8f + 6f * pass.Rows.Count + 6f * pass.ChainIndex);
                }

                boardView.ApplyMoves(pass.Moves);
                if (pass.Moves.Count > 0)
                {
                    yield return WaitUnpaused(settleSeconds);
                }
            }

            if (result.MultiwordBonus > 0)
            {
                tracker.AddScore(result.MultiwordBonus);
                clearBanner.Show(Strings.MultiwordBonus, string.Format(Strings.MultiwordFormat, result.WordCount), result.MultiwordBonus);
                sounds.Play(Sfx.Bonus);
                yield return WaitUnpaused(clearShowSeconds * 0.7f);
                clearBanner.Hide();
            }

            tracker.Record(result);
            PlayerStats.RecordPlacement(result.RowCount, result.Chains);
            PlayerStats.Save();
            AnnounceAchievements();
            RefreshHud();

            while (state == State.Paused)
            {
                yield return null;
            }

            state = State.CheckingObjectives;
            yield return CheckObjectives();
        }

        /// <summary>A landed Bomb clears the 3×3 area around it (no points), then letters fall as usual.</summary>
        private IEnumerator ExplodeBombs()
        {
            var cleared = new List<Vector2Int>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board[x, y] != Piece.Bomb)
                    {
                        continue;
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (board.IsInside(x + dx, y + dy) && board[x + dx, y + dy] != Board.Empty)
                            {
                                cleared.Add(new Vector2Int(x + dx, y + dy));
                            }
                        }
                    }
                }
            }

            if (cleared.Count == 0)
            {
                yield break;
            }

            cleared = cleared.Distinct().ToList();
            boardView.Highlight(cleared);
            sounds.Play(Sfx.Row);
            Haptics.Pulse();
            yield return WaitUnpaused(0.35f);

            board.Clear(cleared);
            boardView.Remove(cleared);
            ShakeBoard(24f, 0.4f);
            // Independent gravity is applied by the resolver; the other mode drops only what was above.
            if (level.Gravity == GravityMode.AboveClearedOnly)
            {
                boardView.ApplyMoves(board.DropAbove(cleared));
                yield return WaitUnpaused(settleSeconds);
            }
        }

        private static string BannerTitle(ResolutionPass pass)
        {
            if (pass.ChainIndex > 0)
            {
                return string.Format(Strings.ComboFormat, Strings.FormatMultiplier(Scoring.MultiplierTenths(pass.ChainIndex)));
            }

            if (pass.Words.Count > 0)
            {
                return pass.Words.Count > 1 ? Strings.WordsFound : Strings.WordFound;
            }

            return pass.Rows.Count > 1 ? Strings.RowsCleared : Strings.RowCleared;
        }

        private static string BannerBody(List<string> words, int rows)
        {
            var parts = new List<string>();
            if (words.Count > 0)
            {
                parts.Add(string.Join("  ", words));
            }

            if (rows > 0)
            {
                parts.Add(rows == 1 ? Strings.OneRow : string.Format(Strings.RowsFormat, rows));
            }

            return string.Join(" + ", parts);
        }

        private IEnumerator CheckObjectives()
        {
            if (!IsEndless && tracker.AllComplete)
            {
                CompleteLevel();
                yield break;
            }

            if (IsEndless)
            {
                yield return IncreasePressure();
                if (state == State.GameOver)
                {
                    yield break;
                }
            }

            if (level.MoveLimit > 0 && movesUsed >= level.MoveLimit)
            {
                EndGame(Strings.OutOfMoves);
                yield break;
            }

            SpawnPiece();
        }

        /// <summary>
        /// Endless: pieces fall faster and get harder (Easy → Medium → Advanced),
        /// and rows of letters rise from the bottom (GDD 4.2).
        /// </summary>
        private IEnumerator IncreasePressure()
        {
            if (level.SpeedUpEveryPieces > 0 && movesUsed % level.SpeedUpEveryPieces == 0)
            {
                fallInterval = Mathf.Max(level.MinFallInterval, fallInterval * level.SpeedUpFactor);
                speedLevel++;
                generator.Difficulty = speedLevel <= 3 ? Difficulty.Easy
                    : speedLevel <= 6 ? Difficulty.Medium
                    : Difficulty.Advanced;
                RefreshHud();
            }

            if (level.GarbageEveryPieces <= 0 || movesUsed < level.GarbageStartPiece ||
                movesUsed % level.GarbageEveryPieces != 0)
            {
                yield break;
            }

            if (!board.CanPushRow)
            {
                EndGame(Strings.BoardFull);
                yield break;
            }

            // A row of random letters with one hole, so it can still be cleared as a full row.
            var row = new char[board.Width];
            int hole = random.Next(board.Width);
            for (int x = 0; x < row.Length; x++)
            {
                row[x] = x == hole ? Board.Empty : generator.RandomLetter();
            }

            boardView.ApplyMoves(board.PushRowFromBottom(row));
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] != Board.Empty)
                {
                    boardView.AddTile(new Vector2Int(x, 0), row[x]);
                }
            }

            yield return WaitUnpaused(settleSeconds);
        }

        private void CompleteLevel()
        {
            state = State.LevelCompleted;
            boardView.ShowGhost(null);
            sounds.Play(Sfx.Win);
            string status = $"{Strings.ObjectiveDone}: {ObjectiveText.DescribeAll(level.Objectives).Replace("\n", " · ")}";

            if (IsDaily)
            {
                RecordDailyRun(completed: true);
                levelCompletePanel.Show(Strings.DailyDone, 3, status, tracker.Score, DailyBestScore(), wordsFound, hasNextLevel: false);
            }
            else
            {
                int stars = Mathf.Max(1, level.StarsFor(tracker.Score));
                bool firstThreeStars = stars == 3 && PlayerData.Stars(level.LevelNumber) < 3;
                PlayerData.RecordLevelResult(level.LevelNumber, tracker.Score, stars);
                PlayerData.UnlockLevel(level.LevelNumber + 1);
                Leaderboards.Service.Submit(Leaderboards.Level(level.LevelNumber), tracker.Score);
                if (firstThreeStars)
                {
                    // Boosters are earned by playing well (GDD 3.3: never sold).
                    Boosters.AddEach(1);
                    toast.Show(Strings.DailyRewardToast);
                }

                bool hasNext = GameSession.LevelIndex + 1 < levels.Length;
                levelCompletePanel.Show(Strings.LevelComplete, stars, status, tracker.Score,
                    PlayerData.BestScore(level.LevelNumber), wordsFound, hasNext);
            }

            AnnounceAchievements();
        }

        private void EndGame(string reason)
        {
            state = State.GameOver;
            boardView.ShowGhost(null);
            sounds.Play(Sfx.Lose);

            bool newRecord = false;
            int best;
            if (IsEndless)
            {
                newRecord = PlayerData.RecordEndlessRun(tracker.Score);
                best = PlayerData.EndlessBest;
                Leaderboards.Service.Submit(Leaderboards.Endless, tracker.Score);
            }
            else if (IsDaily)
            {
                RecordDailyRun(completed: false);
                best = DailyBestScore();
            }
            else
            {
                PlayerData.RecordLevelResult(level.LevelNumber, tracker.Score, 0);
                best = PlayerData.BestScore(level.LevelNumber);
            }

            AnnounceAchievements();
            gameOverPanel.Show(reason, tracker.Score, best, newRecord);
        }

        private void RecordDailyRun(bool completed)
        {
            DailyChallenge.RecordRun(DailyChallenge.Today, tracker.Words, Mathf.RoundToInt(playSeconds), completed, dictionary.Version);
            Leaderboards.Service.Submit(Leaderboards.Daily(DailyChallenge.Today), tracker.Score);
        }

        private static int DailyBestScore()
        {
            var top = Leaderboards.Service.Top(Leaderboards.Daily(DailyChallenge.Today), 1);
            return top.Count > 0 ? top[0].score : 0;
        }

        private void AnnounceAchievements()
        {
            foreach (Achievement achievement in Achievements.CheckNewlyUnlocked())
            {
                toast.Show(string.Format(Strings.AchievementUnlockedFormat, achievement.Title));
            }

            RefreshBoosters();
        }

        private void RefreshHud()
        {
            hud.SetScore(tracker.Score);
            if (IsEndless)
            {
                hud.SetObjective(string.Format(Strings.EndlessBestFormat, Strings.FormatNumber(PlayerData.EndlessBest)), -1f);
                hud.SetCounter(string.Format(Strings.SpeedFormat, speedLevel));
                hud.SetStars(0, false);
            }
            else
            {
                hud.SetObjective(tracker.ProgressLine(), tracker.Fraction);
                hud.SetCounter(level.MoveLimit > 0 ? string.Format(Strings.MovesFormat, level.MoveLimit - movesUsed) : "");
                hud.SetStars(level.StarsFor(tracker.Score), !IsDaily);
            }
        }

        /// <summary>Waits, but the clock stops while the game is paused.</summary>
        private IEnumerator WaitUnpaused(float seconds)
        {
            while (seconds > 0f)
            {
                if (state != State.Paused)
                {
                    seconds -= Time.deltaTime;
                }

                yield return null;
            }
        }

        // ---------------------------------------------------------------- Buttons

        public void Pause()
        {
            if (state != State.Falling && state != State.Locking && state != State.Resolving &&
                state != State.CheckingObjectives)
            {
                return;
            }

            stateBeforePause = state;
            state = State.Paused;
            RefreshPauseLabels();
            RefreshBoosters();
            pausePanel.SetActive(true);
        }

        public void Resume()
        {
            if (state != State.Paused)
            {
                return;
            }

            confirmDialog.Cancel();
            state = stateBeforePause;
            pausePanel.SetActive(false);
            RefreshBoosters();
        }

        /// <summary>From the pause menu: asks first, since the run's progress would be lost.</summary>
        public void RequestRestart() => confirmDialog.Show(Strings.ConfirmRestart, Restart);

        public void RequestExit() => confirmDialog.Show(Strings.ConfirmMainMenu, Exit);

        public void Restart() => SceneManager.LoadScene(SceneNames.Game);

        public void NextLevel()
        {
            GameSession.LevelIndex++;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        /// <summary>Leaves the game: to the level map in the campaign, to the main menu otherwise.</summary>
        public void Exit() => SceneLoader.LoadWithLoadingScreen(mode == GameMode.Campaign ? SceneNames.LevelMap : SceneNames.MainMenu);

        /// <summary>Opens the list of this run's words and their meanings.</summary>
        public void ShowMeanings()
        {
            var lines = wordsFound.Distinct()
                .Select(word => $"<b>{word}</b>\n{dictionary.Definition(word) ?? ""}")
                .ToList();
            glossaryPanel.Show(lines.Count > 0 ? string.Join("\n\n", lines) : Strings.MeaningsEmpty);
        }

        public void ToggleMusic()
        {
            MusicPlayer.ToggleMusic();
            RefreshPauseLabels();
        }

        public void ToggleSound()
        {
            PlayerData.SoundOn = !PlayerData.SoundOn;
            RefreshPauseLabels();
        }

        private void RefreshPauseLabels()
        {
            pauseMusicLabel.text = PlayerData.MusicOn ? Strings.MusicOn : Strings.MusicOff;
            pauseSoundLabel.text = PlayerData.SoundOn ? Strings.SoundOn : Strings.SoundOff;
        }

        // ---------------------------------------------------------------- Test hooks (see AssemblyInfo.cs)

        internal State CurrentState => state;
        internal bool IsFalling => PieceActive;
        internal bool HasEnded => state == State.LevelCompleted || state == State.GameOver;
        internal int PieceColumn => piece?.X ?? -1;
        internal Piece CurrentPiece => piece;
        internal Piece NextPieceForTests => nextPiece;
        internal int Score => tracker.Score;
        internal Board BoardForTests => board;

        internal void Shift(int dx)
        {
            if (PieceActive)
            {
                TryMove(dx, 0);
            }
        }

        /// <summary>Puts a letter straight on the board (tests set up positions with it).</summary>
        internal void PlaceLetter(Vector2Int cell, char letter)
        {
            board[cell] = letter;
            boardView.AddTile(cell, letter);
        }
    }
}
