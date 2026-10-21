using System;
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

namespace WordDrop.Gameplay
{
    /// <summary>
    /// Runs a level: spawns pieces, makes them fall, handles input, clears words
    /// and full rows (with combos), and ends the level with a result panel.
    /// Keyboard controls for testing on a computer: ←/→ move, ↑ rotate 90°,
    /// ↓ fall faster, space drop, Esc pause.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private enum State { Falling, Resolving, Paused, Ended }

        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private TextAsset wordList;

        [Header("View")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardInput boardInput;
        [SerializeField] private GameHud hud;
        [SerializeField] private NextPiecePreview nextPiecePreview;
        [SerializeField] private WordFoundOverlay wordFoundOverlay;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private TMP_Text pauseMusicLabel;
        [SerializeField] private LevelCompletePanel levelCompletePanel;
        [SerializeField] private GameOverPanel gameOverPanel;

        [Header("Timing")]
        [SerializeField] private float softDropMultiplier = 12f;
        [SerializeField] private float wordShowSeconds = 1.1f;
        [SerializeField] private float settleSeconds = 0.3f;

        private LevelDefinition level;
        private WordDictionary dictionary;
        private PieceGenerator generator;
        private Board board;
        private Piece piece;
        private Piece nextPiece;

        private State state;
        private State stateBeforePause;
        private float fallTimer;
        private int dragStartColumn;
        private int score;
        private int piecesLeft;
        private readonly List<string> wordsFound = new List<string>();

        private void Start()
        {
            level = levels[Mathf.Clamp(LevelSelection.CurrentIndex, 0, levels.Length - 1)];
            dictionary = WordDictionary.FromText(wordList.text);
            generator = new PieceGenerator(dictionary, level.WordPieceChance, new System.Random());
            board = new Board(level.BoardWidth, level.BoardHeight);
            piecesLeft = level.Pieces;

            boardView.Init(board.Width, board.Height);
            boardInput.Tapped += Rotate;
            boardInput.DragStarted += () => dragStartColumn = piece?.X ?? 0;
            boardInput.DraggedColumns += DragTo;
            boardInput.SwipedDown += HardDrop;

            pausePanel.SetActive(false);
            levelCompletePanel.gameObject.SetActive(false);
            gameOverPanel.gameObject.SetActive(false);

            hud.SetLevel(level.LevelNumber);
            hud.SetScore(0, level);
            hud.SetPiecesLeft(piecesLeft);

            SpawnPiece();
        }

        private void Update()
        {
            ReadKeyboard();
            if (state != State.Falling)
            {
                return;
            }

            bool softDrop = Keyboard.current != null && Keyboard.current.downArrowKey.isPressed;
            fallTimer += Time.deltaTime * (softDrop ? softDropMultiplier : 1f);
            if (fallTimer >= level.FallInterval)
            {
                fallTimer = 0f;
                if (!TryMove(0, -1))
                {
                    Land();
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
            Piece next = nextPiece ?? generator.Next();
            nextPiece = generator.Next();
            nextPiecePreview.Show(piecesLeft > 1 ? nextPiece : null);
            next.X = (board.Width - next.BoxSize) / 2;
            next.Y = board.Height - next.BoxSize;

            if (!next.FitsAt(board, next.X, next.Y))
            {
                FinishLevel(Strings.BoardFull);
                return;
            }

            piece = next;
            boardView.ShowPiece(piece);
            fallTimer = 0f;
            state = State.Falling;
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
            return true;
        }

        // Hooks for the play mode smoke test (see AssemblyInfo.cs).
        internal bool IsFalling => state == State.Falling;
        internal bool HasEnded => state == State.Ended;
        internal int PieceColumn => piece?.X ?? -1;
        internal Piece CurrentPiece => piece;
        internal int Score => score;

        /// <summary>Puts a letter straight on the board (tests set up positions with it).</summary>
        internal void PlaceLetter(Vector2Int cell, char letter)
        {
            board[cell] = letter;
            boardView.AddTile(cell, letter);
        }
        internal void Shift(int dx)
        {
            if (state == State.Falling)
            {
                TryMove(dx, 0);
            }
        }

        internal void Rotate()
        {
            if (state != State.Falling)
            {
                return;
            }

            if (piece.TryRotate(board))
            {
                boardView.UpdatePiece();
            }
        }

        private void DragTo(int columnOffset)
        {
            if (state != State.Falling)
            {
                return;
            }

            int target = dragStartColumn + columnOffset;
            while (piece.X != target && TryMove(target > piece.X ? 1 : -1, 0))
            {
            }
        }

        internal void HardDrop()
        {
            if (state != State.Falling)
            {
                return;
            }

            while (TryMove(0, -1))
            {
            }

            Land();
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

            if (state != State.Falling)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame) TryMove(-1, 0);
            if (keyboard.rightArrowKey.wasPressedThisFrame) TryMove(1, 0);
            if (keyboard.upArrowKey.wasPressedThisFrame) Rotate();
            if (keyboard.spaceKey.wasPressedThisFrame) HardDrop();
        }

        // ---------------------------------------------------------------- Words

        private void Land()
        {
            piece.PlaceOn(board);
            boardView.LockPiece();
            piece = null;
            state = State.Resolving;
            StartCoroutine(ResolveBoard());
        }

        /// <summary>
        /// Clears words, then full rows, over and over until nothing is left to
        /// clear. Every pass after the first is a combo worth more points.
        /// </summary>
        private IEnumerator ResolveBoard()
        {
            for (int combo = 1; ; combo++)
            {
                List<FoundWord> found = WordFinder.Find(board, dictionary);
                if (found.Count > 0)
                {
                    List<string> display = found.Select(w => dictionary.Display(w.Key)).ToList();
                    List<Vector2Int> cells = found.SelectMany(w => w.Cells).Distinct().ToList();
                    wordsFound.AddRange(display);

                    string title = combo > 1 ? string.Format(Strings.ComboFormat, combo)
                        : found.Count > 1 ? Strings.WordsFound : Strings.WordFound;
                    int points = found.Sum(w => Scoring.PointsFor(w.Key.Length, combo));
                    yield return ClearCells(cells, title, string.Join("  ", display), points, () => board.DropAbove(cells));
                    continue;
                }

                List<int> rows = board.FullRows();
                if (rows.Count > 0)
                {
                    string title = combo > 1 ? string.Format(Strings.ComboFormat, combo)
                        : rows.Count > 1 ? Strings.RowsCleared : Strings.RowCleared;
                    string body = rows.Count > 1 ? string.Format(Strings.RowsFormat, rows.Count) : Strings.OneRow;
                    int points = Scoring.PointsForRows(rows.Count, combo);
                    yield return ClearCells(board.CellsInRows(rows), title, body, points, () => board.CollapseRows(rows));
                    continue;
                }

                break;
            }

            piecesLeft--;
            hud.SetPiecesLeft(piecesLeft);

            if (piecesLeft > 0)
            {
                SpawnPiece();
            }
            else
            {
                FinishLevel(Strings.NotEnoughPoints);
            }
        }

        /// <summary>Scores and highlights the cells, shows the banner, then removes them and lets the rest fall.</summary>
        private IEnumerator ClearCells(List<Vector2Int> cells, string title, string body, int points,
            Func<List<(Vector2Int from, Vector2Int to)>> collapse)
        {
            score += points;
            hud.SetScore(score, level);
            boardView.Highlight(cells);
            wordFoundOverlay.Show(title, body, points);

            yield return WaitUnpaused(wordShowSeconds);

            wordFoundOverlay.Hide();
            board.Clear(cells);
            boardView.Remove(cells);
            boardView.ApplyMoves(collapse());

            yield return WaitUnpaused(settleSeconds);
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

        // ---------------------------------------------------------------- End of level

        /// <summary>
        /// The level ends when the pieces run out or the board fills up.
        /// Either way, one star or more means the level is passed.
        /// </summary>
        private void FinishLevel(string reasonIfLost)
        {
            if (level.StarsFor(score) > 0)
            {
                Win();
            }
            else
            {
                Lose(reasonIfLost);
            }
        }

        private void Win()
        {
            state = State.Ended;
            int stars = level.StarsFor(score);
            PlayerData.RecordLevelResult(level.LevelNumber, score, stars);
            bool hasNext = LevelSelection.CurrentIndex + 1 < levels.Length;
            levelCompletePanel.Show(stars, score, PlayerData.BestScore(level.LevelNumber), wordsFound, hasNext);
        }

        private void Lose(string reason)
        {
            state = State.Ended;
            PlayerData.RecordLevelResult(level.LevelNumber, score, 0);
            gameOverPanel.Show(reason);
        }

        // ---------------------------------------------------------------- Buttons

        public void Pause()
        {
            if (state == State.Paused || state == State.Ended)
            {
                return;
            }

            stateBeforePause = state;
            state = State.Paused;
            RefreshMusicLabel();
            pausePanel.SetActive(true);
        }

        public void Resume()
        {
            if (state != State.Paused)
            {
                return;
            }

            state = stateBeforePause;
            pausePanel.SetActive(false);
        }

        public void Restart() => SceneManager.LoadScene(SceneNames.Game);

        public void NextLevel()
        {
            LevelSelection.CurrentIndex++;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        public void GoToMainMenu() => SceneLoader.LoadWithLoadingScreen(SceneNames.MainMenu);

        public void ToggleMusic()
        {
            MusicPlayer.ToggleMusic();
            RefreshMusicLabel();
        }

        private void RefreshMusicLabel()
        {
            pauseMusicLabel.text = PlayerData.MusicOn ? Strings.MusicOn : Strings.MusicOff;
        }
    }
}
