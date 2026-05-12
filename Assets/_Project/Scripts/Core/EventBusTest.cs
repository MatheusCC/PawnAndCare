using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Disposable smoke test for EventBus — subscribes to GameStateChangedEvent and
    /// logs every fire.
    /// </summary>
    // Attach to one GameObject (or two, to verify multi-subscriber), press Play,
    // trigger a state change via GameManager.ChangeState, watch the console.
    // Delete this file after validation.
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