using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Throwaway on-screen day/phase/clock readout for development. Reads DayManager each frame and
    /// draws it with IMGUI. Replaced by the real day HUD in the polish pass.
    /// </summary>
    public class DayDebugHud : MonoBehaviour
    {
        private const float LABEL_X = 10.0f;
        private const float LABEL_Y = 10.0f;
        private const float LABEL_WIDTH = 360.0f;
        private const float LABEL_HEIGHT = 30.0f;
        private const int FONT_SIZE = 22;
        private const float MINUTES_PER_HOUR = 60.0f;

        private static readonly Color LABEL_COLOR = new Color(0.49f, 0.71f, 0.84f);

        private GUIStyle labelStyle;

        private void OnGUI()
        {
            if (DayManager.Instance != null)
            {
                EnsureStyle();
                int day = DayManager.Instance.CurrentDay;
                DayPhase phase = DayManager.Instance.CurrentPhase;
                float timeOfDay = DayManager.Instance.CurrentTime;
                int hour = Mathf.FloorToInt(timeOfDay);
                int minute = Mathf.FloorToInt((timeOfDay - hour) * MINUTES_PER_HOUR);
                Rect rect = new Rect(LABEL_X, LABEL_Y, LABEL_WIDTH, LABEL_HEIGHT);
                GUI.Label(rect, $"Day {day}  —  {phase}  —  {hour:00}:{minute:00}", labelStyle);
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
