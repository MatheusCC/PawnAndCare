using System.Collections.Generic;
using PawsAndCare.Core;

namespace PawsAndCare.UI
{
    /// <summary>
    /// Owns which panels are open and the single pause state that follows from them. A panel opens/
    /// closes through here so the day cycle is frozen while any open panel wants it paused and resumes
    /// only once none do. Single-scene for now — panels are scene objects, so this holds no cross-scene
    /// references beyond the current scene's lifetime.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private List<UIPanel> openPanels;

        protected override void OnInitialize()
        {
            openPanels = new List<UIPanel>();
        }

        /// <summary>Shows the panel and updates the pause state. No-op if it is already open.</summary>
        public void OpenPanel(UIPanel panel)
        {
            if (panel != null && !openPanels.Contains(panel))
            {
                openPanels.Add(panel);
                panel.Show();
                RefreshPause();
            }
        }

        /// <summary>Hides the panel and updates the pause state. No-op if it was not open.</summary>
        public void ClosePanel(UIPanel panel)
        {
            if (panel != null && openPanels.Remove(panel))
            {
                panel.Hide();
                RefreshPause();
            }
        }

        /// <summary>Closes every open panel (e.g. on a hard state change) and resumes time.</summary>
        public void CloseAll()
        {
            for (int i = openPanels.Count - 1; i >= 0; i--)
            {
                openPanels[i].Hide();
            }

            openPanels.Clear();
            RefreshPause();
        }

        // Time stays frozen while any open panel wants it paused; resumes only when none remain.
        private void RefreshPause()
        {
            if (DayManager.Instance != null)
            {
                bool shouldPause = false;

                for (int i = 0; i < openPanels.Count; i++)
                {
                    if (openPanels[i].PausesGame)
                    {
                        shouldPause = true;
                        break;
                    }
                }

                DayManager.Instance.SetPaused(shouldPause);
            }
        }
    }
}
