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
        private static readonly Color ScreenBackground = Hex(0x0E1A35);

        // ---------------------------------------------------------------- Main menu

        private static void BuildMainMenu()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("MainMenuScreen").AddComponent<MainMenuScreen>();

            var music = new GameObject("MusicPlayer", typeof(AudioSource));
            music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectSetup.MusicPath);
            music.GetComponent<AudioSource>().playOnAwake = false;
            music.AddComponent<MusicPlayer>();

            Logo(ui.Safe, new Vector2(0, -330), 600);

            GameObject column = Child("Buttons", ui.Safe);
            Place(column, new Vector2(0.5f, 0.5f), new Vector2(0, -360), new Vector2(820, 760));
            VerticalStack(column, padding: 0, spacing: 22);

            Button play = PrimaryButton(column.transform, "PlayButton", Strings.Play, 150, 60, out TMP_Text playLabel);
            Button endless = SecondaryButton(column.transform, "EndlessButton", Strings.Endless, 120, out _);
            TMP_Text endlessBest = Label(column.transform, "EndlessBest", "", 30, TextMuted);
            Sized(endlessBest, 34);
            GameObject row1 = ButtonRow(column.transform, "Row1", 110);
            Button daily = SecondaryButton(row1.transform, "DailyChallengeButton", Strings.DailyChallenge, 110, out _);
            Button howTo = SecondaryButton(row1.transform, "HowToPlayButton", Strings.HowToPlay, 110, out _);
            GameObject row2 = ButtonRow(column.transform, "Row2", 110);
            Button achievements = SecondaryButton(row2.transform, "AchievementsButton", Strings.Achievements, 110, out _);
            Button records = SecondaryButton(row2.transform, "LeaderboardsButton", Strings.Leaderboards, 110, out _);
            Button settings = SecondaryButton(column.transform, "SettingsButton", Strings.Settings, 110, out _);

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
            Button music = SecondaryButton(card.transform, "MusicToggle", Strings.MusicOn, 110, out TMP_Text musicLabel);
            Button sound = SecondaryButton(card.transform, "SoundToggle", Strings.SoundOn, 110, out TMP_Text soundLabel);
            Button vibration = SecondaryButton(card.transform, "VibrationToggle", Strings.VibrationOn, 110, out TMP_Text vibrationLabel);
            terms = SecondaryButton(card.transform, "TermsButton", Strings.TermsOfUse, 110, out _);
            privacy = SecondaryButton(card.transform, "PrivacyButton", Strings.PrivacyPolicy, 110, out _);
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
            GameObject card = RoundedPanel($"Step{number}", parent, Card, 1f);
            Sized(card, 290);

            GameObject badge = RoundedPanel("Badge", card.transform, Green, 0.6f);
            Place(badge, new Vector2(0, 1), new Vector2(70, -70), new Vector2(80, 80));
            Stretch(Label(badge.transform, "Number", number.ToString(), 44, TextWhite, FontStyles.Bold).rectTransform);

            TMP_Text titleLabel = Label(card.transform, "Title", title, 40, TextWhite, FontStyles.Bold, TextAlignmentOptions.Left);
            Place(titleLabel.gameObject, new Vector2(0, 1), new Vector2(390, -70), new Vector2(560, 60));
            TMP_Text body = Label(card.transform, "Text", text, 29, TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 20;
            body.fontSizeMax = 29;
            Place(body.gameObject, new Vector2(0, 1), new Vector2(390, -185), new Vector2(560, 160));

            GameObject art = Child("Picture", card.transform);
            Place(art, new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(240, 240));
            picture(art.transform);
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
                GameObject tile = RoundedPanel("Tile", parent, color, 3f);
                Place(tile, new Vector2(0.5f, 0.5f), vertical ? new Vector2(0, -offset) : new Vector2(offset, 0), new Vector2(size, size));
                Stretch(Label(tile.transform, "Letter", letters[i].ToString(), size * 0.62f, TextWhite, FontStyles.Bold).rectTransform);
            }
        }

        /// <summary>Daily Challenge screen (GDD 4.3), following the mockup: date, result, play, claim reward.</summary>
        private static DailyChallengePanel BuildDailyPanel(Transform parent)
        {
            GameObject root = ScreenPanel("DailyPanel", parent, Strings.DailyChallenge, out RectTransform content, out Button back);
            VerticalStack(content.gameObject, padding: 20, spacing: 28);

            TMP_Text date = Label(content, "Date", "22 de outubro de 2026", 50, TextWhite, FontStyles.Bold);
            Sized(date, 80);
            Sized(Label(content, "Goal", Strings.DailyGoal, 34, TextMuted), 60);

            GameObject card = RoundedPanel("ResultCard", content, Card, 1f);
            Sized(card, 420);
            // Shown once today's challenge is completed: a green check and "Desafio concluído!".
            GameObject doneGroup = Child("Done", card.transform);
            Stretch((RectTransform)doneGroup.transform);
            GameObject badge = RoundedPanel("Badge", doneGroup.transform, Green, 0.5f);
            Place(badge, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(160, 160));
            GameObject check = Child("Check", badge.transform, typeof(Image));
            Stretch((RectTransform)check.transform, 28, 28, 28, 28);
            check.GetComponent<Image>().sprite = LoadSprite(CheckIconPath);
            TMP_Text done = Label(doneGroup.transform, "Label", Strings.DailyDone, 40, TextWhite, FontStyles.Bold);
            Place(done.gameObject, new Vector2(0.5f, 1), new Vector2(0, -255), new Vector2(800, 60));
            TMP_Text status = Label(card.transform, "Status", Strings.DailyNotPlayed, 38, TextWhite);
            Place(status.gameObject, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(820, 80));

            Button play = PrimaryButton(content, "PlayButton", Strings.Play, 150, 60, out _);
            Button claim = SecondaryButton(content, "ClaimButton", Strings.ClaimReward, 130, out TMP_Text claimLabel);

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

            GameObject statsCard = RoundedPanel("Stats", list, TrackDark, 1f);
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
            GameObject go = RoundedPanel($"Achievement{index + 1}", parent, Card, 1f);
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

        /// <summary>World 1 as a winding path of level nodes, level 1 at the bottom (GDD 7.1, item 7).</summary>
        private static void BuildLevelMap()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("LevelMapScreen").AddComponent<LevelMapScreen>();
            Sprite star = LoadSprite(StarSpritePath);

            Header(ui.Safe, Strings.LevelMap, out Button back);
            TMP_Text world = Label(ui.Safe, "World", Strings.World1, 34, Yellow, FontStyles.Bold);
            Place(world.gameObject, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(900, 50));

            GameObject scrollObject = Child("Scroll", ui.Safe, typeof(Image), typeof(RectMask2D));
            Stretch((RectTransform)scrollObject.transform, 0, 0, 0, 200);
            scrollObject.GetComponent<Image>().color = Color.clear;
            GameObject contentObject = Child("Content", scrollObject.transform);
            var content = (RectTransform)contentObject.transform;

            const int count = GameSession.CampaignLevelCount;
            const float spacing = 240f;
            const float bottomMargin = 220f;
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

            var nodes = new Button[count];
            var stars = new Image[count * 3];
            Vector2 previous = Vector2.zero;
            // Path dots get their own layer, created first, so they stay behind every node and its stars.
            GameObject dots = Child("Path", content);
            Stretch((RectTransform)dots.transform);
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2(Mathf.Sin(i * 0.95f) * 230f, -(height - bottomMargin - i * spacing));

                if (i > 0)
                {
                    for (int d = 1; d <= 3; d++)
                    {
                        GameObject dot = RoundedPanel("PathDot", dots.transform, new Color(1f, 1f, 1f, 0.25f), 3f);
                        Place(dot, new Vector2(0.5f, 1), Vector2.Lerp(previous, position, d / 4f), new Vector2(26, 26));
                    }
                }

                GameObject node = RoundedPanel($"Level{i + 1}", content, Green, 0.56f);
                Place(node, new Vector2(0.5f, 1), position, new Vector2(170, 170));
                var shadow = node.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.35f);
                shadow.effectDistance = new Vector2(0, -8);
                var button = node.AddComponent<Button>();
                button.targetGraphic = node.GetComponent<Image>();
                var colors = button.colors;
                colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 1f);
                button.colors = colors;
                Stretch(Label(node.transform, "Number", (i + 1).ToString(), 64, TextWhite, FontStyles.Bold).rectTransform);
                UnityEventTools.AddIntPersistentListener(button.onClick, screen.PlayLevel, i);
                nodes[i] = button;

                for (int s = 0; s < 3; s++)
                {
                    stars[i * 3 + s] = StarImage(node.transform, $"Star{s + 1}", star, new Vector2(0.5f, 0f),
                        new Vector2((s - 1) * 48, -30 + (s == 1 ? -6 : 0)), 44);
                }

                previous = position;
            }

            WireArray(screen, "nodes", nodes);
            WireArray(screen, "stars", stars);
            Wire(screen, "scrollRect", scroll);
            OnClick(back, screen.Back);

            Save(ui.Scene, SceneNames.LevelMap);
        }

        // ---------------------------------------------------------------- Helpers

        private static GameObject ButtonRow(Transform parent, string name, float height)
        {
            GameObject row = Child(name, parent);
            Sized(row, height);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>A full-screen page with a title and a back button; children go in <paramref name="content"/>.</summary>
        private static GameObject ScreenPanel(string name, Transform parent, string title, out RectTransform content, out Button back)
        {
            GameObject root = Child(name, parent, typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = ScreenBackground;

            Header(root.transform, title, out back);

            GameObject body = Child("Content", root.transform);
            content = (RectTransform)body.transform;
            Stretch(content, 40, 40, 40, 200);
            return root;
        }

        private static void Header(Transform parent, string title, out Button back)
        {
            TMP_Text label = Label(parent, "Title", title, 58, TextWhite, FontStyles.Bold);
            Place(label.gameObject, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(760, 90));

            GameObject backButton = RoundedPanel("BackButton", parent, Secondary, 0.9f);
            Place(backButton, new Vector2(0, 1), new Vector2(90, -95), new Vector2(110, 110));
            back = backButton.AddComponent<Button>();
            back.targetGraphic = backButton.GetComponent<Image>();
            Stretch(Label(backButton.transform, "Arrow", "<", 60, TextWhite, FontStyles.Bold).rectTransform);
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

        /// <param name="top">True to show it under the top bar (in the game, where the bottom is busy).</param>
        private static Toast BuildToast(Transform parent, bool top = false)
        {
            GameObject root = RoundedPanel("Toast", parent, TrackDark, 1f);
            Place(root, new Vector2(0.5f, top ? 1f : 0f), new Vector2(0, top ? -320 : 230), new Vector2(940, 120));
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
