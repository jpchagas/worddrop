using UnityEngine.SceneManagement;

namespace WordDrop.Core
{
    /// <summary>
    /// Goes to a scene through the Loading screen, which loads it in the background.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>The scene the Loading screen should load next.</summary>
        public static string PendingScene { get; private set; }

        public static void LoadWithLoadingScreen(string sceneName)
        {
            PendingScene = sceneName;
            SceneManager.LoadScene(SceneNames.Loading);
        }

        /// <summary>Returns the pending scene and clears it, or the fallback when none was requested.</summary>
        public static string TakePendingScene(string fallback)
        {
            string scene = string.IsNullOrEmpty(PendingScene) ? fallback : PendingScene;
            PendingScene = null;
            return scene;
        }
    }
}
