using UnityEngine;

namespace PawsAndCare.Reputation
{
    /// <summary>
    /// Throwaway on-screen reputation readout for development. Reads ReputationManager each frame and
    /// draws it with IMGUI. Replaced by the real UI in the polish pass.
    /// </summary>
    public class ReputationDebugHud : MonoBehaviour
    {
        private const float LABEL_X = 10.0f;
        private const float LABEL_Y = 50.0f;
        private const float LABEL_WIDTH = 360.0f;
        private const float LABEL_HEIGHT = 30.0f;
        private const int FONT_SIZE = 22;

        private static readonly Color LABEL_COLOR = new Color(0.51f, 0.78f, 0.52f);

        private GUIStyle labelStyle;

        private void OnGUI()
        {
            if (ReputationManager.Instance != null)
            {
                EnsureStyle();
                float reputation = ReputationManager.Instance.CurrentReputation;
                Rect rect = new Rect(LABEL_X, LABEL_Y, LABEL_WIDTH, LABEL_HEIGHT);
                GUI.Label(rect, $"Reputation: {reputation:0}/100", labelStyle);
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
