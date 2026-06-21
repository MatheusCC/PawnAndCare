using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Owns the flow of in-game time and the day cycle. Time is not real-time: it advances at
    /// gameMinutesPerRealSecond (scaled by timeScale). Each day runs a short PreOpen beat, then the
    /// clock runs openingHour → closingHour through Morning/Midday/Afternoon/Closing, then a Closed
    /// beat before auto-rolling to the next day. Phase transitions and day boundaries are broadcast
    /// over the EventBus so other systems (spawning, economy) stay decoupled.
    /// </summary>
    public class DayManager : Singleton<DayManager>
    {
        private const float MINUTES_PER_HOUR = 60.0f;

        [SerializeField]
        [Tooltip("In-game minutes that pass per real second at timeScale 1.")]
        private float gameMinutesPerRealSecond = 2.0f;

        [SerializeField]
        [Tooltip("Speed multiplier on time progression (1x / 2x / 3x).")]
        private float timeScale = 1.0f;

        [SerializeField]
        [Tooltip("Hour (0-24) the facility opens and starts accepting customers.")]
        private float openingHour = 8.0f;

        [SerializeField]
        [Tooltip("Hour the Morning phase ends and Midday begins.")]
        private float morningEndHour = 11.0f;

        [SerializeField]
        [Tooltip("Hour the Midday phase ends and Afternoon begins.")]
        private float middayEndHour = 14.0f;

        [SerializeField]
        [Tooltip("Hour the Afternoon phase ends and Closing begins (stops accepting new customers).")]
        private float closingStartHour = 17.0f;

        [SerializeField]
        [Tooltip("Hour the facility closes and the day ends.")]
        private float closingHour = 18.0f;

        [SerializeField]
        [Tooltip("Real seconds of the between-days PreOpen beat.")]
        private float preOpenSeconds = 3.0f;

        [SerializeField]
        [Tooltip("Real seconds the end-of-day Closed beat holds before the next day starts.")]
        private float closedSeconds = 3.0f;

        private int currentDay;
        private float currentTime;
        private DayPhase currentPhase;
        private bool isRunning;
        private bool isPaused;
        private bool isAcceptingCustomers;
        private float beatTimer;

        public int CurrentDay
        {
            get { return currentDay; }
        }

        public float CurrentTime
        {
            get { return currentTime; }
        }

        public DayPhase CurrentPhase
        {
            get { return currentPhase; }
        }

        public bool IsPaused
        {
            get { return isPaused; }
        }

        public bool IsAcceptingCustomers
        {
            get { return isAcceptingCustomers; }
        }

        protected override void OnInitialize()
        {
            currentDay = 0;
            currentTime = openingHour;
            // Sentinel: the game "starts closed", so the first day opens with a clean CLOSED → PRE_OPEN transition.
            currentPhase = DayPhase.CLOSED;
            isRunning = false;
            isPaused = false;
            isAcceptingCustomers = false;
            beatTimer = 0.0f;
        }

        /// <summary>
        /// Starts the day cycle at day 1. Called by GameManager.BootGame once the facility/NavMesh exist.
        /// </summary>
        public void BeginDayCycle()
        {
            currentDay = 1;
            isRunning = true;
            StartDay();
        }

        /// <summary>
        /// Pauses or resumes the flow of time (e.g. the pause menu). Phase logic is frozen while paused.
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        /// <summary>
        /// Sets the time-progression multiplier (e.g. 1x / 2x / 3x). Values at or below zero are ignored.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            if (scale > 0.0f)
            {
                timeScale = scale;
            }
        }

        private void Update()
        {
            if (isRunning && !isPaused)
            {
                TickDay(Time.deltaTime);
            }
        }

        private void TickDay(float deltaTime)
        {
            if (currentPhase == DayPhase.PRE_OPEN)
            {
                TickPreOpen(deltaTime);
            }
            else if (currentPhase == DayPhase.CLOSED)
            {
                TickClosed(deltaTime);
            }
            else
            {
                TickOpenHours(deltaTime);
            }
        }

        // PreOpen is a fixed real-time planning beat with the clock parked at openingHour.
        private void TickPreOpen(float deltaTime)
        {
            beatTimer -= deltaTime;

            if (beatTimer <= 0.0f)
            {
                EnterPhase(DayPhase.MORNING);
            }
        }

        // Open hours: advance the clock and switch sub-phase as it crosses the hour thresholds.
        private void TickOpenHours(float deltaTime)
        {
            float gameHoursPerSecond = (gameMinutesPerRealSecond * timeScale) / MINUTES_PER_HOUR;
            currentTime += gameHoursPerSecond * deltaTime;

            if (currentTime >= closingHour)
            {
                currentTime = closingHour;
                EnterPhase(DayPhase.CLOSED);
            }
            else
            {
                DayPhase derived = DerivePhase(currentTime);

                if (derived != currentPhase)
                {
                    EnterPhase(derived);
                }
            }
        }

        // Closed is a fixed real-time beat for the end-of-day summary, then the next day auto-starts.
        private void TickClosed(float deltaTime)
        {
            beatTimer -= deltaTime;

            if (beatTimer <= 0.0f)
            {
                currentDay++;
                StartDay();
            }
        }

        private void StartDay()
        {
            currentTime = openingHour;
            EventBus.Publish(new DayStartedEvent(currentDay));
            EnterPhase(DayPhase.PRE_OPEN);
        }

        private void EnterPhase(DayPhase newPhase)
        {
            DayPhase previous = currentPhase;
            currentPhase = newPhase;
            isAcceptingCustomers = IsOpenPhase(newPhase);
            EventBus.Publish(new DayPhaseChangedEvent(previous, newPhase));

            if (newPhase == DayPhase.PRE_OPEN)
            {
                beatTimer = preOpenSeconds;
            }
            else if (newPhase == DayPhase.CLOSED)
            {
                beatTimer = closedSeconds;
                EventBus.Publish(new DayEndedEvent(currentDay));
            }
        }

        private DayPhase DerivePhase(float timeOfDay)
        {
            DayPhase phase = DayPhase.CLOSING;

            if (timeOfDay < morningEndHour)
            {
                phase = DayPhase.MORNING;
            }
            else if (timeOfDay < middayEndHour)
            {
                phase = DayPhase.MIDDAY;
            }
            else if (timeOfDay < closingStartHour)
            {
                phase = DayPhase.AFTERNOON;
            }

            return phase;
        }

        private bool IsOpenPhase(DayPhase phase)
        {
            bool open = phase == DayPhase.MORNING || phase == DayPhase.MIDDAY || phase == DayPhase.AFTERNOON;

            return open;
        }
    }
}
