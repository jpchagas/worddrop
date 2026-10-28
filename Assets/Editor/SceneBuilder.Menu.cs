using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Gameplay;
using WordDrop.Progress;
using WordDrop.Screens;
using WordDrop.UI;

namespace WordDrop.EditorTools
{
    /// <summary>The MainMenu scene (with How to Play, Daily Challenge, achievements and records) and the LevelMap scene.</summary>
    public static partial class SceneBuilder
    {
        private static readonly Color ToastBackground = Hex(0x1A2C55);
        private static readonly Color DailyColor = Hex(0xF39A1E);
        private static readonly Color HowToColor = Hex(0x3D8BEA);
        private static readonly Color AchievementsColor = Hex(0x9B59F0);
        private static readonly Color RecordsColor = Hex(0xEF5468);
        private static readonly Color PathBed = Hex(0x0F1C40);

        // ---------------------------------------------------------------- Main menu

        private static void BuildMainMenu()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("MainMenuScreen").AddComponent<MainMenuScreen>();

            var music = new GameObject("MusicPlayer", typeof(AudioSource));
            music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectSetup.MusicPath);
            music.GetComponent<AudioSource>().playOnAwake = false;
            music.AddComponent<MusicPlayer>();

            Foliage(ui.Safe);
            GameObject decor = Child("FloatingTiles", ui.Safe);
            Stretch((RectTransform)decor.transform);
            // Dimmer tiles low among the leaves first, then the bright ones around the logo.
            FloatingTile(decor.transform, "E", 4, new Vector2(0, 0), new Vector2(150, 330), 100, -14, 0.6f);
            FloatingTile(decor.transform, "O", 2, new Vector2(1, 0), new Vector2(-150, 380), 90, 12, 0.6f);
            FloatingTile(decor.transform, "W", 1, new Vector2(0, 1), new Vector2(130, -150), 150, -12, 1f);
            FloatingTile(decor.transform, "R", 2, new Vector2(1, 1), new Vector2(-125, -235), 140, 14, 1f);
            FloatingTile(decor.transform, "A", 3, new Vector2(0, 1), new Vector2(100, -560), 120, 10, 0.9f);
            FloatingTile(decor.transform, "D", 5, new Vector2(1, 1), new Vector2(-105, -540), 110, -10, 0.9f);

            Logo(ui.Safe, new Vector2(0, -400), 700);

            const float settingsSize = 110f;
            GameObject settingsFace = ButtonFace("SettingsButton", ui.Safe, Secondary, settingsSize);
            Place(settingsFace, new Vector2(1, 1), new Vector2(-90, -95), new Vector2(settingsSize, settingsSize));
            Button settings = MakeClickable(settingsFace);
            ButtonIcon(settingsFace, GearIconPath, settingsSize, 64, 0);

            GameObject column = Child("Buttons", ui.Safe);
            Place(column, new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(820, 760));

            Button play = PrimaryButton(column.transform, "PlayButton", Strings.Play, 170, 62, out TMP_Text playLabel);
            Place(play.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(820, 170));
            ButtonIcon(play.gameObject, PlayIconPath, 170, 64, 90);
            Button endless = SecondaryButton(column.transform, "EndlessButton", Strings.Endless, 130, out _);
            Place(endless.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(820, 130));
            ButtonIcon(endless.gameObject, InfinityIconPath, 130, 64, 85);
            TMP_Text endlessBest = Label(column.transform, "EndlessBest", "", 30, TextMuted);
            Place(endlessBest.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(820, 40));

            Button daily = MenuTile(column.transform, "DailyChallengeButton", Strings.DailyChallenge, CalendarIconPath, DailyColor, new Vector2(-306, -220));
            Button howTo = MenuTile(column.transform, "HowToPlayButton", Strings.HowToPlay, QuestionIconPath, HowToColor, new Vector2(-102, -220));
            Button achievements = MenuTile(column.transform, "AchievementsButton", Strings.Achievements, TrophyIconPath, AchievementsColor, new Vector2(102, -220));
            Button records = MenuTile(column.transform, "LeaderboardsButton", Strings.Leaderboards, PodiumIconPath, RecordsColor, new Vector2(306, -220));

            TMP_Text credits = Label(ui.Safe, "Credits", Strings.Credits, 26, TextMuted);
            Place(credits.gameObject, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(1000, 50));

