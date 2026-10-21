using UnityEngine;

namespace WordDrop.Core
{
    /// <summary>
    /// App-wide setup that runs once at startup, before any scene loads.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Mobile defaults to 30 fps; animations look much smoother at 60.
            Application.targetFrameRate = 60;
            // Keep the screen awake while playing.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
