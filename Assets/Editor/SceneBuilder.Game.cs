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

        private static readonly Color BoardFrame = Hex(0x34589C);
        private static readonly Color WellColor = Hex(0x14254F);
        private static readonly Color HudCard = Hex(0x0B1736, 0.75f);
        private static readonly Color WordGreen = Hex(0x5BE06A);
        private static readonly Color BannerBackground = Hex(0x1A2F60, 0.96f);

        // Board frame, per side: the lit bezel, then padding inside the well.
        private const float BoardBezel = 12f;
        private const float BoardPadding = 26f;

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
            Stretch((RectTransform)playArea.transform, 30, 200, 30, 250);
            playArea.GetComponent<Image>().color = Color.clear;

            GameObject board = BuildBoardFrame(playArea.transform, levels[0].BoardWidth, levels[0].BoardHeight,
                out GameObject slots, out GameObject tiles);

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

        /// <summary>
        /// The board: a soft shadow, a lit frame and a recessed well, with the
        /// empty-cell slots and the tiles inside. BoardView goes on the returned
        /// object, which keeps the board's aspect ratio.
        /// </summary>
        private static GameObject BuildBoardFrame(Transform parent, int columns, int rows, out GameObject slots, out GameObject tiles)
        {
            GameObject board = Child("Board", parent);
            var fitter = board.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            // Cells are about 70 units on a reference phone; the frame adds the same on every side.
            const float cell = 70f;
            fitter.aspectRatio = (columns * cell + 2 * BoardPadding) / (rows * cell + 2 * BoardPadding);

            GameObject shadow = SoftShadow(board.transform, Vector2.zero, Vector2.zero, Vector2.zero, alpha: 0.6f);
            Stretch((RectTransform)shadow.transform, -35, -53, -35, -17);

            GameObject frame = CardPanel("Frame", board.transform, BoardFrame);
            Stretch((RectTransform)frame.transform);
            frame.GetComponent<Image>().raycastTarget = false;

            GameObject well = Well("Well", board.transform);
            Stretch((RectTransform)well.transform, BoardBezel, BoardBezel, BoardBezel, BoardBezel);

            slots = Child("Slots", board.transform);
            Stretch((RectTransform)slots.transform, BoardPadding, BoardPadding, BoardPadding, BoardPadding);
            tiles = Child("Tiles", board.transform);
            Stretch((RectTransform)tiles.transform, BoardPadding, BoardPadding, BoardPadding, BoardPadding);
            return board;
        }

        /// <summary>
        /// Top bar (GDD 7.3) in three parts, sized to fit phones narrower than the
        /// 1080-unit reference: a score card on the left, level, progress and
        /// stars in the center, and the next piece and pause on the right.
        /// </summary>
        private static GameHud BuildHud(Transform parent, Sprite star, TileView tilePrefab, GameController controller,
            out NextPiecePreview nextPreview)
        {
            GameObject root = Child("Hud", parent);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 230);

            // Left: score, and a counter under it (moves left, or speed in Endless).
            GameObject scoreCard = CardPanel("ScoreCard", root.transform, HudCard);
            Place(scoreCard, new Vector2(0, 1), new Vector2(165, -110), new Vector2(270, 190));
            TMP_Text scoreTitle = Label(scoreCard.transform, "ScoreTitle", Strings.Score, 28, TextMuted);
            Place(scoreTitle.gameObject, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(240, 40));
            TMP_Text score = Label(scoreCard.transform, "ScoreLabel", "0", 64, TextWhite, FontStyles.Bold);
            score.enableAutoSizing = true;
            score.fontSizeMin = 36;
            score.fontSizeMax = 64;
            score.textWrappingMode = TextWrappingModes.NoWrap;
            Place(score.gameObject, new Vector2(0.5f, 1), new Vector2(0, -94), new Vector2(240, 76));
            TMP_Text counter = Label(scoreCard.transform, "CounterLabel", "", 26, Yellow);
            counter.enableAutoSizing = true;
            counter.fontSizeMin = 16;
            counter.fontSizeMax = 26;
            Place(counter.gameObject, new Vector2(0.5f, 1), new Vector2(0, -152), new Vector2(240, 36));

            // Center: level or mode, objective progress, stars.
            TMP_Text title = Label(root.transform, "TitleLabel", string.Format(Strings.LevelFormat, 1), 42, TextWhite, FontStyles.Bold);
            title.enableAutoSizing = true;
            title.fontSizeMin = 26;
            title.fontSizeMax = 42;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Place(title.gameObject, new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(330, 56));
            TMP_Text objective = Label(root.transform, "ObjectiveLabel", "Pontos 0/500", 24, TextMuted);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 14;
            objective.fontSizeMax = 24;
            Place(objective.gameObject, new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(330, 36));

            GameObject track = RoundedPanel("ProgressTrack", root.transform, TrackDark, 2.2f);
            Place(track, new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(300, 30));
            GameObject fill = RoundedPanel("ProgressFill", track.transform, Green, 2.2f);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(3, 3);
            fillRect.offsetMax = new Vector2(-3, -3);
            GameObject gloss = RoundedPanel("Gloss", fill.transform, new Color(1f, 1f, 1f, 0.3f), 4f);
            Stretch((RectTransform)gloss.transform, 6, 12, 6, 3);
            gloss.GetComponent<Image>().raycastTarget = false;

            GameObject starsRoot = Child("Stars", root.transform);
            Place(starsRoot, new Vector2(0.5f, 1), new Vector2(0, -182), new Vector2(240, 60));
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = StarImage(starsRoot.transform, $"Star{i + 1}", star, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 66, 0), 54);
                stars[i].color = new Color(1f, 1f, 1f, 0.18f); // Unearned; GameHud lights them up.
            }

            // Right: the next piece in a small card, then the pause button.
            GameObject nextCard = CardPanel("NextCard", root.transform, HudCard);
            Place(nextCard, new Vector2(1, 1), new Vector2(-230, -110), new Vector2(150, 190));
            TMP_Text nextLabel = Label(nextCard.transform, "NextLabel", Strings.Next, 24, TextMuted);
            Place(nextLabel.gameObject, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(140, 36));
            GameObject nextBox = Well("NextPiece", nextCard.transform);
            Stretch((RectTransform)nextBox.transform, 12, 12, 12, 54);
            GameObject nextTiles = Child("Tiles", nextBox.transform);
            Stretch((RectTransform)nextTiles.transform, 10, 10, 10, 10);
            nextPreview = nextBox.AddComponent<NextPiecePreview>();
            Wire(nextPreview, "tilePrefab", tilePrefab);
            Wire(nextPreview, "tilesRoot", nextTiles.transform);

            const float pauseSize = 110f;
            GameObject pause = ButtonFace("PauseButton", root.transform, Secondary, pauseSize);
            Place(pause, new Vector2(1, 1), new Vector2(-85, -70), new Vector2(pauseSize, pauseSize));
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
            GameObject root = Child("ClearBanner", parent);
            // Over the middle of the board: below the rows where the next piece
            // spawns, and above the bottom rows where most words form.
            const float width = 700f;
            const float height = 380f;
            Place(root, new Vector2(0.5f, 1f), new Vector2(0, -720), new Vector2(width, height));
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f; // Hidden until something clears.
            group.blocksRaycasts = false;

            SoftShadow(root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), alpha: 0.7f);
            GameObject card = CardPanel("Card", root.transform, BannerBackground);
            Stretch((RectTransform)card.transform);
            VerticalStack(card, padding: 26, spacing: 4);

            TMP_Text title = Label(card.transform, "Title", Strings.WordFound, 46, Yellow, FontStyles.Bold);
            Sized(title, 60);
            TMP_Text words = Label(card.transform, "Words", "SOL", 88, WordGreen, FontStyles.Bold);
            words.enableAutoSizing = true;
            words.fontSizeMin = 36;
            words.fontSizeMax = 88;
            Sized(words, 110);
            TMP_Text points = Label(card.transform, "Points", "+90", 60, Yellow, FontStyles.Bold);
            Sized(points, 72);
            TMP_Text definition = Label(card.transform, "Definition", "", 30, TextWhite, FontStyles.Italic);
            definition.enableAutoSizing = true;
            definition.fontSizeMin = 20;
            definition.fontSizeMax = 30;
            Sized(definition, 76);

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

        /// <summary>Bottom bar (from the mockup): Bomb, Shuffle and Star as 3D buttons, each with a count badge.</summary>
        private static BoosterBar BuildBoosterBar(Transform parent, GameController controller)
        {
            GameObject root = Child("BoosterBar", parent);
            Place(root, new Vector2(0.5f, 0f), new Vector2(0, 98), new Vector2(620, 170));
            var group = root.AddComponent<CanvasGroup>();

            const float size = 140f;
            float lip = ButtonLipHeight(size);
            string[] icons = { BombIconPath, ShuffleIconPath, StarSpritePath };
            var buttons = new Button[3];
            var counts = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject face = ButtonFace($"Booster{i}", root.transform, Secondary, size);
                Place(face, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 190, 0), new Vector2(150, size));
                buttons[i] = MakeClickable(face);

                GameObject icon = Child("Icon", face.transform, typeof(Image));
                Stretch((RectTransform)icon.transform, 32, 24 + lip, 32, 24);
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = LoadSprite(icons[i]);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = i == 2 ? Yellow : TextWhite;

                // Count badge on the top-right corner, a small glossy tile.
                GameObject badge = TileFace("Count", face.transform, Green);
                Place(badge, new Vector2(1, 1), new Vector2(-4, -2), new Vector2(60, 60));
                counts[i] = Label(badge.transform, "Label", "3", 32, TextWhite, FontStyles.Bold);
                TileLetter(counts[i]);
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

        /// <summary>A recessed area, darker under its top edge (the board, the next-piece box).</summary>
        private static GameObject Well(string name, Transform parent)
        {
            GameObject go = Child(name, parent, typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = well;
            image.type = Image.Type.Sliced;
            image.color = WellColor;
            image.raycastTarget = false;
            return go;
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
