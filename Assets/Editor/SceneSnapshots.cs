using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// Renders each scene to a PNG in a phone resolution, to review layouts
    /// without opening the Editor. Needs a GPU (do not pass -nographics):
    /// -executeMethod WordDrop.EditorTools.SceneSnapshots.CaptureAll
    /// Images are written to Snapshots/ (ignored by git).
    /// </summary>
    public static class SceneSnapshots
    {
        private const string OutputFolder = "Snapshots";
        private const int Width = 1080;
        private const int Height = 1920;

        [MenuItem("Word Drop/Gerar capturas das telas")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);

            foreach (string sceneName in SceneNames.All)
            {
                EditorSceneManager.OpenScene(SceneBuilder.ScenePath(sceneName));
                Capture(sceneName);
            }

            // Overlay panels, which start hidden.
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.Consent));
            CaptureWithPanel("LegalPanel", "Consent_Legal", panel =>
                panel.GetComponent<UI.LegalTextPanel>().ShowTermsOfUse());

            foreach (string panel in new[] { "SettingsPanel", "HowToPlayPanel", "DailyPanel", "AchievementsPanel", "LeaderboardPanel" })
            {
                EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.MainMenu));
                CaptureWithPanel(panel, "MainMenu_" + panel, null);
            }

            foreach (string panel in new[] { "BriefingPanel", "PausePanel", "ConfirmDialog", "LevelCompletePanel", "GameOverPanel", "GlossaryPanel" })
            {
                EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.Game));
                CaptureWithPanel(panel, "Game_" + panel, null);
            }

            Debug.Log($"[Word Drop] Capturas salvas em {Path.GetFullPath(OutputFolder)}");
        }

        private static void CaptureWithPanel(string panelName, string fileName, System.Action<GameObject> show)
        {
            GameObject panel = FindInactive(panelName);
            panel.SetActive(true);
            show?.Invoke(panel);
            Capture(fileName);
        }

        private static GameObject FindInactive(string name)
        {
            foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }

            throw new System.InvalidOperationException($"'{name}' not found in the open scene.");
        }

        private static void Capture(string fileName)
        {
            Camera camera = Camera.main;
            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;

            Canvas.ForceUpdateCanvases();
            camera.Render();
            Canvas.ForceUpdateCanvases(); // Layout groups settle after the first pass.
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName + ".png"), image.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }
    }
}