            GameObject settingsPanel = BuildSettingsPanel(ui.Safe, screen, out Button terms, out Button privacy);
            GameObject howToPlay = BuildHowToPlayPanel(ui.Safe, screen);
            DailyChallengePanel dailyPanel = BuildDailyPanel(ui.Safe);
            AchievementsPanel achievementsPanel = BuildAchievementsPanel(ui.Safe);
            LeaderboardPanel leaderboardPanel = BuildLeaderboardPanel(ui.Safe);
            Toast toast = BuildToast(ui.Safe);
            Wire(dailyPanel, "toast", toast);
            LegalTextPanel legal = LegalPanel(ui.Safe);

            Wire(screen, "playLabel", playLabel);
            Wire(screen, "endlessBestLabel", endlessBest);
            Wire(screen, "settingsPanel", settingsPanel);
            Wire(screen, "howToPlayPanel", howToPlay);

            OnClick(play, screen.Play);
            OnClick(endless, screen.PlayEndless);
            OnClick(daily, dailyPanel.Open);
            OnClick(howTo, screen.OpenHowToPlay);
            OnClick(achievements, achievementsPanel.Open);
            OnClick(records, leaderboardPanel.Open);
            OnClick(settings, screen.OpenSettings);
            OnClick(terms, legal.ShowTermsOfUse);
            OnClick(privacy, legal.ShowPrivacyPolicy);

