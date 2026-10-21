using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// One-click project configuration for a portrait mobile game.
    /// From the command line: -executeMethod WordDrop.EditorTools.ProjectSetup.RunAll
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "Cruze";
        public const string ProductName = "Word Drop";
        public const string BundleId = "com.cruze.worddrop";

        public const string CruzeLogoPath = "Assets/Art/Branding/cruze_logo.png";
        public const string WordDropLogoPath = "Assets/Art/Branding/worddrop_logo.png";
        public const string MusicPath = "Assets/Audio/Music/manyuko_burger_flip.mp3";

        [MenuItem("Word Drop/Configurar projeto e gerar cenas")]
        public static void RunAll()
        {
            ConfigurePlayerSettings();
            ConfigureImports();
            SceneBuilder.BuildAllScenes();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Word Drop] Projeto configurado e cenas geradas.");
        }

        [MenuItem("Word Drop/Somente configurações do projeto")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);

            // Portrait only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Desktop/Web test builds open in a phone-shaped window.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;

            // Google Play requires 64-bit (ARM64), which needs IL2CPP.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // We show our own Cruze splash scene instead of Unity's.
            PlayerSettings.SplashScreen.show = false;
        }

        private static void ConfigureImports()
        {
            foreach (string path in new[] { CruzeLogoPath, WordDropLogoPath })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var audio = (AudioImporter)AssetImporter.GetAtPath(MusicPath);
            AudioImporterSampleSettings settings = audio.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming; // Long track: stream instead of holding it all in memory.
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            audio.defaultSampleSettings = settings;
            audio.loadInBackground = true;
            audio.SaveAndReimport();
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = SceneNames.All
                .Select(name => new EditorBuildSettingsScene(SceneBuilder.ScenePath(name), true))
                .ToArray();
        }
    }
}
