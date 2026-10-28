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
        private static readonly Color Divider = Hex(0x22386A);
        private static readonly Color RecordBadge = Hex(0xFFC83A);
        private static readonly Color RecordText = Hex(0x3A2400);

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

            var shaker = playArea.AddComponent<Shaker>();

            GameObject board = BuildBoardFrame(playArea.transform, levels[0].BoardWidth, levels[0].BoardHeight,
                out GameObject slots, out GameObject tiles);
            BuildBoardEffects(board.transform, out UiParticles particles, out FloatingText floatingText);

            var boardView = board.AddComponent<BoardView>();
            Wire(boardView, "tilePrefab", tilePrefab);
            Wire(boardView, "slotsRoot", slots.transform);
            Wire(boardView, "tilesRoot", tiles.transform);
            Wire(boardView, "slotSprite", rounded);
            Wire(boardView, "particles", particles);
            Wire(boardView, "floatingText", floatingText);

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
            Wire(controller, "boardShaker", shaker);
            Wire(levelComplete, "sounds", sounds);

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
        /// Layers over the board's tiles, laid out like them so they share
        /// coordinates: tile-burst particles, then floating points on top.
        /// </summary>
        private static void BuildBoardEffects(Transform board, out UiParticles particles, out FloatingText floatingText)
        {
            GameObject particleLayer = Child("Particles", board);
            Stretch((RectTransform)particleLayer.transform, BoardPadding, BoardPadding, BoardPadding, BoardPadding);
            particles = particleLayer.AddComponent<UiParticles>();
            Wire(particles, "chunkSprite", tileSprite);
            Wire(particles, "sparkSprite", glow);

            GameObject textLayer = Child("FloatingText", board);
            Stretch((RectTransform)textLayer.transform, BoardPadding, BoardPadding, BoardPadding, BoardPadding);
            TMP_Text template = Label(textLayer.transform, "Template", "+90", 64, Yellow, FontStyles.Bold);
            template.textWrappingMode = TextWrappingModes.NoWrap;
            Place(template.gameObject, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 100));
            template.gameObject.SetActive(false);
            floatingText = textLayer.AddComponent<FloatingText>();
            Wire(floatingText, "template", template);
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
            GameObject root = ModalCard("BriefingPanel", parent, new Vector2(880, 900), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 26;

            TMP_Text title = Label(card.transform, "Title", string.Format(Strings.LevelFormat, 1), 72, TextWhite, FontStyles.Bold);
            Sized(title, 100);

            GameObject goal = CardPanel("ObjectiveCard", card.transform, Inset);
            Sized(goal, 230);
            TMP_Text heading = Label(goal.transform, "ObjectiveHeading", Strings.Objective, 32, Yellow, FontStyles.Bold);
            Place(heading.gameObject, new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(700, 50));
            TMP_Text objective = Label(goal.transform, "Objective", string.Format(Strings.ObjectiveScore, "500"), 44, TextWhite, FontStyles.Bold);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 24;
            objective.fontSizeMax = 44;
            Stretch(objective.rectTransform, 30, 20, 30, 80);

            TMP_Text tip = Label(card.transform, "Tip", "", 32, TextMuted, FontStyles.Italic);
            tip.enableAutoSizing = true;
            tip.fontSizeMin = 22;
            tip.fontSizeMax = 32;
            Sized(tip, 220);
            Button start = WithIcon(PrimaryButton(card.transform, "StartButton", Strings.Start, 150, 60, out _), PlayIconPath, 150);
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
            Button resume = WithIcon(PrimaryButton(card.transform, "ResumeButton", Strings.Resume, 140, 56, out _), PlayIconPath, 140);
            Button restart = WithIcon(SecondaryButton(card.transform, "RestartButton", Strings.RestartLevel, 120, out _), RestartIconPath, 120);
            Button music = WithIcon(SecondaryButton(card.transform, "MusicToggle", Strings.MusicOn, 120, out musicLabel), MusicIconPath, 120);
            Button sound = WithIcon(SecondaryButton(card.transform, "SoundToggle", Strings.SoundOn, 120, out soundLabel), SoundIconPath, 120);
            Button menu = WithIcon(SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 120, out exitLabel), HomeIconPath, 120);

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
            GameObject root = ModalCard("LevelCompletePanel", parent, new Vector2(900, 1500), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 20;

            // Stars on a slowly turning sunburst; confetti bursts from each star as it lands.
            GameObject starRow = Child("Stars", card.transform);
            Sized(starRow, 230);
            GameObject burst = Child("Rays", starRow.transform, typeof(Image));
            Place(burst, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(620, 620));
            var burstImage = burst.GetComponent<Image>();
            burstImage.sprite = rays;
            burstImage.color = new Color(1f, 0.85f, 0.4f, 0.22f);
            burstImage.raycastTarget = false;
            burst.AddComponent<Spin>();
            GameObject halo = Child("Glow", starRow.transform, typeof(Image));
            Place(halo, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(520, 360));
            var haloImage = halo.GetComponent<Image>();
            haloImage.sprite = glow;
            haloImage.color = new Color(1f, 0.75f, 0.25f, 0.35f);
            haloImage.raycastTarget = false;
            var stars = new[]
            {
                StarImage(starRow.transform, "Star1", star, new Vector2(0.5f, 0.5f), new Vector2(-210, -20), 150),
                StarImage(starRow.transform, "Star2", star, new Vector2(0.5f, 0.5f), new Vector2(0, 10), 190),
                StarImage(starRow.transform, "Star3", star, new Vector2(0.5f, 0.5f), new Vector2(210, -20), 150),
            };
            GameObject confettiLayer = Child("Confetti", starRow.transform);
            Stretch((RectTransform)confettiLayer.transform);
            var confetti = confettiLayer.AddComponent<UiParticles>();
            Wire(confetti, "chunkSprite", tileSprite);
            Wire(confetti, "sparkSprite", glow);

            TMP_Text title = Label(card.transform, "Title", Strings.LevelComplete, 72, TextWhite, FontStyles.Bold);
            Sized(title, 96);
            TMP_Text objective = Label(card.transform, "ObjectiveStatus", Strings.ObjectiveDone, 32, TextMuted);
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 20;
            objective.fontSizeMax = 32;
            Sized(objective, 70);

            ScoreColumns(card.transform, out TMP_Text score, out TMP_Text best, out TMP_Text count, withWords: true);

            TMP_Text wordList = Label(card.transform, "WordList", "SOL · MAR", 34, WordGreen, FontStyles.Bold);
            wordList.enableAutoSizing = true;
            wordList.fontSizeMin = 18;
            wordList.fontSizeMax = 34;
            Sized(wordList, 80);

            Button next = WithIcon(PrimaryButton(card.transform, "NextLevelButton", Strings.NextLevel, 140, 56, out _), PlayIconPath, 140);
            Button meanings = WithIcon(SecondaryButton(card.transform, "MeaningsButton", Strings.Meanings, 110, out _), BookIconPath, 110);
            Button again = WithIcon(SecondaryButton(card.transform, "PlayAgainButton", Strings.PlayAgain, 110, out _), RestartIconPath, 110);
            Button menu = WithIcon(SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 110, out exitLabel), HomeIconPath, 110);

            OnClick(next, controller.NextLevel);
            OnClick(meanings, controller.ShowMeanings);
            OnClick(again, controller.Restart);
            OnClick(menu, controller.Exit);

            var panel = root.AddComponent<LevelCompletePanel>();
            Wire(panel, "titleLabel", title);
            WireArray(panel, "stars", stars);
            Wire(panel, "confetti", confetti);
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
            GameObject root = ModalCard("GameOverPanel", parent, new Vector2(860, 1060), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 22;

            Sized(Label(card.transform, "Title", Strings.GameOver, 72, TextWhite, FontStyles.Bold), 100);
            TMP_Text reason = Label(card.transform, "Reason", Strings.BoardFull, 36, TextMuted);
            Sized(reason, 100);

            ScoreColumns(card.transform, out TMP_Text score, out TMP_Text best, out _, withWords: false);

            // "Novo recorde!" on a gold badge, centered in its row.
            GameObject recordRow = Child("NewRecord", card.transform);
            Sized(recordRow, 76);
            GameObject badge = ButtonFace("Badge", recordRow.transform, RecordBadge, 76);
            Place(badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 76));
            badge.GetComponent<Image>().raycastTarget = false;
            TMP_Text record = Label(badge.transform, "Label", Strings.NewRecord, 40, RecordText, FontStyles.Bold);
            record.fontSharedMaterial = boldFont.material; // No soft shadow on the light badge.
            Stretch(record.rectTransform, 0, ButtonLipHeight(76), 0, 0);

            Button again = WithIcon(PrimaryButton(card.transform, "TryAgainButton", Strings.TryAgain, 140, 56, out _), RestartIconPath, 140);
            Button meanings = WithIcon(SecondaryButton(card.transform, "MeaningsButton", Strings.Meanings, 110, out _), BookIconPath, 110);
            Button menu = WithIcon(SecondaryButton(card.transform, "ExitButton", Strings.MainMenu, 110, out exitLabel), HomeIconPath, 110);

            OnClick(again, controller.Restart);
            OnClick(meanings, controller.ShowMeanings);
            OnClick(menu, controller.Exit);

            var panel = root.AddComponent<GameOverPanel>();
            Wire(panel, "reasonLabel", reason);
            Wire(panel, "scoreLabel", score);
            Wire(panel, "bestLabel", best);
            Wire(panel, "newRecordLabel", recordRow);
            root.SetActive(false);
            return panel;
        }

        /// <summary>
        /// Result stats (from the mockup): score and best side by side, and
        /// optionally the number of words found on a row under them.
        /// </summary>
        private static void ScoreColumns(Transform parent, out TMP_Text score, out TMP_Text best, out TMP_Text words, bool withWords)
        {
            GameObject stats = CardPanel("Stats", parent, Inset);
            Sized(stats, withWords ? 290 : 190);

            score = ScoreColumn(stats.transform, "Score", Strings.Score, TextWhite, 0.25f);
            best = ScoreColumn(stats.transform, "Best", Strings.Best, Yellow, 0.75f);
            GameObject split = RoundedPanel("Split", stats.transform, Divider, 4f);
            Place(split, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(4, 130));

            words = null;
            if (!withWords)
            {
                return;
            }

            GameObject line = RoundedPanel("Line", stats.transform, Divider, 4f);
            var lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0, 1);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.offsetMin = new Vector2(30, -184);
            lineRect.offsetMax = new Vector2(-30, -180);

            Image icon = StarImage(stats.transform, "WordsIcon", LoadSprite(StarSpritePath), new Vector2(0, 0), new Vector2(62, 54), 46);
            icon.color = Yellow;
            TMP_Text title = Label(stats.transform, "WordsTitle", Strings.WordsFoundCount, 34, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(title.gameObject, new Vector2(0, 0), new Vector2(355, 54), new Vector2(500, 60));
            words = Label(stats.transform, "WordsValue", "0", 46, TextWhite, FontStyles.Bold, TextAlignmentOptions.Right);
            Place(words.gameObject, new Vector2(1, 0), new Vector2(-130, 54), new Vector2(200, 60));
        }

        private static TMP_Text ScoreColumn(Transform parent, string name, string caption, Color valueColor, float x)
        {
            TMP_Text title = Label(parent, name + "Title", caption, 32, TextMuted);
            Place(title.gameObject, new Vector2(x, 1), new Vector2(0, -50), new Vector2(330, 44));
            TMP_Text value = Label(parent, name + "Value", "0", 64, valueColor, FontStyles.Bold);
            value.enableAutoSizing = true;
            value.fontSizeMin = 36;
            value.fontSizeMax = 64;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            Place(value.gameObject, new Vector2(x, 1), new Vector2(0, -115), new Vector2(330, 80));
            return value;
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
