using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PawsAndCare.Core;
using PawsAndCare.Economy;
using PawsAndCare.Workers;

namespace PawsAndCare.UI
{
    /// <summary>
    /// The hire panel: lists the StaffDirector's hire options, one HireCandidate each, and hires on
    /// click. Candidates are built once from the (static) option list; affordability is refreshed each
    /// time the panel opens and whenever the balance changes while it is open — event-driven, no
    /// per-frame work.
    /// </summary>
    public class HireScreen : UIPanel
    {
        [SerializeField]
        [Tooltip("The staff system this panel drives (reads HireOptions, calls TryHire).")]
        private StaffDirector staffDirector = null;

        [SerializeField]
        [Tooltip("Candidate prefab instantiated once per hire option. Needs a HireCandidate component.")]
        private HireCandidate candidatePrefab = null;

        [SerializeField]
        [Tooltip("Parent the candidates are spawned under — usually a VerticalLayoutGroup.")]
        private Transform candidateContainer = null;

        [SerializeField]
        [Tooltip("Optional close button; wired to close this panel.")]
        private Button closeButton = null;

        private List<HireCandidate> candidates;
        private bool candidatesBuilt;

        protected override void Awake()
        {
            base.Awake();

            candidates = new List<HireCandidate>();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        protected override void OnShow()
        {
            if (!candidatesBuilt)
            {
                BuildCandidates();
            }

            RefreshAffordability(EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0.0f);
            EventBus.Subscribe<BalanceChangedEvent>(OnBalanceChanged);
        }

        protected override void OnHide()
        {
            EventBus.Unsubscribe<BalanceChangedEvent>(OnBalanceChanged);
        }

        // Direct unsubscribe on top of base.OnDestroy: if the UIManager is already gone at teardown,
        // ClosePanel → Hide → OnHide never runs, and the subscription would dangle. Unsubscribing an
        // already-removed handler is a no-op, so the usual path stays harmless.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus.Unsubscribe<BalanceChangedEvent>(OnBalanceChanged);
        }

        private void BuildCandidates()
        {
            if (staffDirector == null || candidatePrefab == null || candidateContainer == null)
            {
                Debug.LogError("[HireScreen] staffDirector, candidatePrefab, or candidateContainer is missing — assign them in the inspector.", this);
            }
            else
            {
                IReadOnlyList<StaffDirector.HireOption> options = staffDirector.HireOptions;

                for (int i = 0; i < options.Count; i++)
                {
                    // Invalid options (mis-authored prefab, already logged by StaffDirector) get no
                    // row — TryHire refuses them anyway.
                    if (options[i].IsValid)
                    {
                        HireCandidate candidate = Instantiate(candidatePrefab, candidateContainer);
                        candidate.Setup(i, options[i].Role, options[i].HireCost, OnHireRequested);
                        candidates.Add(candidate);
                    }
                }

                candidatesBuilt = true;
            }
        }

        private void OnHireRequested(int index)
        {
            // Affordability updates itself: a successful hire raises an expense → BalanceChangedEvent.
            staffDirector.TryHire(index);
        }

        private void OnBalanceChanged(BalanceChangedEvent eventData)
        {
            RefreshAffordability(eventData.NewBalance);
        }

        // Null-guarded (not just error-logged in BuildCandidates) because this also runs from
        // BalanceChangedEvent while the panel is open. Rows and options are not index-aligned —
        // invalid options get no row — so each row is matched back through its OptionIndex.
        private void RefreshAffordability(float balance)
        {
            if (staffDirector != null)
            {
                IReadOnlyList<StaffDirector.HireOption> options = staffDirector.HireOptions;

                for (int i = 0; i < candidates.Count; i++)
                {
                    candidates[i].SetAffordable(options[candidates[i].OptionIndex].HireCost <= balance);
                }
            }
        }
    }
}
