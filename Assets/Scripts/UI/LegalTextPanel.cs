using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;

namespace WordDrop.UI
{
    /// <summary>
    /// Full-screen panel that shows the Terms of Use or the Privacy Policy.
    /// The texts are plain .txt files in Assets/Content/Legal.
    /// </summary>
    public class LegalTextPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TextAsset termsOfUse;
        [SerializeField] private TextAsset privacyPolicy;

        public void ShowTermsOfUse() => Show(Strings.TermsOfUse, termsOfUse);

        public void ShowPrivacyPolicy() => Show(Strings.PrivacyPolicy, privacyPolicy);

        public void Hide() => gameObject.SetActive(false);

        private void Show(string title, TextAsset text)
        {
            titleLabel.text = title;
            bodyLabel.text = text != null ? text.text : string.Empty;
            gameObject.SetActive(true);
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
