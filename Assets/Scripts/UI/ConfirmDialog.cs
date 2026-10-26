using System;
using TMPro;
using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>A yes/no question, e.g. before leaving a level and losing progress (GDD 7.1).</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageLabel;

        private Action onYes;

        public void Show(string message, Action yes)
        {
            messageLabel.text = message;
            onYes = yes;
            gameObject.SetActive(true);
        }

        public void Confirm()
        {
            gameObject.SetActive(false);
            Action action = onYes;
            onYes = null;
            action?.Invoke();
        }

        public void Cancel()
        {
            onYes = null;
            gameObject.SetActive(false);
        }
    }
}
