using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WordDrop.Gameplay;
using WordDrop.Gameplay.View;
using WordDrop.Progress;

namespace WordDrop.Tests
{
    /// <summary>
    /// Plays the real Game scene. Any exception or error log fails a test. When
    /// run with graphics, screenshots are saved to Snapshots/ (Game_*.png).
    /// Saved progress touched by the tests is restored afterwards.
    /// </summary>
    public class GameSmokeTests
    {
        private static readonly List<string> IntKeys = new List<string>
        {
            "campaign_unlocked_level", "endless_best", "endless_last",
            "booster_bomb", "booster_shuffle", "booster_star",
        };

        private static readonly List<string> StringKeys = new List<string>
        {
            "player_stats", "leaderboard_endless",
        };

        private readonly Dictionary<string, int?> savedInts = new Dictionary<string, int?>();
        private readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();

        static GameSmokeTests()
        {
            for (int n = 1; n <= GameSession.CampaignLevelCount; n++)
            {
                IntKeys.Add($"level_{n}_best");
                IntKeys.Add($"level_{n}_stars");
                StringKeys.Add($"leaderboard_level_{n}");
            }

            foreach (Achievement achievement in Achievements.All)
            {
                IntKeys.Add("achievement_" + achievement.Id);
            }

            string today = DailyChallenge.DateKey(DailyChallenge.Today);
            foreach (string field in new[] { "words", "seconds", "completed", "claimed" })
            {
                IntKeys.Add($"daily_{today}_{field}");
            }

            StringKeys.Add($"daily_{today}_content");
            StringKeys.Add("leaderboard_daily_" + today);
        }

