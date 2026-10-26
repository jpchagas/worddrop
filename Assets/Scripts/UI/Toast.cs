using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>A short message that fades in and out, e.g. "Conquista desbloqueada!". Messages queue up.</summary>
    public class Toast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float seconds = 2.2f;

        private readonly Queue<string> queue = new Queue<string>();
        private bool showing;

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void Show(string message)
        {
            queue.Enqueue(message);
            if (!showing)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            showing = true;
            while (queue.Count > 0)
            {
                label.text = queue.Dequeue();
                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(seconds);
                yield return Fade(1f, 0f);
            }

            showing = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / 0.25f);
                yield return null;
            }

            group.alpha = to;
        }
    }
}
