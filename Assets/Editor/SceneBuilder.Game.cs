using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Gameplay;
using WordDrop.Gameplay.View;
using WordDrop.UI;

namespace WordDrop.EditorTools
{
    /// <summary>The Game scene: HUD, board, clear banner, briefing, pause, confirmation and result panels.</summary>
    public static partial class SceneBuilder
    {
        private const string StarSpritePath = UiArtFolder + "/star.png";
        private const string TilePrefabPath = "Assets/Prefabs/Tile.prefab";
        private const string WordListPath = "Assets/Content/Words/palavras.txt";

        private static readonly Color BoardBackground = Hex(0x0E1A35);
        private static readonly Color WordGreen = Hex(0x5BE06A);
        private static readonly Color BannerBackground = Hex(0x0A1226, 0.94f);

        private static void BuildGame()
        {
            // Creating a scene unloads assets that were just made in memory, so
            // load them back from disk only after the scene exists.
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var star = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
            var tilePrefab = AssetDatabase.LoadAssetAtPath<TileView>(TilePrefabPath);
            LevelDefinition[] levels = Enumerable.Range(1, GameSession.CampaignLevelCount)
                .Select(n => AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelCatalog.LevelPath(n)))
                .ToArray();
            var endless = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelCatalog.EndlessPath);
            var daily = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelCatalog.DailyPath);

            var controller = new GameObject("GameController").AddComponent<GameController>();
            var sounds = new GameObject("SoundEffects", typeof(AudioSource)).AddComponent<SoundEffects>();

            GameHud hud = BuildHud(ui.Safe, star, tilePrefab, controller, out NextPiecePreview nextPreview);

            // Play area: catches touches anywhere around the board.
            GameObject playArea = Child("PlayArea", ui.Safe, typeof(Image));
            Stretch((RectTransform)playArea.transform, 30, 190, 30, 270);
            playArea.GetComponent<Image>().color = Color.clear;

            GameObject board = RoundedPanel("Board", playArea.transform, BoardBackground, 1f);
            var fitter = board.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)levels[0].BoardWidth / levels[0].BoardHeight;

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

            BoosterBar boosterBar = BuildBoosterBar(ui.Safe, controller);
            WordFoundOverlay banner = BuildClearBanner(ui.Safe);
            LevelBriefingPanel briefing = BuildBriefingPanel(ui.Safe, controller);
            GameObject pausePanel = BuildPausePanel(ui.Safe, controller, out TMP_Text musicLabel, out TMP_Text soundLabel, out TMP_Text pauseExit);
            LevelCompletePanel levelComplete = BuildLevelCompletePanel(ui.Safe, star, controller, out TMP_Text completeExit);
            GameOverPanel gameOver = BuildGameOverPanel(ui.Safe, controller, out TMP_Text gameOverExit);
            GlossaryPanel glossary = BuildGlossaryPanel(ui.Safe);
            Toast toast = BuildToast(ui.Safe, top: true);
            ConfirmDialog confirm = BuildConfirmDialog(ui.Safe); // Last, so it shows above the pause menu.

            WireArray(controller, "levels", levels);
            Wire(controller, "endlessRules", endless);
            Wire(controller, "dailyRules", daily);
            Wire(controller, "wordList", AssetDatabase.LoadAssetAtPath<TextAsset>(WordListPath));
            Wire(controller, "boardView", boardView);
            Wire(controller, "boardInput", boardInput);
            Wire(controller, "hud", hud);
            Wire(controller, "nextPiecePreview", nextPreview);
            Wire(controller, "boosterBar", boosterBar);
            Wire(controller, "glossaryPanel", glossary);
            Wire(controller, "toast", toast);
            WireArray(controller, "exitLabels", pauseExit, completeExit, gameOverExit);
            Wire(controller, "clearBanner", banner);
            Wire(controller, "briefingPanel", briefing);
            Wire(controller, "pausePanel", pausePanel);
            Wire(controller, "pauseMusicLabel", musicLabel);
            Wire(controller, "pauseSoundLabel", soundLabel);
            Wire(controller, "confirmDialog", confirm);
            Wire(controller, "levelCompletePanel", levelComplete);
            Wire(controller, "gameOverPanel", gameOver);
            Wire(controller, "sounds", sounds);

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
            rect.sizeDelta = new Vector2(0, 250);

            // Left: score and a counter (moves left, or speed in Endless). Sizes
            // leave room for narrow phones, where the canvas can be under 1000 units wide.
            TMP_Text scoreTitle = Label(root.transform, "ScoreTitle", Strings.Score, 32, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(scoreTitle.gameObject, new Vector2(0, 1), new Vector2(180, -45), new Vector2(280, 44));
            TMP_Text score = Label(root.transform, "ScoreLabel", "0", 60, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(score.gameObject, new Vector2(0, 1), new Vector2(180, -100), new Vector2(280, 70));
            TMP_Text counter = Label(root.transform, "CounterLabel", "", 30, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(counter.gameObject, new Vector2(0, 1), new Vector2(180, -165), new Vector2(280, 44));

            // Center: level or mode, objective progress, stars.
            TMP_Text title = Label(root.transform, "TitleLabel", string.Format(Strings.LevelFormat, 1), 38, TextWhite, FontStyles.Bold);
            Place(title.gameObject, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(300, 50));
            TMP_Text objective = Label(root.transform, "ObjectiveLabel", "Pontos 0/500", 24, TextMuted);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 14;
            objective.fontSizeMax = 24;
            Place(objective.gameObject, new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(330, 40));

            GameObject track = RoundedPanel("ProgressTrack", root.transform, TrackDark, 2.5f);
            Place(track, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(280, 24));
            GameObject fill = RoundedPanel("ProgressFill", track.transform, Green, 2.5f);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            GameObject starsRoot = Child("Stars", root.transform);
            Place(starsRoot, new Vector2(0.5f, 1), new Vector2(0, -168), new Vector2(240, 60));
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = StarImage(starsRoot.transform, $"Star{i + 1}", star, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 72, 0), 56);
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

            const float pauseSize = 110f;
            GameObject pause = ButtonFace("PauseButton", root.transform, Secondary, pauseSize);
            Place(pause, new Vector2(1, 1), new Vector2(-80, -110), new Vector2(pauseSize, pauseSize));
            Button pauseButton = MakeClickable(pause);
            foreach (float x in new[] { -14f, 14f })
            {
                GameObject bar = RoundedPanel("Bar", pause.transform, TextWhite, 6f);
                bar.GetComponent<Image>().raycastTarget = false;
                Place(bar, new Vector2(0.5f, 0.5f), new Vector2(x, ButtonLipHeight(pauseSize) / 2f), new Vector2(16, 50));
            }

            OnClick(pauseButton, controller.Pause);

            var hud = root.AddComponent<GameHud>();
            Wire(hud, "titleLabel", title);
            Wire(hud, "scoreLabel", score);
            Wire(hud, "counterLabel", counter);
            Wire(hud, "objectiveLabel", objective);
            Wire(hud, "progressTrack", track);
            Wire(hud, "progressFill", fillRect);
            Wire(hud, "starsRoot", starsRoot);
            WireArray(hud, "stars", stars);
            return hud;
        }

        private static WordFoundOverlay BuildClearBanner(Transform parent)
        {
            GameObject root = RoundedPanel("ClearBanner", parent, BannerBackground, 1f);
            // Over the top of the board, which is usually empty, so the cleared letters stay visible.
            Place(root, new Vector2(0.5f, 1f), new Vector2(0, -560), new Vector2(980, 500));
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f; // Hidden until something clears.
            group.blocksRaycasts = false;
            VerticalStack(root, padding: 40, spacing: 10);

            TMP_Text title = Label(root.transform, "Title", Strings.WordFound, 60, Yellow, FontStyles.Bold);
            Sized(title, 80);
            TMP_Text words = Label(root.transform, "Words", "SOL", 96, WordGreen, FontStyles.Bold);
            words.enableAutoSizing = true;
            words.fontSizeMin = 36;
            words.fontSizeMax = 96;
            Sized(words, 140);
            TMP_Text points = Label(root.transform, "Points", "+90", 72, Yellow, FontStyles.Bold);
            Sized(points, 100);
            TMP_Text definition = Label(root.transform, "Definition", "", 30, TextWhite, FontStyles.Italic);
            definition.enableAutoSizing = true;
            definition.fontSizeMin = 20;
            definition.fontSizeMax = 30;
            Sized(definition, 80);

            var overlay = root.AddComponent<WordFoundOverlay>();
            Wire(overlay, "definitionLabel", definition);
            Wire(overlay, "group", group);
            Wire(overlay, "titleLabel", title);
            Wire(overlay, "wordsLabel", words);
            Wire(overlay, "pointsLabel", points);
            return overlay;
        }

        private static LevelBriefingPanel BuildBriefingPanel(Transform parent, GameController controller)
        {
            GameObject root = ModalCard("BriefingPanel", parent, new Vector2(880, 1000), out GameObject card);

            TMP_Text title = Label(card.transform, "Title", string.Format(Strings.LevelFormat, 1), 72, TextWhite, FontStyles.Bold);
            Sized(title, 100);
            Sized(Label(card.transform, "ObjectiveHeading", Strings.Objective, 34, Yellow, FontStyles.Bold), 50);
            TMP_Text objective = Label(card.transform, "Objective", string.Format(Strings.ObjectiveScore, "500"), 44, TextWhite, FontStyles.Bold);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 26;
            objective.fontSizeMax = 44;
            Sized(objective, 170);
            TMP_Text tip = Label(card.transform, "Tip", "", 34, TextMuted);
            tip.enableAutoSizing = true;
            tip.fontSizeMin = 22;
            tip.fontSizeMax = 34;
            Sized(tip, 260);
            Button start = PrimaryButton(card.transform, "StartButton", Strings.Start, 150, 60, out _);
            OnClick(start, controller.StartLevel);

            var panel = root.AddComponent<LevelBriefingPanel>();
            Wire(panel, "titleLabel", title);
            Wire(panel, "objectiveLabel", objective);
            Wire(panel, "tipLabel", tip);
            root.SetActive(false);
            return panel;
        }

        private static GameObject BuildPausePanel(Transform parent, GameController controller,
            out TMP_Text musicLabel, out TMP_Text soundLabel, out TMP_Text exitLabel)
        {
            GameObject root = ModalCard("PausePanel", parent, new Vector2(860, 1000), out GameObject card);

            Sized(Label(card.transform, "Title", Strings.Paused, 64, TextWhite, FontStyles.Bold), 100);
            Button resume = PrimaryButton(card.transform, "ResumeButton", Strings.Resume, 140, 56, out _);
            Button restart = SecondaryButton(card.transform, "RestartButton", Strings.RestartLevel, 120, out _);
            Button music = SecondaryButton(card.transform, "MusicToggle", Strings.MusicOn, 120, out musicLabel);
            Button sound = SecondaryButton(card.transform, "SoundToggle", Strings.SoundOn, 120, out soundLabel);
            Button menu = SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 120, out exitLabel);

            OnClick(resume, controller.Resume);
            OnClick(restart, controller.RequestRestart);
            OnClick(music, controller.ToggleMusic);
            OnClick(sound, controller.ToggleSound);
            OnClick(menu, controller.RequestExit);

            root.SetActive(false);
            return root;
        }

        private static ConfirmDialog BuildConfirmDialog(Transform parent)
        {
            GameObject root = ModalCard("ConfirmDialog", parent, new Vector2(820, 640), out GameObject card);

            TMP_Text message = Label(card.transform, "Message", Strings.ConfirmRestart, 40, TextWhite);
            Sized(message, 200);
            Button yes = PrimaryButton(card.transform, "YesButton", Strings.Yes, 130, 52, out _);
            Button no = SecondaryButton(card.transform, "NoButton", Strings.No, 120, out _);

            var dialog = root.AddComponent<ConfirmDialog>();
            Wire(dialog, "messageLabel", message);
            OnClick(yes, dialog.Confirm);
            OnClick(no, dialog.Cancel);
            root.SetActive(false);
            return dialog;
        }

        private static LevelCompletePanel BuildLevelCompletePanel(Transform parent, Sprite star, GameController controller,
            out TMP_Text exitLabel)
        {
            GameObject root = ModalCard("LevelCompletePanel", parent, new Vector2(900, 1620), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 22;

            TMP_Text title = Label(card.transform, "Title", Strings.LevelComplete, 72, TextWhite, FontStyles.Bold);
            Sized(title, 100);
            TMP_Text objective = Label(card.transform, "ObjectiveStatus", Strings.ObjectiveDone, 32, TextMuted);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 20;
            objective.fontSizeMax = 32;
            Sized(objective, 90);

            GameObject starRow = Child("Stars", card.transform);
            Sized(starRow, 200);
            var stars = new[]
            {
                StarImage(starRow.transform, "Star1", star, new Vector2(0.5f, 0.5f), new Vector2(-210, -20), 150),
                StarImage(starRow.transform, "Star2", star, new Vector2(0.5f, 0.5f), new Vector2(0, 10), 190),
                StarImage(starRow.transform, "Star3", star, new Vector2(0.5f, 0.5f), new Vector2(210, -20), 150),
            };

            GameObject stats = RoundedPanel("Stats", card.transform, Inset, 1f);
            Sized(stats, 270);
            VerticalStack(stats, padding: 25, spacing: 8);
            TMP_Text score = StatRow(stats.transform, "ScoreRow", Strings.Score, TextWhite);
            TMP_Text best = StatRow(stats.transform, "BestRow", Strings.Best, Yellow);
            TMP_Text count = StatRow(stats.transform, "WordsRow", Strings.WordsFoundCount, TextWhite);

            TMP_Text wordList = Label(card.transform, "WordList", "SOL · MAR", 34, WordGreen, FontStyles.Bold);
            wordList.enableAutoSizing = true;
            wordList.fontSizeMin = 18;
            wordList.fontSizeMax = 34;
            Sized(wordList, 100);

            Button next = PrimaryButton(card.transform, "NextLevelButton", Strings.NextLevel, 140, 56, out _);
            Button meanings = SecondaryButton(card.transform, "MeaningsButton", Strings.Meanings, 110, out _);
            Button again = SecondaryButton(card.transform, "PlayAgainButton", Strings.PlayAgain, 110, out _);
            Button menu = SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 110, out exitLabel);

            OnClick(next, controller.NextLevel);
            OnClick(meanings, controller.ShowMeanings);
            OnClick(again, controller.Restart);
            OnClick(menu, controller.Exit);

            var panel = root.AddComponent<LevelCompletePanel>();
            Wire(panel, "titleLabel", title);
            WireArray(panel, "stars", stars);
            Wire(panel, "objectiveLabel", objective);
            Wire(panel, "scoreLabel", score);
            Wire(panel, "bestLabel", best);
            Wire(panel, "wordCountLabel", count);
            Wire(panel, "wordListLabel", wordList);
            Wire(panel, "nextLevelButton", next.gameObject);

            root.SetActive(false);
            return panel;
        }

        private static GameOverPanel BuildGameOverPanel(Transform parent, GameController controller, out TMP_Text exitLabel)
        {
            GameObject root = ModalCard("GameOverPanel", parent, new Vector2(860, 1180), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 22;

            Sized(Label(card.transform, "Title", Strings.GameOver, 72, TextWhite, FontStyles.Bold), 100);
            TMP_Text reason = Label(card.transform, "Reason", Strings.BoardFull, 38, TextMuted);
            Sized(reason, 130);

            GameObject stats = RoundedPanel("Stats", card.transform, Inset, 1f);
            Sized(stats, 220);
            VerticalStack(stats, padding: 30, spacing: 10);
            TMP_Text score = StatRow(stats.transform, "ScoreRow", Strings.Score, TextWhite);
            TMP_Text best = StatRow(stats.transform, "BestRow", Strings.Best, Yellow);

            TMP_Text record = Label(card.transform, "NewRecord", Strings.NewRecord, 48, Yellow, FontStyles.Bold);
            Sized(record, 60);
            Button again = PrimaryButton(card.transform, "TryAgainButton", Strings.TryAgain, 140, 56, out _);
            Button meanings = SecondaryButton(card.transform, "MeaningsButton", Strings.Meanings, 110, out _);
            Button menu = SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 110, out exitLabel);

            OnClick(again, controller.Restart);
            OnClick(meanings, controller.ShowMeanings);
            OnClick(menu, controller.Exit);

            var panel = root.AddComponent<GameOverPanel>();
            Wire(panel, "reasonLabel", reason);
            Wire(panel, "scoreLabel", score);
            Wire(panel, "bestLabel", best);
            Wire(panel, "newRecordLabel", record.gameObject);
            root.SetActive(false);
            return panel;
        }

        /// <summary>Bottom bar (from the mockup): Bomb, Shuffle and Star, each with a count badge.</summary>
        private static BoosterBar BuildBoosterBar(Transform parent, GameController controller)
        {
            GameObject root = Child("BoosterBar", parent);
            Place(root, new Vector2(0.5f, 0f), new Vector2(0, 95), new Vector2(620, 150));
            var group = root.AddComponent<CanvasGroup>();

            string[] icons = { BombIconPath, ShuffleIconPath, StarSpritePath };
            var buttons = new Button[3];
            var counts = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject circle = RoundedPanel($"Booster{i}", root.transform, Secondary, 0.75f);
                Place(circle, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 200, 0), new Vector2(128, 128));
                var shadow = circle.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.35f);
                shadow.effectDistance = new Vector2(0, -6);
                buttons[i] = MakeClickable(circle);

                GameObject icon = Child("Icon", circle.transform, typeof(Image));
                Stretch((RectTransform)icon.transform, 26, 26, 26, 26);
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = LoadSprite(icons[i]);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = i == 2 ? Yellow : TextWhite;

                GameObject badge = RoundedPanel("Count", circle.transform, Green, 0.9f);
                Place(badge, new Vector2(1, 0), new Vector2(-10, 12), new Vector2(54, 54));
                badge.GetComponent<Image>().raycastTarget = false;
                counts[i] = Label(badge.transform, "Label", "3", 30, TextWhite, FontStyles.Bold);
                Stretch(counts[i].rectTransform);
            }

            OnClick(buttons[0], controller.UseBomb);
            OnClick(buttons[1], controller.UseShuffle);
            OnClick(buttons[2], controller.UseStar);

            var bar = root.AddComponent<BoosterBar>();
            WireArray(bar, "buttons", buttons);
            WireArray(bar, "countLabels", counts);
            Wire(bar, "group", group);
            return bar;
        }

        /// <summary>The meanings of the run's words (GDD 7.1, item 13).</summary>
        private static GlossaryPanel BuildGlossaryPanel(Transform parent)
        {
            GameObject root = Child("GlossaryPanel", parent, typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = Dim;

            GameObject card = CardPanel("Card", root.transform);
            Stretch((RectTransform)card.transform, 50, 80, 50, 80);
            AddPopIn(root, card);

            TMP_Text title = Label(card.transform, "Title", Strings.Meanings, 56, TextWhite, FontStyles.Bold);
            Place(title.gameObject, new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(800, 90));

            RectTransform list = ScrollList(card.transform, top: 150);
            ((RectTransform)list.parent).offsetMin = new Vector2(50, 200);
            ((RectTransform)list.parent).offsetMax = new Vector2(-50, -150);
            TMP_Text body = Label(list, "Body", "", 34, TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);

            Button close = PrimaryButton(card.transform, "CloseButton", Strings.Close, 120, 50, out _);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.anchoredPosition = new Vector2(0, 100);
            closeRect.sizeDelta = new Vector2(600, 120);

            var panel = root.AddComponent<GlossaryPanel>();
            Wire(panel, "bodyLabel", body);
            Wire(panel, "scrollRect", list.parent.GetComponent<ScrollRect>());
            OnClick(close, panel.Hide);
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

            SoftShadow(root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, alpha: 0.7f);
            card = CardPanel("Card", root.transform);
            Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            VerticalStack(card, padding: 60, spacing: 30);
            AddPopIn(root, card);
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

            // The glossy tile sprite already has its own lip, so no extra drop shadow.
            GameObject go = TileFace("Tile", null, Color.white);
            ((RectTransform)go.transform).sizeDelta = new Vector2(140, 140);

            TMP_Text letter = Label(go.transform, "Letter", "A", 80, TextWhite, FontStyles.Bold);
            TileLetter(letter);

            GameObject icon = Child("Icon", go.transform, typeof(Image));
            Stretch((RectTransform)icon.transform, 18, 24, 18, 12);
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;
            icon.SetActive(false);

            var view = go.AddComponent<TileView>();
            Wire(view, "background", go.GetComponent<Image>());
            Wire(view, "letterLabel", letter);
            Wire(view, "icon", icon.GetComponent<Image>());
            Wire(view, "bombSprite", LoadSprite(BombIconPath));
            Wire(view, "starSprite", LoadSprite(StarSpritePath));

            PrefabUtility.SaveAsPrefabAsset(go, TilePrefabPath);
            Object.DestroyImmediate(go);
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
