using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Screens;
using WordDrop.UI;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// Generates the Splash, Consent, Loading and MainMenu scenes and their UI.
    /// Running it again OVERWRITES those scenes, so after you start tweaking a
    /// scene by hand in the Editor, stop regenerating it.
    /// </summary>
    public static partial class SceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string UiArtFolder = "Assets/Art/UI";
        private const string RoundedSpritePath = UiArtFolder + "/rounded_rect.png";
        private const string GradientSpritePath = UiArtFolder + "/background_gradient.png";
        private const string TermsPath = "Assets/Content/Legal/termos_de_uso.txt";
        private const string PrivacyPath = "Assets/Content/Legal/politica_de_privacidade.txt";

        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // Palette from screens_draft.png.
        private static readonly Color SplashBackground = Hex(0x0B0B12);
        private static readonly Color GradientTop = Hex(0x1B3058);
        private static readonly Color GradientBottom = Hex(0x0A1226);
        private static readonly Color Card = Hex(0x15264A);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color Green = Hex(0x3DB54A);
        private static readonly Color Secondary = Hex(0x243B66);
        private static readonly Color TrackDark = Hex(0x081022);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextMuted = Hex(0xB4C2DC);
        private static readonly Color Yellow = Hex(0xFFC83A);

        private static Sprite rounded;
        private static Sprite gradient;
        private static TMP_FontAsset font;

        public static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        public static void BuildAllScenes()
        {
            CreateUiSprites();
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            gradient = AssetDatabase.LoadAssetAtPath<Sprite>(GradientSpritePath);
            font = TMP_Settings.defaultFontAsset;

            BuildSplash();
            BuildConsent();
            BuildLoading();
            BuildMainMenu();
            BuildGame();
        }

        // ---------------------------------------------------------------- Scenes

        private static void BuildSplash()
        {
            var ui = NewUiScene(SplashBackground, useGradient: false);

            GameObject logo = Child("CruzeLogo", ui.Safe, typeof(Image), typeof(CanvasGroup));
            Place(logo, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(560, 716));
            var image = logo.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectSetup.CruzeLogoPath);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var screen = new GameObject("SplashScreen").AddComponent<SplashScreen>();
            Wire(screen, "logoGroup", logo.GetComponent<CanvasGroup>());

            Save(ui.Scene, SceneNames.Splash);
        }

        private static void BuildConsent()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("ConsentScreen").AddComponent<ConsentScreen>();

            Logo(ui.Safe, new Vector2(0, -380), 760);

            GameObject card = RoundedPanel("Card", ui.Safe, Card, 1f);
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(940, 900));
            VerticalStack(card, padding: 60, spacing: 30);

            Sized(Label(card.transform, "Title", Strings.ConsentTitle, 64, TextWhite, FontStyles.Bold), 90);
            Sized(Label(card.transform, "Body", Strings.ConsentBody, 40, TextMuted), 200);
            Button terms = SecondaryButton(card.transform, "TermsButton", Strings.TermsOfUse, 110, out _);
            Button privacy = SecondaryButton(card.transform, "PrivacyButton", Strings.PrivacyPolicy, 110, out _);
            Button accept = PrimaryButton(card.transform, "AcceptButton", Strings.AcceptAndContinue, 140, 56, out _);

            LegalTextPanel legal = LegalPanel(ui.Safe);
            OnClick(terms, legal.ShowTermsOfUse);
            OnClick(privacy, legal.ShowPrivacyPolicy);
            OnClick(accept, screen.Accept);

            Save(ui.Scene, SceneNames.Consent);
        }

        private static void BuildLoading()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);

            Logo(ui.Safe, new Vector2(0, 220), 820, anchor: new Vector2(0.5f, 0.5f));

            TMP_Text loading = Label(ui.Safe, "LoadingLabel", Strings.Loading, 44, TextMuted);
            Place(loading.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(760, 60));

            GameObject track = RoundedPanel("ProgressTrack", ui.Safe, TrackDark, 1.7f);
            Place(track, new Vector2(0.5f, 0.5f), new Vector2(0, -360), new Vector2(760, 56));

            GameObject fill = RoundedPanel("ProgressFill", track.transform, Green, 1.7f);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.35f, 1f); // Preview value; LoadingScreen drives it at runtime.
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            TMP_Text percent = Label(ui.Safe, "PercentLabel", "35%", 40, TextWhite, FontStyles.Bold);
            Place(percent.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0, -440), new Vector2(300, 60));

            var screen = new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
            Wire(screen, "progressFill", fillRect);
            Wire(screen, "percentLabel", percent);

            Save(ui.Scene, SceneNames.Loading);
        }

        private static void BuildMainMenu()
        {
            var ui = NewUiScene(GradientBottom, useGradient: true);
            var screen = new GameObject("MainMenuScreen").AddComponent<MainMenuScreen>();

            var music = new GameObject("MusicPlayer", typeof(AudioSource));
            music.GetComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectSetup.MusicPath);
            music.GetComponent<AudioSource>().playOnAwake = false;
            music.AddComponent<MusicPlayer>();

            Logo(ui.Safe, new Vector2(0, -420), 840);

            GameObject column = Child("Buttons", ui.Safe);
            Place(column, new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(800, 680));
            VerticalStack(column, padding: 0, spacing: 36);

            Button play = PrimaryButton(column.transform, "PlayButton", Strings.Play, 170, 76, out _);
            Button daily = SecondaryButton(column.transform, "DailyChallengeButton", Strings.DailyChallenge, 130, out _);
            Button howTo = SecondaryButton(column.transform, "HowToPlayButton", Strings.HowToPlay, 130, out _);
            Button settings = SecondaryButton(column.transform, "SettingsButton", Strings.Settings, 130, out _);

            TMP_Text credits = Label(ui.Safe, "Credits", Strings.Credits, 26, TextMuted);
            Place(credits.gameObject, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(1000, 50));

            GameObject toast = RoundedPanel("Toast", ui.Safe, TrackDark, 1f);
            Place(toast, new Vector2(0.5f, 0f), new Vector2(0, 220), new Vector2(520, 110));
            var toastGroup = toast.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            Stretch(Label(toast.transform, "Label", Strings.ComingSoon, 48, Yellow, FontStyles.Bold).rectTransform);

            // Settings panel
            GameObject settingsPanel = Child("SettingsPanel", ui.Safe, typeof(Image));
            Stretch((RectTransform)settingsPanel.transform);
            settingsPanel.GetComponent<Image>().color = Dim;

            GameObject settingsCard = RoundedPanel("Card", settingsPanel.transform, Card, 1f);
            Place(settingsCard, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 820));
            VerticalStack(settingsCard, padding: 60, spacing: 30);

            Sized(Label(settingsCard.transform, "Title", Strings.Settings, 64, TextWhite, FontStyles.Bold), 100);
            Button musicToggle = SecondaryButton(settingsCard.transform, "MusicToggle", Strings.MusicOn, 120, out TMP_Text musicLabel);
            Button terms = SecondaryButton(settingsCard.transform, "TermsButton", Strings.TermsOfUse, 120, out _);
            Button privacy = SecondaryButton(settingsCard.transform, "PrivacyButton", Strings.PrivacyPolicy, 120, out _);
            Button close = PrimaryButton(settingsCard.transform, "CloseButton", Strings.Close, 130, 52, out _);
            settingsPanel.SetActive(false);

            LegalTextPanel legal = LegalPanel(ui.Safe);

            Wire(screen, "settingsPanel", settingsPanel);
            Wire(screen, "musicToggleLabel", musicLabel);
            Wire(screen, "toast", toastGroup);

            OnClick(play, screen.Play);
            OnClick(daily, screen.DailyChallenge);
            OnClick(howTo, screen.HowToPlay);
            OnClick(settings, screen.OpenSettings);
            OnClick(musicToggle, screen.ToggleMusic);
            OnClick(terms, legal.ShowTermsOfUse);
            OnClick(privacy, legal.ShowPrivacyPolicy);
            OnClick(close, screen.CloseSettings);

            Save(ui.Scene, SceneNames.MainMenu);
        }

        // ---------------------------------------------------------------- Shared pieces

        private struct UiScene
        {
            public Scene Scene;
            public Transform Safe;
        }

        private static UiScene NewUiScene(Color cameraColor, bool useGradient)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraColor;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject background = Child("Background", canvasObject.transform, typeof(Image));
            Stretch((RectTransform)background.transform);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            if (useGradient)
            {
                backgroundImage.sprite = gradient;
            }
            else
            {
                backgroundImage.color = cameraColor;
            }

            GameObject safe = Child("SafeArea", canvasObject.transform, typeof(SafeArea));
            Stretch((RectTransform)safe.transform);

            return new UiScene { Scene = scene, Safe = safe.transform };
        }

        private static void Logo(Transform parent, Vector2 position, float size, Vector2? anchor = null)
        {
            GameObject logo = Child("WordDropLogo", parent, typeof(Image));
            Place(logo, anchor ?? new Vector2(0.5f, 1f), position, new Vector2(size, size));
            var image = logo.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectSetup.WordDropLogoPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static LegalTextPanel LegalPanel(Transform parent)
        {
            GameObject root = Child("LegalPanel", parent, typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = Dim;

            GameObject card = RoundedPanel("Card", root.transform, Card, 1f);
            Stretch((RectTransform)card.transform, 50, 80, 50, 80);

            TMP_Text title = Label(card.transform, "Title", Strings.TermsOfUse, 56, TextWhite, FontStyles.Bold);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(-80, 90);

            GameObject viewport = Child("Viewport", card.transform, typeof(Image), typeof(RectMask2D));
            Stretch((RectTransform)viewport.transform, 50, 200, 50, 150);
            viewport.GetComponent<Image>().color = Color.clear; // Invisible, but catches drags for scrolling.

            TMP_Text body = Label(viewport.transform, "Body", string.Empty, 34, TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0, 1);
            bodyRect.anchorMax = new Vector2(1, 1);
            bodyRect.pivot = new Vector2(0.5f, 1);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0, 0);
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = card.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = bodyRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

            Button close = PrimaryButton(card.transform, "CloseButton", Strings.Close, 120, 50, out _);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.anchoredPosition = new Vector2(0, 100);
            closeRect.sizeDelta = new Vector2(600, 120);

            var panel = root.AddComponent<LegalTextPanel>();
            Wire(panel, "titleLabel", title);
            Wire(panel, "bodyLabel", body);
            Wire(panel, "scrollRect", scroll);
            Wire(panel, "termsOfUse", AssetDatabase.LoadAssetAtPath<TextAsset>(TermsPath));
            Wire(panel, "privacyPolicy", AssetDatabase.LoadAssetAtPath<TextAsset>(PrivacyPath));
            OnClick(close, panel.Hide);

            root.SetActive(false);
            return panel;
        }

        private static Button PrimaryButton(Transform parent, string name, string text, float height, float fontSize, out TMP_Text label)
        {
            return MakeButton(parent, name, text, Green, height, fontSize, out label);
        }

        private static Button SecondaryButton(Transform parent, string name, string text, float height, out TMP_Text label)
        {
            return MakeButton(parent, name, text, Secondary, height, 46, out label);
        }

        private static Button MakeButton(Transform parent, string name, string text, Color color, float height, float fontSize, out TMP_Text label)
        {
            GameObject go = RoundedPanel(name, parent, color, 1.2f);
            Sized(go, height);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -8);

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            label = Label(go.transform, "Label", text, fontSize, TextWhite, FontStyles.Bold);
            Stretch(label.rectTransform, 20, 0, 20, 0);
            return button;
        }

        // ---------------------------------------------------------------- Low-level helpers

        private static GameObject Child(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                go.AddComponent(component);
            }

            return go;
        }

        private static GameObject RoundedPanel(string name, Transform parent, Color color, float cornerScale)
        {
            GameObject go = Child(name, parent, typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale; // Higher = smaller corner radius.
            image.color = color;
            return go;
        }

        private static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = Child(name, parent);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(GameObject go, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void VerticalStack(GameObject go, int padding, float spacing)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Sized(Component component, float height) => Sized(component.gameObject, height);

        private static void Sized(GameObject go, float height)
        {
            if (!go.TryGetComponent(out LayoutElement element))
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.preferredHeight = height;
        }

        private static void Wire(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string field, params Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void OnClick(Button button, UnityAction action)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void Save(Scene scene, string sceneName)
        {
            Directory.CreateDirectory(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath(sceneName));
        }

        private static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        // ---------------------------------------------------------------- Generated sprites

        private static void CreateUiSprites()
        {
            Directory.CreateDirectory(UiArtFolder);

            const int size = 128;
            const float radius = 48f;
            var roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the pixel to the rounded rectangle's inner core, for smooth edges.
                    float dx = Mathf.Max(radius - (x + 0.5f), 0f, (x + 0.5f) - (size - radius));
                    float dy = Mathf.Max(radius - (y + 0.5f), 0f, (y + 0.5f) - (size - radius));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    roundedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            WriteSprite(roundedTexture, RoundedSpritePath, new Vector4(radius, radius, radius, radius));

            var gradientTexture = new Texture2D(4, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++)
            {
                Color color = Color.Lerp(GradientBottom, GradientTop, y / 255f);
                for (int x = 0; x < 4; x++)
                {
                    gradientTexture.SetPixel(x, y, color);
                }
            }

            WriteSprite(gradientTexture, GradientSpritePath, Vector4.zero);
        }

        private static void WriteSprite(Texture2D texture, string path, Vector4 border)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
