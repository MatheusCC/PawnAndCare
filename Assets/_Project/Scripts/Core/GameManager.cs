using UnityEngine;

namespace PawsAndCare.Core
{
    public enum GameState
    {
        MAIN_MENU = 0,
        PLAYING = 1,
        PAUSED = 2,
        LOADING = 3
    }

    /// <summary>
    /// Central authority for the game's high-level state.
    /// Owns the GameState lifecycle; ChangeState is the single mutation entry point.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        private GameState initialState; 
        private GameState currentState;
        private GameState previousState;

        public GameState CurrentState 
        { 
            get { return currentState; }
        }

        public GameState PreviousState 
        { 
            get { return previousState; }
        }

        protected override void OnInitialize()
        {
            initialState = GameState.PLAYING;

            currentState = initialState;
            previousState = initialState;
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ChangeState(GameState.PAUSED);
            }           
        }



        /// <summary>
        /// Transitions the game to a new state. Same-state calls are ignored.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState != currentState)
            {
                previousState = currentState;
                currentState = newState;

#if UNITY_EDITOR
                Debug.Log($"[GameManager] State: {previousState} → {currentState}");
#endif

                EventBus.Publish(new GameStateChangedEvent(previousState, currentState));
            }
        }
    }
}
