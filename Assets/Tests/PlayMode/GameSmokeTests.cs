using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WordDrop.Gameplay;
using WordDrop.Gameplay.View;

namespace WordDrop.Tests
{
    /// <summary>
    /// Plays level 1 to the end by spreading pieces over the columns and dropping
    /// them. Any exception or error log fails the test. When run with graphics, it
    /// also saves screenshots to Snapshots/ (Game_Play, Game_WordFound, Game_End).
    /// </summary>
    public class GameSmokeTests
    {
        private static readonly string[] SavedKeys = { "level_1_best", "level_1_stars" };
        private int[] savedValues;

        [SetUp]
        public void RememberProgress() => savedValues = System.Array.ConvertAll(SavedKeys, k => PlayerPrefs.GetInt(k, 0));

        [TearDown]
        public void RestoreProgress()
        {
            Time.timeScale = 1f;
            for (int i = 0; i < SavedKeys.Length; i++)
            {
                PlayerPrefs.SetInt(SavedKeys[i], savedValues[i]);
            }

            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator Level1_PlaysThroughToAResultPanel()
        {
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<GameController>();
            var overlay = Object.FindFirstObjectByType<WordFoundOverlay>().GetComponent<CanvasGroup>();
            Assert.IsNotNull(controller);

            Time.timeScale = 4f;
            int dropped = 0;
            bool capturedWord = false;
            float timeout = Time.realtimeSinceStartup + 120f;

            while (!controller.HasEnded && Time.realtimeSinceStartup < timeout)
            {
                if (!capturedWord && overlay.alpha > 0.95f)
                {
                    capturedWord = true;
                    Capture("Game_WordFound");
                }

                if (controller.IsFalling)
                {
                    // Let it fall a moment so the screenshot shows a piece in the air.
                    yield return new WaitForSeconds(0.4f);
                    if (dropped == 10)
                    {
                        Capture("Game_Play");
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
            Assert.IsTrue(controller.HasEnded, "The level did not finish in time.");
            yield return new WaitForSeconds(0.2f);
            Capture("Game_End");
            Debug.Log($"[Word Drop] Smoke test dropped {dropped} pieces.");
        }

        [UnityTest]
        public IEnumerator FullBottomRow_IsClearedAndScored()
        {
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<GameController>();
            Piece piece = controller.CurrentPiece;

            // Fill row 0 with X (in no word that fits here), leaving holes exactly
            // under the piece's lowest blocks, then drop the piece into them.
            int lowest = int.MaxValue;
            for (int i = 0; i < Piece.BlockCount; i++)
            {
                lowest = Mathf.Min(lowest, piece.CellOf(i).y);
            }

            var holes = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < Piece.BlockCount; i++)
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
            while (!controller.IsFalling && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(controller.Score, 100, "A full row is worth at least 100 points.");
            Assert.IsTrue(controller.IsFalling, "The next piece should spawn after the clear.");
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
