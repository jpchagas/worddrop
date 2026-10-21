using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;
using WordDrop.Gameplay;
using WordDrop.Gameplay.View;

namespace WordDrop.EditorTools
{
    /// <summary>The Game scene: HUD, board, word banner, pause and result panels.</summary>
    public static partial class SceneBuilder
    {
        private const string StarSpritePath = UiArtFolder + "/star.png";
        private const string TilePrefabPath = "Assets/Prefabs/Tile.prefab";
        private const string Level1Path = "Assets/Content/Levels/Level01.asset";
        private const string WordListPath = "Assets/Content/Words/palavras.txt";

        private static readonly Color BoardBackground = Hex(0x0E1A35);
        private static readonly Color WordGreen = Hex(0x5BE06A);
        private static readonly Color BannerBackground = Hex(0x0A1226, 0.94f);

        private static void BuildGame()
        {
            CreateStarSprite();
            CreateTilePrefab();
            CreateLevel1IfMissing();

            // Creating a scene unloads assets that were just made in memory, so
            // load them back from disk only after the scene exists.
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var star = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
            var tilePrefab = AssetDatabase.LoadAssetAtPath<TileView>(TilePrefabPath);
            var level1 = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level1Path);
            var controller = new GameObject("GameController").AddComponent<GameController>();

            GameHud hud = BuildHud(ui.Safe, star, tilePrefab, controller, out NextPiecePreview nextPreview);

            // Play area: catches touches anywhere around the board.
            GameObject playArea = Child("PlayArea", ui.Safe, typeof(Image));
            Stretch((RectTransform)playArea.transform, 30, 50, 30, 260);
            playArea.GetComponent<Image>().color = Color.clear;

            GameObject board = RoundedPanel("Board", playArea.transform, BoardBackground, 1f);
            var fitter = board.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)level1.BoardWidth / level1.BoardHeight;

            GameObject slots = Child("Slots", board.transform);
            Stretch((RectTransform)slots.transform, 16, 16, 16, 16);
            GameObject tiles = Child("Tiles", board.transform);
            Stretch((RectTransform)tiles.transform, 16, 16, 16, 16);

            var boardView = board.AddComponent<BoardView>();
            Wire(boardView, "tilePrefab", tilePrefab);
            Wire(boardView, "slotsRoot", slots.transform);
            Wire(boardView, "tilesRoot", tiles.transform);
            Wire(boardView, "slotSprite", rounded);

            var boardInput = playArea.AddComponent<BoardInput>();
            Wire(boardInput, "boardView", boardView);

            WordFoundOverlay overlay = BuildWordFoundOverlay(ui.Safe);
            GameObject pausePanel = BuildPausePanel(ui.Safe, controller, out TMP_Text musicLabel);
            LevelCompletePanel levelComplete = BuildLevelCompletePanel(ui.Safe, star, controller);
            GameOverPanel gameOver = BuildGameOverPanel(ui.Safe, controller);

            WireArray(controller, "levels", level1);
            Wire(controller, "wordList", AssetDatabase.LoadAssetAtPath<TextAsset>(WordListPath));
            Wire(controller, "boardView", boardView);
            Wire(controller, "boardInput", boardInput);
            Wire(controller, "hud", hud);
            Wire(controller, "nextPiecePreview", nextPreview);
            Wire(controller, "wordFoundOverlay", overlay);
            Wire(controller, "pausePanel", pausePanel);
            Wire(controller, "pauseMusicLabel", musicLabel);
            Wire(controller, "levelCompletePanel", levelComplete);
            Wire(controller, "gameOverPanel", gameOver);

