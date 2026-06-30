using UnityEngine;
using TMPro;
using PawsAndCare.Core;
using PawsAndCare.Economy;
using PawsAndCare.Reputation;

namespace PawsAndCare.UI
{
    /// <summary>
    /// Always-on status bar: balance, reputation, day, clock, and phase. Updated purely by events —
    /// each field is rewritten only when its value actually changes (balance/reputation/day/phase on
    /// their change events, the clock once per in-game minute), so there is no per-frame work or
    /// allocation. Current values are pulled once on enable/start for the initial display. Every field
    /// is optional. Replaces the temporary OnGUI debug HUDs.
    /// </summary>
    public class StatusBarHud : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Shows the current balance, e.g. \"$125.00\".")]
        private TMP_Text moneyText = null;

        [SerializeField]
        [Tooltip("Shows the current reputation, e.g. \"52/100\".")]
        private TMP_Text reputationText = null;

        [SerializeField]
        [Tooltip("Shows the current day number, e.g. \"Day 3\".")]
        private TMP_Text dayText = null;

        [SerializeField]
        [Tooltip("Shows the in-game clock, e.g. \"09:15\".")]
        private TMP_Text clockText = null;

        [SerializeField]
        [Tooltip("Shows the current day phase, e.g. \"MORNING\".")]
        private TMP_Text phaseText = null;

        private void OnEnable()
        {
            EventBus.Subscribe<BalanceChangedEvent>(OnBalanceChanged);
            EventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
            EventBus.Subscribe<GameMinuteChangedEvent>(OnGameMinuteChanged);
            RefreshFromManagers();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BalanceChangedEvent>(OnBalanceChanged);
            EventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
            EventBus.Unsubscribe<GameMinuteChangedEvent>(OnGameMinuteChanged);
        }

        // OnEnable can run before the manager singletons' Awake; re-pull once they all exist so the
        // initial display is correct even if no change event has fired yet.
        private void Start()
        {
            RefreshFromManagers();
        }

        private void OnBalanceChanged(BalanceChangedEvent eventData)
        {
            SetMoney(eventData.NewBalance);
        }

        private void OnReputationChanged(ReputationChangedEvent eventData)
        {
            SetReputation(eventData.NewReputation);
        }

        private void OnDayStarted(DayStartedEvent eventData)
        {
            SetDay(eventData.DayNumber);
        }

        private void OnDayPhaseChanged(DayPhaseChangedEvent eventData)
        {
            SetPhase(eventData.CurrentPhase);
        }

        private void OnGameMinuteChanged(GameMinuteChangedEvent eventData)
        {
            SetText(clockText, TimeFormatUtils.FormatClock(eventData.Hour, eventData.Minute));
        }

        private void RefreshFromManagers()
        {
            if (EconomyManager.Instance != null)
            {
                SetMoney(EconomyManager.Instance.Balance);
            }

            if (ReputationManager.Instance != null)
            {
                SetReputation(ReputationManager.Instance.CurrentReputation);
            }

            if (DayManager.Instance != null)
            {
                SetDay(DayManager.Instance.CurrentDay);
                SetPhase(DayManager.Instance.CurrentPhase);
                SetText(clockText, TimeFormatUtils.FormatTimeOfDay(DayManager.Instance.CurrentTime));
            }
        }

        private void SetMoney(float balance)
        {
            SetText(moneyText, MoneyFormatUtils.Format(balance));
        }

        private void SetReputation(float reputation)
        {
            SetText(reputationText, $"{reputation:0}/100");
        }

        private void SetDay(int day)
        {
            SetText(dayText, $"Day {day}");
        }

        private void SetPhase(DayPhase phase)
        {
            SetText(phaseText, phase.ToString());
        }

        private void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
