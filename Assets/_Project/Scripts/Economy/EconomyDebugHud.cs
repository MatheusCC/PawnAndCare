using UnityEngine;

namespace PawsAndCare.Economy
{
    /// <summary>
    /// Throwaway on-screen balance readout for development. Reads the current balance straight from
    /// EconomyManager each frame and draws it with IMGUI — so it shows the starting balance
    /// immediately and never misses an update. Replaced by the real UI (and BalanceChangedEvent-driven
    /// floaters) in the polish pass.
    /// </summary>
    public class EconomyDebugHud : MonoBehaviour
    {
        private const float LABEL_X = 100.0f;
        private const float LABEL_Y = 100.0f;
        private const float LABEL_WIDTH = 500.0f;
        private const float LABEL_HEIGHT = 100.0f;
        private const int FONT_SIZE = 32;

        private static readonly Color LABEL_COLOR = new Color(1.0f, 0.85f, 0.0f);

        private GUIStyle labelStyle;

        private void OnGUI()
        {
            if (EconomyManager.Instance != null)
            {
                EnsureStyle();
                float balance = EconomyManager.Instance.Balance;
                float dailyRevenue = EconomyManager.Instance.DailyRevenue;
                Rect rect = new Rect(LABEL_X, LABEL_Y, LABEL_WIDTH, LABEL_HEIGHT);
                GUI.Label(rect, $"Balance: ${balance:0.00}\nToday: ${dailyRevenue:0.00}", labelStyle);
            }
        }

        // Built lazily on first draw: GUI.skin is only valid inside OnGUI, so the style can't be
        // created in Awake. Cached afterwards so it isn't rebuilt every frame.
        private void EnsureStyle()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = FONT_SIZE;
                labelStyle.normal.textColor = LABEL_COLOR;
            }
        }
    }
}
