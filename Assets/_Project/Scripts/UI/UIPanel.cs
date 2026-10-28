using UnityEngine;

namespace PawsAndCare.UI
{
    // Base class for a full-screen/modal uGUI panel (hire screen, day-end summary, pause menu, …).
    // Visibility is driven through a CanvasGroup so the object stays alive for fade tweens later.
    // Open()/Close() route through the UIManager so it can own the pause + stacking bookkeeping in
    // one place; subclasses populate their content in the OnShow/OnHide hooks.
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("While this panel is open, freeze the day/time (management screens). Leave off for non-blocking overlays like alerts.")]
        private bool pausesGame = true;

        private CanvasGroup canvasGroup;

        // Whether this panel should freeze the day cycle while open.
        public bool PausesGame
        {
            get { return pausesGame; }
        }

        // Whether this panel is currently open.
        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            // Author panels start hidden; force a clean closed state at boot regardless of how the
            // CanvasGroup was left in the scene.
            IsOpen = false;
            ApplyVisibility(false);
        }

        // A panel destroyed while open (scene teardown) must release its UIManager entry — otherwise
        // the manager keeps a dead reference and, if this was the last pausing panel, time never
        // resumes. Routing through ClosePanel also runs Hide → OnHide, so subclasses' event
        // unsubscriptions fire too.
        protected virtual void OnDestroy()
        {
            if (IsOpen && UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel(this);
            }
        }

        // Opens this panel through the UIManager (which handles pause + stacking).
        public void Open()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel(this);
            }
            else
            {
                Debug.LogError("[UIPanel] No UIManager in the scene — cannot open panel.", this);
            }
        }

        // Closes this panel through the UIManager.
        public void Close()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel(this);
            }
        }

        // Show/Hide are called by the UIManager only, so the pause bookkeeping stays in one owner.
        public void Show()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                ApplyVisibility(true);
                OnShow();
            }
        }

        public void Hide()
        {
            if (IsOpen)
            {
                IsOpen = false;
                ApplyVisibility(false);
                OnHide();
            }
        }

        // Toggles visibility + interactivity via the CanvasGroup (kept as one place so a fade tween
        // can replace the instant alpha flip later). Null-guarded because Hide can run during this
        // GameObject's own destruction, when the sibling CanvasGroup may already be gone.
        private void ApplyVisibility(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1.0f : 0.0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        // Hook for subclasses to build/refresh content when the panel opens.
        protected virtual void OnShow() { }

        // Hook for subclasses to release/reset content when the panel closes.
        protected virtual void OnHide() { }
    }
}
