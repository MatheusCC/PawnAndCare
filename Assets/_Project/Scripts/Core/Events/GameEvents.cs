namespace PawsAndCare.Core
{
    /// <summary>
    /// Published whenever GameManager transitions between game states.
    /// </summary>
    public readonly struct GameStateChangedEvent
    {
        private readonly GameState oldState;
        private readonly GameState newState;

        public GameState OldState { get { return oldState; } }
        public GameState NewState { get { return newState; } }

        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            this.oldState = oldState;
            this.newState = newState;
        }
    }
}