            Save(ui.Scene, SceneNames.MainMenu);
        }

        private static GameObject BuildSettingsPanel(Transform parent, MainMenuScreen screen, out Button terms, out Button privacy)
        {
            GameObject root = ModalCard("SettingsPanel", parent, new Vector2(860, 1080), out GameObject card);
            ((VerticalLayoutGroup)card.GetComponent<LayoutGroup>()).spacing = 24;

            Sized(Label(card.transform, "Title", Strings.Settings, 64, TextWhite, FontStyles.Bold), 100);
            Button music = WithIcon(SecondaryButton(card.transform, "MusicToggle", Strings.MusicOn, 110, out TMP_Text musicLabel), MusicIconPath, 110);
            Button sound = WithIcon(SecondaryButton(card.transform, "SoundToggle", Strings.SoundOn, 110, out TMP_Text soundLabel), SoundIconPath, 110);
            Button vibration = WithIcon(SecondaryButton(card.transform, "VibrationToggle", Strings.VibrationOn, 110, out TMP_Text vibrationLabel), VibrationIconPath, 110);
            terms = WithIcon(SecondaryButton(card.transform, "TermsButton", Strings.TermsOfUse, 110, out _), BookIconPath, 110);
            privacy = WithIcon(SecondaryButton(card.transform, "PrivacyButton", Strings.PrivacyPolicy, 110, out _), BookIconPath, 110);
            Button close = PrimaryButton(card.transform, "CloseButton", Strings.Close, 130, 52, out _);

            Wire(screen, "musicToggleLabel", musicLabel);
            Wire(screen, "soundToggleLabel", soundLabel);
            Wire(screen, "vibrationToggleLabel", vibrationLabel);
            OnClick(music, screen.ToggleMusic);
            OnClick(sound, screen.ToggleSound);
            OnClick(vibration, screen.ToggleVibration);
            OnClick(close, screen.CloseSettings);

            root.SetActive(false);
            return root;
        }

        /// <summary>How to Play (GDD 7.1, item 9), following the mockup: numbered steps with small tile pictures.</summary>
        private static GameObject BuildHowToPlayPanel(Transform parent, MainMenuScreen screen)
        {
            GameObject root = ScreenPanel("HowToPlayPanel", parent, Strings.HowToPlay, out RectTransform content, out Button back);
            VerticalStack(content.gameObject, padding: 0, spacing: 24);

            StepCard(content, 1, Strings.HowToPlayStep1Title, Strings.HowToPlayStep1, card =>
                MiniTiles(card, "TRA", new[] { 1, 2, 3 }, vertical: true));
            StepCard(content, 2, Strings.HowToPlayStep2Title, Strings.HowToPlayStep2, card =>
                MiniTiles(card, "MÃE", new[] { 4, 4, 4 }, vertical: false));
            StepCard(content, 3, Strings.HowToPlayStep3Title, Strings.HowToPlayStep3, card =>
                MiniTiles(card, "SOL", new[] { 2, 2, 2 }, vertical: false, highlight: true));
            StepCard(content, 4, Strings.HowToPlayStep4Title, Strings.HowToPlayStep4, card =>
                MiniTiles(card, "CA SA", new[] { 5, 5, 0, 5, 5 }, vertical: false));

            Button gotIt = PrimaryButton(content, "GotItButton", Strings.GotIt, 140, 56, out _);
            OnClick(gotIt, screen.CloseHowToPlay);
            OnClick(back, screen.CloseHowToPlay);
            root.SetActive(false);
            return root;
        }

        private static void StepCard(Transform parent, int number, string title, string text, System.Action<Transform> picture)
        {
            GameObject card = CardPanel($"Step{number}", parent);
            Sized(card, 290);

            // A round green badge, like the level map's nodes.
            GameObject badge = Child("Badge", card.transform, typeof(Image));
            Place(badge, new Vector2(0, 1), new Vector2(70, -70), new Vector2(84, 84));
            var badgeImage = badge.GetComponent<Image>();
            badgeImage.sprite = node;
            badgeImage.color = Green;
            badgeImage.raycastTarget = false;
            Stretch(Label(badge.transform, "Number", number.ToString(), 44, TextWhite, FontStyles.Bold).rectTransform, 0, 84f * 16f / 192f, 0, 0);

            TMP_Text titleLabel = Label(card.transform, "Title", title, 40, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(titleLabel.gameObject, new Vector2(0, 1), new Vector2(415, -70), new Vector2(550, 60));
            TMP_Text body = Label(card.transform, "Text", text, 29, TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 20;
            body.fontSizeMax = 29;
            Place(body.gameObject, new Vector2(0, 1), new Vector2(415, -185), new Vector2(550, 160));

            GameObject art = Child("Picture", card.transform);
            Place(art, new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(240, 240));
            picture(art.transform);
        }

        /// <summary>A few rows of colorful tiles settled at the bottom of a mini board, as an illustration.</summary>
        private static void MiniBoard(Transform board)
        {
            string[] rows = { "PALAVRAS", "S.O.L.MA", "..R..E.." };
            const float size = 46f;
            for (int row = 0; row < rows.Length; row++)
            {
                for (int col = 0; col < rows[row].Length; col++)
                {
                    char letter = rows[row][col];
                    if (letter == '.')
                    {
                        continue;
                    }

                    GameObject tile = TileFace("Tile", board, Gameplay.View.TileView.ColorForHint((row * 3 + col) % 6));
                    Place(tile, new Vector2(0.5f, 0), new Vector2((col - 3.5f) * (size + 2f), 22f + size / 2f + row * (size + 2f)), new Vector2(size, size));
                    TileLetter(Label(tile.transform, "Letter", letter.ToString(), size * 0.58f, TextWhite, FontStyles.Bold));
                }
            }
        }

        /// <summary>A few small tiles for illustrations; a space leaves a gap, hint 0 is the neutral color.</summary>
        private static void MiniTiles(Transform parent, string letters, int[] hints, bool vertical, bool highlight = false)
        {
            float size = Mathf.Min(64f, 230f / letters.Length - 6f);
            for (int i = 0; i < letters.Length; i++)
            {
                if (letters[i] == ' ')
                {
                    continue;
                }

                float offset = (i - (letters.Length - 1) / 2f) * (size + 6f);
                Color color = highlight ? WordGreen : Gameplay.View.TileView.ColorForHint(hints[i]);
                GameObject tile = TileFace("Tile", parent, color);
                Place(tile, new Vector2(0.5f, 0.5f), vertical ? new Vector2(0, -offset) : new Vector2(offset, 0), new Vector2(size, size));
                TileLetter(Label(tile.transform, "Letter", letters[i].ToString(), size * 0.6f, TextWhite, FontStyles.Bold));
            }
        }

        /// <summary>Daily Challenge screen (GDD 4.3), following the mockup: date, result, play, claim reward.</summary>
        private static DailyChallengePanel BuildDailyPanel(Transform parent)
        {
            GameObject root = ScreenPanel("DailyPanel", parent, Strings.DailyChallenge, out RectTransform content, out Button back);
            VerticalStack(content.gameObject, padding: 20, spacing: 28);

            // Date next to an orange calendar tile.
            GameObject dateRow = Child("DateRow", content);
            Sized(dateRow, 110);
            GameObject calendar = ButtonFace("Calendar", dateRow.transform, DailyColor, 100);
            Place(calendar, new Vector2(0.5f, 0.5f), new Vector2(-330, 0), new Vector2(100, 100));
            calendar.GetComponent<Image>().raycastTarget = false;
            ButtonIcon(calendar, CalendarIconPath, 100, 58, 0);
            TMP_Text date = Label(dateRow.transform, "Date", "22 de outubro de 2026", 50, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            date.enableAutoSizing = true;
            date.fontSizeMin = 30;
            date.fontSizeMax = 50;
            date.textWrappingMode = TextWrappingModes.NoWrap;
            Place(date.gameObject, new Vector2(0.5f, 0.5f), new Vector2(70, 0), new Vector2(640, 90));
            Sized(Label(content, "Goal", Strings.DailyGoal, 34, TextMuted), 56);

            GameObject card = CardPanel("ResultCard", content);
            Sized(card, 520);
            // A little board with today's letters; a check badge covers it once the challenge is completed.
            GameObject board = Well("MiniBoard", card.transform);
            Place(board, new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(420, 290));
            MiniBoard(board.transform);
            GameObject doneGroup = Child("Done", card.transform);
            Stretch((RectTransform)doneGroup.transform);
            GameObject dim = Child("Dim", doneGroup.transform, typeof(Image));
            Place(dim, new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(420, 290));
            dim.GetComponent<Image>().sprite = well;
            dim.GetComponent<Image>().type = Image.Type.Sliced;
            dim.GetComponent<Image>().color = Hex(0x0A1430, 0.65f);
            GameObject badge = Child("Badge", doneGroup.transform, typeof(Image));
            Place(badge, new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(170, 170));
            badge.GetComponent<Image>().sprite = node;
            badge.GetComponent<Image>().color = Green;
            GameObject check = Child("Check", badge.transform, typeof(Image));
            Stretch((RectTransform)check.transform, 36, 40, 36, 30);
            check.GetComponent<Image>().sprite = LoadSprite(CheckIconPath);
            TMP_Text done = Label(doneGroup.transform, "Label", Strings.DailyDone, 40, TextWhite, FontStyles.Bold);
            Place(done.gameObject, new Vector2(0.5f, 0), new Vector2(0, 135), new Vector2(800, 60));
            TMP_Text status = Label(card.transform, "Status", Strings.DailyNotPlayed, 36, TextWhite);
            status.enableAutoSizing = true;
            status.fontSizeMin = 22;
            status.fontSizeMax = 36;
            Place(status.gameObject, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(820, 70));

            Button play = WithIcon(PrimaryButton(content, "PlayButton", Strings.Play, 150, 60, out _), PlayIconPath, 150);
            Button claim = WithIcon(SecondaryButton(content, "ClaimButton", Strings.ClaimReward, 130, out TMP_Text claimLabel), GiftIconPath, 130);

            var panel = root.AddComponent<DailyChallengePanel>();
            Wire(panel, "dailyRules", AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelCatalog.DailyPath));
            Wire(panel, "dateLabel", date);
            Wire(panel, "statusLabel", status);
            Wire(panel, "doneBadge", doneGroup);
            Wire(panel, "claimButton", claim);
            Wire(panel, "claimLabel", claimLabel);
            OnClick(play, panel.Play);
            OnClick(claim, panel.Claim);
            OnClick(back, panel.Close);
            root.SetActive(false);
            return panel;
        }

        private static AchievementsPanel BuildAchievementsPanel(Transform parent)
        {
            GameObject root = ScreenPanel("AchievementsPanel", parent, Strings.Achievements, out RectTransform content, out Button back);

            TMP_Text count = Label(content, "Count", "0 de 15 conquistas", 34, Yellow, FontStyles.Bold);
            var countRect = count.rectTransform;
            countRect.anchorMin = new Vector2(0, 1);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.pivot = new Vector2(0.5f, 1);
            countRect.anchoredPosition = Vector2.zero;
            countRect.sizeDelta = new Vector2(0, 60);

            RectTransform list = ScrollList(content, top: 80);

            GameObject statsCard = RoundedPanel("Stats", list, Inset, 1f);
            Sized(statsCard, 400);
            TMP_Text statsTitle = Label(statsCard.transform, "Title", Strings.Statistics, 40, TextWhite, FontStyles.Bold);
            Place(statsTitle.gameObject, new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(800, 60));
            TMP_Text stats = Label(statsCard.transform, "Body", "", 32, TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Stretch(stats.rectTransform, 40, 20, 40, 90);

            var rows = new AchievementRow[Achievements.All.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = AchievementRowView(list, i);
            }

            var panel = root.AddComponent<AchievementsPanel>();
            WireArray(panel, "rows", rows);
            Wire(panel, "countLabel", count);
            Wire(panel, "statsLabel", stats);
            OnClick(back, panel.Close);
            root.SetActive(false);
            return panel;
        }

        private static AchievementRow AchievementRowView(Transform parent, int index)
        {
            GameObject go = CardPanel($"Achievement{index + 1}", parent);
            Sized(go, 180);

            TMP_Text title = Label(go.transform, "Title", "Conquista", 38, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(title.gameObject, new Vector2(0, 1), new Vector2(330, -45), new Vector2(600, 56));
            TMP_Text reward = Label(go.transform, "Reward", "+1", 28, Yellow, FontStyles.Bold, TextAlignmentOptions.Right);
            Place(reward.gameObject, new Vector2(1, 1), new Vector2(-150, -45), new Vector2(260, 50));
            TMP_Text description = Label(go.transform, "Description", "", 28, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Place(description.gameObject, new Vector2(0, 1), new Vector2(430, -95), new Vector2(800, 44));

            GameObject track = RoundedPanel("ProgressTrack", go.transform, TrackDark, 2.5f);
            Place(track, new Vector2(0, 0), new Vector2(330, 38), new Vector2(600, 24));
            GameObject fill = RoundedPanel("ProgressFill", track.transform, Green, 2.5f);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            TMP_Text progress = Label(go.transform, "Progress", "0/1", 28, TextWhite, FontStyles.Normal, TextAlignmentOptions.Right);
            Place(progress.gameObject, new Vector2(1, 0), new Vector2(-150, 38), new Vector2(260, 44));

            var row = go.AddComponent<AchievementRow>();
            row.background = go.GetComponent<Image>();
            row.titleLabel = title;
            row.descriptionLabel = description;
            row.progressFill = fillRect;
            row.progressLabel = progress;
            row.rewardLabel = reward;
            return row;
        }

        private static LeaderboardPanel BuildLeaderboardPanel(Transform parent)
        {
            GameObject root = ScreenPanel("LeaderboardPanel", parent, Strings.Leaderboards, out RectTransform content, out Button back);
            RectTransform list = ScrollList(content, top: 0);
            TMP_Text body = Label(list, "Body", "", 36, TextWhite, FontStyles.Normal, TextAlignmentOptions.TopLeft);

            var panel = root.AddComponent<LeaderboardPanel>();
            Wire(panel, "bodyLabel", body);
            OnClick(back, panel.Close);
            root.SetActive(false);
            return panel;
        }

        // ---------------------------------------------------------------- Level map

        /// <summary>
        /// World 1 as a winding road of level nodes, level 1 at the bottom (GDD 7.1,
        /// item 7). LevelMapScreen colors the nodes and path, and puts the glow
        /// and the pin on the current level.
        /// </summary>
        private static void BuildLevelMap()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("LevelMapScreen").AddComponent<LevelMapScreen>();
            Sprite star = LoadSprite(StarSpritePath);
            Foliage(ui.Safe);

            Header(ui.Safe, Strings.LevelMap, out Button back);
            GameObject worldBadge = CardPanel("WorldBadge", ui.Safe, Inset);
            Place(worldBadge, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(640, 64));
            TMP_Text world = Label(worldBadge.transform, "World", Strings.World1, 32, Yellow, FontStyles.Bold);
            world.enableAutoSizing = true;
            world.fontSizeMin = 20;
            world.fontSizeMax = 32;
            world.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(world.rectTransform, 24, 0, 24, 0);

            GameObject scrollObject = Child("Scroll", ui.Safe, typeof(Image), typeof(RectMask2D));
            Stretch((RectTransform)scrollObject.transform, 0, 0, 0, 215);
            scrollObject.GetComponent<Image>().color = Color.clear;
            GameObject contentObject = Child("Content", scrollObject.transform);
            var content = (RectTransform)contentObject.transform;

            const int count = GameSession.CampaignLevelCount;
            const float spacing = 300f;
            const float bottomMargin = 270f;
            const float nodeSize = 180f;
            float height = bottomMargin * 2 + spacing * (count - 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, height);

            var scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)scrollObject.transform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;

            var positions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector2(Mathf.Sin(i * 0.95f) * 240f, -(height - bottomMargin - i * spacing));
            }

            // Layers, back to front: road bed, road dots, current-level glow, nodes, pin.
            BuildMapPath(content, positions, out Image[] pathDots, out int[] pathDotLevels);

            GameObject glowObject = Child("CurrentGlow", content, typeof(Image));
            Place(glowObject, new Vector2(0.5f, 1), Vector2.zero, new Vector2(330, 330));
            var glowImage = glowObject.GetComponent<Image>();
            glowImage.sprite = glow;
            glowImage.color = new Color(1f, 0.78f, 0.25f, 0.55f);
            glowImage.raycastTarget = false;

            var nodes = new Button[count];
            var stars = new Image[count * 3];
            var numbers = new GameObject[count];
            var locks = new GameObject[count];
            float lip = nodeSize * 16f / 192f;
            for (int i = 0; i < count; i++)
            {
                GameObject nodeObject = Child($"Level{i + 1}", content, typeof(Image));
                Place(nodeObject, new Vector2(0.5f, 1), positions[i], new Vector2(nodeSize, nodeSize));
                nodeObject.GetComponent<Image>().sprite = node;
                nodeObject.GetComponent<Image>().color = Green; // LevelMapScreen sets the real state color.
                Button button = MakeClickable(nodeObject);
                ColorBlock colors = button.colors;
                colors.disabledColor = Color.white; // LevelMapScreen gives locked nodes their own color.
                button.colors = colors;
                UnityEventTools.AddIntPersistentListener(button.onClick, screen.PlayLevel, i);
                nodes[i] = button;

                TMP_Text number = Label(nodeObject.transform, "Number", (i + 1).ToString(), 72, TextWhite, FontStyles.Bold);
                Stretch(number.rectTransform, 0, lip, 0, 0);
                numbers[i] = number.gameObject;

                GameObject lockIcon = Child("Lock", nodeObject.transform, typeof(Image));
                Place(lockIcon, new Vector2(0.5f, 0.5f), new Vector2(0, lip / 2f), new Vector2(72, 72));
                var lockImage = lockIcon.GetComponent<Image>();
                lockImage.sprite = LoadSprite(LockIconPath);
                lockImage.color = new Color(1f, 1f, 1f, 0.7f);
                lockImage.raycastTarget = false;
                locks[i] = lockIcon;

                // Stars on a small arc under the node.
                for (int s = 0; s < 3; s++)
                {
                    stars[i * 3 + s] = StarImage(nodeObject.transform, $"Star{s + 1}", star, new Vector2(0.5f, 0.5f),
                        new Vector2((s - 1) * 54, s == 1 ? -128 : -116), s == 1 ? 52 : 46);
                }
            }

            GameObject marker = Child("Marker", content, typeof(Image));
            Place(marker, new Vector2(0.5f, 1), Vector2.zero, new Vector2(96, 96));
            var markerImage = marker.GetComponent<Image>();
            markerImage.sprite = LoadSprite(PinIconPath);
            markerImage.color = Yellow;
            markerImage.raycastTarget = false;
            var markerShadow = marker.AddComponent<Shadow>();
            markerShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            markerShadow.effectDistance = new Vector2(0, -6);

            WireArray(screen, "nodes", nodes);
            WireArray(screen, "stars", stars);
            WireArray(screen, "numberLabels", numbers);
            WireArray(screen, "lockIcons", locks);
            WireArray(screen, "pathDots", pathDots);
            var serialized = new SerializedObject(screen);
            SerializedProperty levels = serialized.FindProperty("pathDotLevels");
            levels.arraySize = pathDotLevels.Length;
            for (int i = 0; i < pathDotLevels.Length; i++)
            {
                levels.GetArrayElementAtIndex(i).intValue = pathDotLevels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Wire(screen, "marker", marker.transform);
            Wire(screen, "currentGlow", glowImage);
            Wire(screen, "scrollRect", scroll);
            OnClick(back, screen.Back);

            Save(ui.Scene, SceneNames.LevelMap);
        }

        /// <summary>
        /// The road between level nodes: a smooth curve through them, drawn as a
        /// solid bed with dots on it. Dots near a node are left out, so they
        /// never touch the node or its stars.
        /// </summary>
        private static void BuildMapPath(Transform content, Vector2[] nodes, out Image[] dots, out int[] dotLevels)
        {
            GameObject bedLayer = Child("PathBed", content);
            Stretch((RectTransform)bedLayer.transform);
            GameObject dotLayer = Child("PathDots", content);
            Stretch((RectTransform)dotLayer.transform);

            const float bedStep = 12f;
            const float dotStep = 44f;
            const float clearance = 140f;
            var dotList = new List<Image>();
            var levelList = new List<int>();
            for (int i = 0; i + 1 < nodes.Length; i++)
            {
                Vector2 p0 = nodes[Mathf.Max(i - 1, 0)];
                Vector2 p1 = nodes[i];
                Vector2 p2 = nodes[i + 1];
                Vector2 p3 = nodes[Mathf.Min(i + 2, nodes.Length - 1)];

                float sinceBed = bedStep;
                float sinceDot = dotStep / 2f;
                Vector2 previous = p1;
                for (int k = 1; k <= 200; k++)
                {
                    Vector2 point = CatmullRom(p0, p1, p2, p3, k / 200f);
                    float step = Vector2.Distance(previous, point);
                    previous = point;
                    sinceBed += step;
                    sinceDot += step;

                    if (sinceBed >= bedStep)
                    {
                        sinceBed = 0f;
                        GameObject bed = RoundedPanel("Bed", bedLayer.transform, PathBed, 3f);
                        Place(bed, new Vector2(0.5f, 1), point, new Vector2(64, 64));
                        bed.GetComponent<Image>().raycastTarget = false;
                    }

                    bool nearNode = Vector2.Distance(point, p1) < clearance || Vector2.Distance(point, p2) < clearance;
                    if (sinceDot >= dotStep && !nearNode)
                    {
                        sinceDot = 0f;
                        GameObject dot = RoundedPanel("Dot", dotLayer.transform, Color.white, 3f);
                        Place(dot, new Vector2(0.5f, 1), point, new Vector2(24, 24));
                        var image = dot.GetComponent<Image>();
                        image.raycastTarget = false;
                        dotList.Add(image);
                        levelList.Add(i + 2); // This stretch leads to level i + 2.
                    }
                }
            }

            dots = dotList.ToArray();
            dotLevels = levelList.ToArray();
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        // ---------------------------------------------------------------- Menu decor

        /// <summary>Jungle foliage along the bottom of the screen, behind everything in the safe area.</summary>
        private static void Foliage(Transform safe)
        {
            GameObject go = FoliageStrip(safe.parent);
            go.transform.SetSiblingIndex(safe.GetSiblingIndex()); // Just under the safe area, above the background.
        }

        /// <summary>Jungle foliage along the bottom of <paramref name="parent"/>.</summary>
        private static GameObject FoliageStrip(Transform parent)
        {
            GameObject go = Child("Foliage", parent, typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 420); // The sprite's 720×280 shape at the 1080 reference width.
            var image = go.GetComponent<Image>();
            image.sprite = foliage;
            image.raycastTarget = false;
            return go;
        }

        /// <summary>A decorative letter tile that bobs and sways; <paramref name="alpha"/> below 1 pushes it into the distance.</summary>
        private static void FloatingTile(Transform parent, string letter, int hint, Vector2 anchor, Vector2 position, float size,
            float angle, float alpha)
        {
            GameObject tile = TileFace($"Tile{letter}", parent, Gameplay.View.TileView.ColorForHint(hint));
            Place(tile, anchor, position, new Vector2(size, size));
            tile.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            TMP_Text label = Label(tile.transform, "Letter", letter, size * 0.56f, TextWhite, FontStyles.Bold);
            TileLetter(label);
            tile.AddComponent<CanvasGroup>().alpha = alpha;
            tile.AddComponent<Drift>();
        }

        /// <summary>A white icon on a button face, raised above its lip: centered when <paramref name="x"/> is 0, otherwise that far from the left edge.</summary>
        private static Image ButtonIcon(GameObject face, string iconPath, float height, float size, float x)
        {
            GameObject icon = Child("Icon", face.transform, typeof(Image));
            Place(icon, new Vector2(x == 0f ? 0.5f : 0f, 0.5f), new Vector2(x, ButtonLipHeight(height) / 2f), new Vector2(size, size));
            var image = icon.GetComponent<Image>();
            image.sprite = LoadSprite(iconPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A square, colorful 3D menu button with a big icon and a caption under it.</summary>
        private static Button MenuTile(Transform parent, string name, string caption, string iconPath, Color color, Vector2 position)
        {
            const float size = 170f;
            GameObject face = ButtonFace(name, parent, color, size);
            Place(face, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            Button button = MakeClickable(face);
            ButtonIcon(face, iconPath, size, 92, 0);

            TMP_Text label = Label(parent, name + "Caption", caption, 30, TextWhite, FontStyles.Bold);
            label.enableAutoSizing = true;
            label.fontSizeMin = 18;
            label.fontSizeMax = 30;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Place(label.gameObject, new Vector2(0.5f, 0.5f), position + new Vector2(0, -size / 2f - 34), new Vector2(200, 44));
            return button;
        }

        // ---------------------------------------------------------------- Helpers

        /// <summary>A full-screen page with a title and a back button; children go in <paramref name="content"/>.</summary>
        private static GameObject ScreenPanel(string name, Transform parent, string title, out RectTransform content, out Button back)
        {
            GameObject root = Child(name, parent, typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().sprite = gradient;
            FoliageStrip(root.transform);

            Header(root.transform, title, out back);

            GameObject body = Child("Content", root.transform);
            content = (RectTransform)body.transform;
            Stretch(content, 40, 40, 40, 200);
            AddPopIn(root, body, startScale: 0.96f);
            return root;
        }

        private static void Header(Transform parent, string title, out Button back)
        {
            TMP_Text label = Label(parent, "Title", title, 58, TextWhite, FontStyles.Bold);
            Place(label.gameObject, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(760, 90));

            const float size = 110f;
            GameObject backButton = ButtonFace("BackButton", parent, Secondary, size);
            Place(backButton, new Vector2(0, 1), new Vector2(90, -95), new Vector2(size, size));
            back = MakeClickable(backButton);
            Stretch(Label(backButton.transform, "Arrow", "<", 60, TextWhite, FontStyles.Bold).rectTransform,
                0, ButtonLipHeight(size), 0, 0);
        }

        /// <summary>A vertical scrolling list filling <paramref name="parent"/> below <paramref name="top"/>; returns where items go.</summary>
        private static RectTransform ScrollList(Transform parent, float top)
        {
            GameObject viewport = Child("Viewport", parent, typeof(Image), typeof(RectMask2D));
            Stretch((RectTransform)viewport.transform, 0, 0, 0, top);
            viewport.GetComponent<Image>().color = Color.clear;

            GameObject list = Child("List", viewport.transform);
            var rect = (RectTransform)list.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            VerticalStack(list, padding: 0, spacing: 20);
            list.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;
            return rect;
        }

        /// <param name="top">True to show it over the whole top bar (in the game, where the board and boosters are busy).</param>
        private static Toast BuildToast(Transform parent, bool top = false)
        {
            GameObject root = CardPanel("Toast", parent, ToastBackground);
            if (top)
            {
                Place(root, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1020, 200));
            }
            else
            {
                Place(root, new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(940, 120));
            }

            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            TMP_Text label = Label(root.transform, "Label", "", 34, Yellow, FontStyles.Bold);
            label.enableAutoSizing = true;
            label.fontSizeMin = 22;
            label.fontSizeMax = 34;
            Stretch(label.rectTransform, 30, 0, 30, 0);

            var toast = root.AddComponent<Toast>();
            Wire(toast, "group", group);
            Wire(toast, "label", label);
            return toast;
        }
    }
}
