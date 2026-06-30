using System;
using UnityEngine;
using TMPro;

namespace PawsAndCare.UI
{
    /// <summary>
    /// A single feedback popup (e.g. "+$25", "-3 rep") that drifts and fades. On finishing it raises
    /// the onComplete callback so the owner (a pool) can recycle it — the popup never deactivates or
    /// destroys itself, so the pool's active/inactive bookkeeping stays correct. Lives on its own
    /// canvas so its per-frame animation doesn't rebuild the status-bar batch.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FloatingPopup : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The text element this popup drives.")]
        private TMP_Text label = null;

        [SerializeField]
        [Tooltip("Direction + distance (pixels) the popup drifts over its lifetime. For a top status bar, use a negative Y so it drifts downward and stays on screen.")]
        private Vector2 floatOffset = new Vector2(0.0f, -60.0f);

        [SerializeField]
        [Tooltip("Lifetime in seconds before it fades out and completes.")]
        private float duration = 1.0f;

        [SerializeField]
        [Tooltip("Eases the drift over [0,1]; add overshoot here for a bouncier pop.")]
        private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Action<FloatingPopup> onComplete;
        private Vector2 startPosition;
        private float elapsed;
        private bool playing;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = (RectTransform)transform;
        }

        /// <summary>
        /// Sets the popup's text/color and starts the drift/fade. onComplete is invoked when it
        /// finishes — the owner uses it to release the popup back to its pool. The popup never
        /// deactivates or destroys itself.
        /// </summary>
        public void Play(string text, Color color, Action<FloatingPopup> onComplete)
        {
            this.onComplete = onComplete;

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            startPosition = rectTransform.anchoredPosition;
            elapsed = 0.0f;
            canvasGroup.alpha = 1.0f;
            playing = true;
        }

        private void Update()
        {
            if (playing)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                rectTransform.anchoredPosition = startPosition + (floatOffset * moveCurve.Evaluate(progress));
                canvasGroup.alpha = 1.0f - progress;

                if (progress >= 1.0f)
                {
                    playing = false;

                    // Hand lifetime back to the owner (the pool deactivates + recycles it). The popup
                    // must not deactivate or destroy itself — that would bypass the pool and desync
                    // its active/inactive count.
                    if (onComplete != null)
                    {
                        onComplete(this);
                    }
                    else
                    {
                        Debug.LogError("[FloatingPopup] Finished with no onComplete handler — it won't be recycled. Pass the pool-release callback to Play().", this);
                    }
                }
            }
        }
    }
}
