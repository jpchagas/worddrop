using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Screens
{
    /// <summary>
    /// First-launch screen asking the player to accept the Terms of Use and the
    /// Privacy Policy. Accepting is saved, so it is not shown again until the
    /// legal version changes (see <see cref="PlayerData.CurrentLegalVersion"/>).
    /// </summary>
    public class ConsentScreen : MonoBehaviour
    {
        public void Accept()
        {
            PlayerData.AcceptLegal();
            SceneLoader.LoadWithLoadingScreen(SceneNames.MainMenu);
        }
    }
}