            Save(ui.Scene, SceneNames.Game);
        }

        private static GameHud BuildHud(Transform parent, Sprite star, TileView tilePrefab, GameController controller,
            out NextPiecePreview nextPreview)
        {
            GameObject root = Child("Hud", parent);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 240);

            // Left: score and pieces left. Sizes leave room for narrow phones,
            // where the canvas can be under 1000 units wide.
            TMP_Text scoreTitle = Label(root.transform, "ScoreTitle", Strings.Score, 34, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(scoreTitle.gameObject, new Vector2(0, 1), new Vector2(180, -50), new Vector2(280, 50));
            TMP_Text score = Label(root.transform, "ScoreLabel", "0", 64, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(score.gameObject, new Vector2(0, 1), new Vector2(180, -110), new Vector2(280, 80));
            TMP_Text pieces = Label(root.transform, "PiecesLabel", string.Format(Strings.PiecesFormat, 25), 32, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(pieces.gameObject, new Vector2(0, 1), new Vector2(180, -175), new Vector2(280, 50));

            // Center: level, progress toward 3 stars, stars.
            TMP_Text level = Label(root.transform, "LevelLabel", string.Format(Strings.LevelFormat, 1), 40, TextWhite, FontStyles.Bold);
            Place(level.gameObject, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(280, 56));

            GameObject track = RoundedPanel("ProgressTrack", root.transform, TrackDark, 2.5f);
            Place(track, new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(280, 30));
            GameObject fill = RoundedPanel("ProgressFill", track.transform, Green, 2.5f);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = StarImage(root.transform, $"Star{i + 1}", star, new Vector2(0.5f, 1), new Vector2((i - 1) * 80, -165), 64);
                stars[i].color = new Color(1f, 1f, 1f, 0.18f); // Unearned; GameHud lights them up.
            }

            // Right: next piece, then the round pause button.
            TMP_Text nextLabel = Label(root.transform, "NextLabel", Strings.Next, 28, TextMuted);
            Place(nextLabel.gameObject, new Vector2(1, 1), new Vector2(-250, -40), new Vector2(160, 40));
            GameObject nextBox = RoundedPanel("NextPiece", root.transform, TrackDark, 1.5f);
            Place(nextBox, new Vector2(1, 1), new Vector2(-250, -135), new Vector2(140, 140));
            GameObject nextTiles = Child("Tiles", nextBox.transform);
            Stretch((RectTransform)nextTiles.transform, 10, 10, 10, 10);
            nextPreview = nextBox.AddComponent<NextPiecePreview>();
            Wire(nextPreview, "tilePrefab", tilePrefab);
            Wire(nextPreview, "tilesRoot", nextTiles.transform);

            GameObject pause = RoundedPanel("PauseButton", root.transform, Secondary, 0.9f);
            Place(pause, new Vector2(1, 1), new Vector2(-80, -110), new Vector2(110, 110));
            var pauseButton = pause.AddComponent<Button>();
            pauseButton.targetGraphic = pause.GetComponent<Image>();
            foreach (float x in new[] { -14f, 14f })
            {
                GameObject bar = RoundedPanel("Bar", pause.transform, TextWhite, 6f);
                bar.GetComponent<Image>().raycastTarget = false;
                Place(bar, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(16, 50));
            }

            OnClick(pauseButton, controller.Pause);

            var hud = root.AddComponent<GameHud>();
            Wire(hud, "scoreLabel", score);
            Wire(hud, "piecesLabel", pieces);
            Wire(hud, "levelLabel", level);
            Wire(hud, "progressFill", fillRect);
            WireArray(hud, "stars", stars);
            return hud;
        }

        private static WordFoundOverlay BuildWordFoundOverlay(Transform parent)
        {
            GameObject root = RoundedPanel("WordFoundOverlay", parent, BannerBackground, 1f);
            // Over the top of the board, which is usually empty, so the word itself stays visible.
            Place(root, new Vector2(0.5f, 1f), new Vector2(0, -520), new Vector2(980, 420));
            var group = root.AddComponent<CanvasGroup>();
            VerticalStack(root, padding: 40, spacing: 10);

            TMP_Text title = Label(root.transform, "Title", Strings.WordFound, 60, Yellow, FontStyles.Bold);
            Sized(title, 80);
            TMP_Text words = Label(root.transform, "Words", "SOL", 96, WordGreen, FontStyles.Bold);
            words.enableAutoSizing = true;
            words.fontSizeMin = 40;
            words.fontSizeMax = 96;
            Sized(words, 140);
            TMP_Text points = Label(root.transform, "Points", "+60", 72, Yellow, FontStyles.Bold);
            Sized(points, 100);

            var overlay = root.AddComponent<WordFoundOverlay>();
            Wire(overlay, "group", group);
            Wire(overlay, "titleLabel", title);
            Wire(overlay, "wordsLabel", words);
            Wire(overlay, "pointsLabel", points);
            return overlay;
        }

        private static GameObject BuildPausePanel(Transform parent, GameController controller, out TMP_Text musicLabel)
        {
            GameObject root = ModalCard("PausePanel", parent, new Vector2(860, 860), out GameObject card);

            Sized(Label(card.transform, "Title", Strings.Paused, 64, TextWhite, FontStyles.Bold), 100);
            Button resume = PrimaryButton(card.transform, "ResumeButton", Strings.Resume, 140, 56, out _);
            Button restart = SecondaryButton(card.transform, "RestartButton", Strings.RestartLevel, 120, out _);
            Button music = SecondaryButton(card.transform, "MusicToggle", Strings.MusicOn, 120, out musicLabel);
            Button menu = SecondaryButton(card.transform, "MainMenuButton", Strings.MainMenu, 120, out _);

            OnClick(resume, controller.Resume);
            OnClick(restart, controller.Restart);
            OnClick(music, controller.ToggleMusic);
            OnClick(menu, controller.GoToMainMenu);

            root.SetActive(false);
            return root;
        }

        private static LevelCompletePanel BuildLevelCompletePanel(Transform parent, Sprite star, GameController controller)
        {
            GameObject root = ModalCard("LevelCompletePanel", parent, new Vector2(900, 1400), out GameObject card);

            Sized(Label(card.transform, "Title", Strings.LevelComplete, 72, TextWhite, FontStyles.Bold), 100);
            Sized(Label(card.transform, "Subtitle", Strings.GreatJob, 40, TextMuted), 50);

            GameObject starRow = Child("Stars", card.transform);
            Sized(starRow, 200);
            var stars = new[]
            {
                StarImage(starRow.transform, "Star1", star, new Vector2(0.5f, 0.5f), new Vector2(-210, -20), 150),
                StarImage(starRow.transform, "Star2", star, new Vector2(0.5f, 0.5f), new Vector2(0, 10), 190),
                StarImage(starRow.transform, "Star3", star, new Vector2(0.5f, 0.5f), new Vector2(210, -20), 150),
            };

            GameObject stats = RoundedPanel("Stats", card.transform, TrackDark, 1f);
            Sized(stats, 300);
            VerticalStack(stats, padding: 30, spacing: 10);
            TMP_Text score = StatRow(stats.transform, "ScoreRow", Strings.Score, TextWhite);
            TMP_Text best = StatRow(stats.transform, "BestRow", Strings.Best, Yellow);
            TMP_Text count = StatRow(stats.transform, "WordsRow", Strings.WordsFoundCount, TextWhite);

            TMP_Text wordList = Label(card.transform, "WordList", "SOL · MAR", 34, WordGreen, FontStyles.Bold);
            wordList.enableAutoSizing = true;
            wordList.fontSizeMin = 20;
            wordList.fontSizeMax = 34;
            Sized(wordList, 140);

            Button next = PrimaryButton(card.transform, "NextLevelButton", Strings.NextLevel, 140, 56, out _);
            Button again = PrimaryButton(card.transform, "PlayAgainButton", Strings.PlayAgain, 140, 56, out _);
            Button menu = SecondaryButton(card.transform, "MainMenuButton", Strings.MainMenu, 120, out _);
            next.gameObject.SetActive(false);

            OnClick(next, controller.NextLevel);
            OnClick(again, controller.Restart);
            OnClick(menu, controller.GoToMainMenu);

            var panel = root.AddComponent<LevelCompletePanel>();
            WireArray(panel, "stars", stars);
            Wire(panel, "scoreLabel", score);
            Wire(panel, "bestLabel", best);
            Wire(panel, "wordCountLabel", count);
            Wire(panel, "wordListLabel", wordList);
            Wire(panel, "nextLevelButton", next.gameObject);
            Wire(panel, "playAgainButton", again.gameObject);

            root.SetActive(false);
            return panel;
        }

        private static GameOverPanel BuildGameOverPanel(Transform parent, GameController controller)
        {
            GameObject root = ModalCard("GameOverPanel", parent, new Vector2(860, 760), out GameObject card);

            Sized(Label(card.transform, "Title", Strings.GameOver, 72, TextWhite, FontStyles.Bold), 100);
            TMP_Text reason = Label(card.transform, "Reason", Strings.BoardFull, 40, TextMuted);
            Sized(reason, 160);
            Button again = PrimaryButton(card.transform, "TryAgainButton", Strings.TryAgain, 140, 56, out _);
            Button menu = SecondaryButton(card.transform, "MainMenuButton", Strings.MainMenu, 120, out _);

            OnClick(again, controller.Restart);
            OnClick(menu, controller.GoToMainMenu);

            var panel = root.AddComponent<GameOverPanel>();
            Wire(panel, "reasonLabel", reason);
            root.SetActive(false);
            return panel;
        }

        // ---------------------------------------------------------------- Pieces

        /// <summary>A full-screen dimmed backdrop with a centered card that stacks its children.</summary>
        private static GameObject ModalCard(string name, Transform parent, Vector2 size, out GameObject card)
        {
            GameObject root = Child(name, parent, typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = Dim;

            card = RoundedPanel("Card", root.transform, Card, 1f);
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            VerticalStack(card, padding: 60, spacing: 30);
            return root;
        }

        private static TMP_Text StatRow(Transform parent, string name, string title, Color valueColor)
        {
            GameObject row = Child(name, parent);
            Sized(row, 70);
            TMP_Text titleLabel = Label(row.transform, "Title", title, 40, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(titleLabel.rectTransform, 20, 0, 20, 0);
            TMP_Text value = Label(row.transform, "Value", "0", 46, valueColor, FontStyles.Bold, TextAlignmentOptions.Right);
            Stretch(value.rectTransform, 20, 0, 20, 0);
            return value;
        }

        private static Image StarImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, float size)
        {
            GameObject go = Child(name, parent, typeof(Image));
            Place(go, anchor, position, new Vector2(size, size));
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Yellow;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateTilePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TilePrefabPath));

            GameObject go = RoundedPanel("Tile", null, Color.white, 1.6f);
            go.GetComponent<Image>().raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = new Vector2(140, 140);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -6);

            TMP_Text letter = Label(go.transform, "Letter", "A", 80, TextWhite, FontStyles.Bold);
            Stretch(letter.rectTransform);

            var view = go.AddComponent<TileView>();
            Wire(view, "background", go.GetComponent<Image>());
            Wire(view, "letterLabel", letter);

            PrefabUtility.SaveAsPrefabAsset(go, TilePrefabPath);
            Object.DestroyImmediate(go);
        }

        /// <summary>Creates Level 1 only if it doesn't exist, so tuning done in the Inspector is kept.</summary>
        private static void CreateLevel1IfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level1Path) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LevelDefinition>(), Level1Path);
                AssetDatabase.SaveAssets();
            }
        }

        private static void CreateStarSprite()
        {
            const int size = 256;
            const int samples = 4;
            var points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? 124f : 54f;
                points[i] = new Vector2(size / 2f + Mathf.Cos(angle) * radius, size / 2f + Mathf.Sin(angle) * radius - 6f);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            var p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                            if (InPolygon(points, p))
                            {
                                inside++;
                            }
                        }
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside / (float)(samples * samples)));
                }
            }

            WriteSprite(texture, StarSpritePath, Vector4.zero);
        }

        private static bool InPolygon(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
