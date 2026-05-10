using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Disposable smoke test for EventBus. Subscribes to GameStateChangedEvent and
    /// logs every fire. Attach to one GameObject (or two, to verify multi-subscriber),
    /// press Play, press KeypadEnter to trigger a state change via GameManager,
    /// watch the console. Delete this file after validation.
    /// </summary>
    public class EventBusTest : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            Debug.Log($"[EventBusTest:{name}] Received: {evt.OldState} → {evt.NewState}");
        }
    }
}