        [SetUp]
        public void RememberProgress()
        {
            foreach (string key in IntKeys)
            {
                savedInts[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            }

            foreach (string key in StringKeys)
            {
                savedStrings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            }
        }

        [TearDown]
        public void RestoreProgress()
        {
            Time.timeScale = 1f;
            foreach (var (key, value) in savedInts)
            {
                if (value.HasValue)
                {
                    PlayerPrefs.SetInt(key, value.Value);
                }
                else
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            foreach (var (key, value) in savedStrings)
            {
                if (value != null)
                {
                    PlayerPrefs.SetString(key, value);
                }
                else
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            PlayerPrefs.Save();
            PlayerStats.Reload();
        }

        [UnityTest]
        public IEnumerator Level1_PlaysThroughToAResultPanel()
        {
            yield return LoadGame(GameMode.Campaign, 0);
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.AreEqual(GameController.State.Ready, controller.CurrentState, "The briefing waits for 'Começar'.");
            yield return WaitForPopIn();
            Capture("Game_Briefing");

            controller.StartLevel();
            yield return PlayUntilEnd(controller, "Game");
        }

        [UnityTest]
        public IEnumerator Endless_PlaysUntilTheBoardFills()
        {
            yield return LoadGame(GameMode.Endless, 0);
            var controller = Object.FindFirstObjectByType<GameController>();
            controller.StartLevel();
            yield return PlayUntilEnd(controller, "Endless");
            Assert.AreEqual(GameController.State.GameOver, controller.CurrentState, "Endless only ends with game over.");
        }

        [UnityTest]
        public IEnumerator FullBottomRow_IsClearedAndScored()
        {
            yield return LoadGame(GameMode.Campaign, 0);
            var controller = Object.FindFirstObjectByType<GameController>();
            controller.StartLevel();
            Piece piece = controller.CurrentPiece;

            // Fill row 0 with X (in no word), leaving holes exactly under the
            // piece's lowest blocks, then drop the piece into them.
            int lowest = int.MaxValue;
            for (int i = 0; i < piece.BlockCount; i++)
            {
                lowest = Mathf.Min(lowest, piece.CellOf(i).y);
            }

            var holes = new HashSet<int>();
            for (int i = 0; i < piece.BlockCount; i++)
            {
                if (piece.CellOf(i).y == lowest)
                {
                    holes.Add(piece.CellOf(i).x);
                }
            }

            for (int x = 0; x < 10; x++)
            {
                if (!holes.Contains(x))
                {
                    controller.PlaceLetter(new Vector2Int(x, 0), 'X');
                }
            }

            controller.HardDrop();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (!controller.IsFalling && !controller.HasEnded && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(controller.Score, Scoring.RowBonus, "A full row is worth the row bonus.");
            Assert.IsTrue(controller.IsFalling, "The next piece should spawn after the clear.");
        }

        [UnityTest]
        public IEnumerator Level3_FirstPieceFillsTheTutorialRow()
        {
            yield return DropFirstPiece(levelIndex: 2);
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.AreEqual(Scoring.RowBonus, controller.Score, "The O piece completes the pre-filled bottom row.");
        }

        [UnityTest]
        public IEnumerator Level5_FirstPieceTriggersTheTutorialCombo()
        {
            yield return DropFirstPiece(levelIndex: 4);
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.AreEqual(GameController.State.LevelCompleted, controller.CurrentState,
                "SOL then MEL is a combo, which is the level's whole objective.");
        }

        /// <summary>Starts a campaign level and hard-drops its first piece without moving or rotating it.</summary>
        private static IEnumerator DropFirstPiece(int levelIndex)
        {
            yield return LoadGame(GameMode.Campaign, levelIndex);
            var controller = Object.FindFirstObjectByType<GameController>();
            controller.StartLevel();
            controller.HardDrop();

            float timeout = Time.realtimeSinceStartup + 15f;
            while (!controller.IsFalling && !controller.HasEnded && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Bomb_ClearsTheAreaAroundWhereItLands()
        {
            yield return LoadGame(GameMode.Campaign, 3);
            var controller = Object.FindFirstObjectByType<GameController>();
            Boosters.Add(BoosterType.Bomb, 1);
            controller.StartLevel();

            for (int x = 0; x < 10; x++)
            {
                controller.PlaceLetter(new Vector2Int(x, 0), 'X');
            }

            int bombs = Boosters.Count(BoosterType.Bomb);
            controller.UseBomb();
            Assert.AreEqual(PieceType.Single, controller.CurrentPiece.Type);
            Assert.AreEqual(bombs - 1, Boosters.Count(BoosterType.Bomb));
            int column = controller.CurrentPiece.X;

            yield return DropAndSettle(controller);

            Board board = controller.BoardForTests;
            Assert.AreEqual(Board.Empty, board[column, 0], "Under the bomb is cleared.");
            Assert.AreEqual(Board.Empty, board[column - 1, 0]);
            Assert.AreEqual(Board.Empty, board[column + 1, 0]);
            Assert.AreEqual('X', board[column + 2, 0], "Two columns away is untouched.");
        }

        [UnityTest]
        public IEnumerator Star_CompletesAWordAsAnyLetter()
        {
            yield return LoadGame(GameMode.Campaign, 3);
            var controller = Object.FindFirstObjectByType<GameController>();
            Boosters.Add(BoosterType.Star, 1);
            controller.StartLevel();

            controller.UseStar();
            int column = controller.CurrentPiece.X;
            controller.PlaceLetter(new Vector2Int(column - 1, 0), 'S');
            controller.PlaceLetter(new Vector2Int(column + 1, 0), 'L');

            yield return DropAndSettle(controller);

            Assert.GreaterOrEqual(controller.Score, Scoring.WordPoints(3), "S + star + L is SOL.");
        }

        [UnityTest]
        public IEnumerator Daily_DealsTheSamePiecesEveryTimeAndPlaysThrough()
        {
            yield return LoadGame(GameMode.Daily, 0);
            string first = Signature(Object.FindFirstObjectByType<GameController>());

            yield return LoadGame(GameMode.Daily, 0);
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.AreEqual(first, Signature(controller), "Same day, same pieces.");

            controller.StartLevel();
            Assert.IsFalse(Object.FindFirstObjectByType<BoosterBar>().GetComponent<CanvasGroup>().interactable,
                "Boosters are off in the Daily Challenge.");
            yield return PlayUntilEnd(controller, "Daily");
        }

        [UnityTest]
        public IEnumerator LevelMap_LocksLevelsNotReachedYet()
        {
            PlayerPrefs.SetInt("campaign_unlocked_level", 2);
            SceneManager.LoadScene("LevelMap");
            yield return null;
            yield return null;

            Assert.IsTrue(GameObject.Find("Level1").GetComponent<Button>().interactable);
            Assert.IsTrue(GameObject.Find("Level2").GetComponent<Button>().interactable);
            Assert.IsFalse(GameObject.Find("Level3").GetComponent<Button>().interactable);
            Capture("LevelMap");
        }

        [UnityTest]
        public IEnumerator MainMenu_PanelsOpenWithContent()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;

            var daily = Object.FindFirstObjectByType<Screens.DailyChallengePanel>(FindObjectsInactive.Include);
            daily.Open();
            yield return WaitForPopIn();
            Capture("Menu_Daily");
            daily.Close();

            var achievements = Object.FindFirstObjectByType<Screens.AchievementsPanel>(FindObjectsInactive.Include);
            achievements.Open();
            yield return WaitForPopIn();
            Capture("Menu_Achievements");
            achievements.Close();

            var records = Object.FindFirstObjectByType<Screens.LeaderboardPanel>(FindObjectsInactive.Include);
            records.Open();
            yield return WaitForPopIn();
            Capture("Menu_Leaderboards");
            records.Close();

            Object.FindFirstObjectByType<Screens.MainMenuScreen>().OpenHowToPlay();
            yield return WaitForPopIn();
            Capture("Menu_HowToPlay");
        }

        private static string Signature(GameController controller)
        {
            Piece next = controller.NextPieceForTests;
            return $"{next.Type}{new string(next.Letters)}{next.Rotation}{string.Join("", next.Hints)}";
        }

        private static IEnumerator DropAndSettle(GameController controller)
        {
            controller.HardDrop();
            float timeout = Time.realtimeSinceStartup + 15f;
            while (!controller.IsFalling && !controller.HasEnded && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
        }

        private static IEnumerator LoadGame(GameMode mode, int levelIndex)
        {
            GameSession.Mode = mode;
            GameSession.LevelIndex = levelIndex;
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
        }

        /// <summary>A simple bot: spreads pieces over the columns, rotates once and drops.</summary>
        private static IEnumerator PlayUntilEnd(GameController controller, string screenshotPrefix)
        {
            var banner = Object.FindFirstObjectByType<WordFoundOverlay>().GetComponent<CanvasGroup>();
            Time.timeScale = 4f;
            int dropped = 0;
            bool capturedClear = false;
            float timeout = Time.realtimeSinceStartup + 150f;

            while (!controller.HasEnded && Time.realtimeSinceStartup < timeout)
            {
                if (!capturedClear && banner.alpha > 0.95f)
                {
                    capturedClear = true;
                    Capture(screenshotPrefix + "_Clear");
                }

                if (controller.IsFalling)
                {
                    // Let it fall a moment so the screenshot shows a piece in the air.
                    yield return new WaitForSeconds(0.4f);
                    if (!controller.IsFalling)
                    {
                        continue;
                    }

                    if (dropped == 10)
                    {
                        Capture(screenshotPrefix + "_Play");
                    }

                    int targetColumn = dropped * 3 % 8;
                    for (int i = 0; i < 10 && controller.PieceColumn != targetColumn; i++)
                    {
                        controller.Shift(targetColumn > controller.PieceColumn ? 1 : -1);
                    }

                    controller.Rotate();
                    controller.HardDrop();
                    dropped++;
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsTrue(controller.HasEnded, "The run did not finish in time.");
            yield return WaitForPopIn();
            Capture(screenshotPrefix + "_End");
            Debug.Log($"[Word Drop] {screenshotPrefix}: dropped {dropped} pieces, ended in {controller.CurrentState} with {controller.Score} points.");
        }

        /// <summary>Panels play a short entrance (UI.PopIn); screenshots wait for it to finish.</summary>
        private static IEnumerator WaitForPopIn()
        {
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private static void Capture(string fileName)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }

            const int width = 1080;
            const int height = 1920;
            Camera camera = Camera.main;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Snapshots");
            File.WriteAllBytes(Path.Combine("Snapshots", fileName + ".png"), image.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.Destroy(image);
            Object.Destroy(target);
        }
    }
}
