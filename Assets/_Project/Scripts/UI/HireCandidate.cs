using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PawsAndCare.Core;
using PawsAndCare.Workers;

namespace PawsAndCare.UI
{
    /// <summary>
    /// One hireable candidate shown in the hire panel: its role and cost with a Hire button. Purely
    /// presentational — it reports its index back to the owning HireScreen on click and toggles its own
    /// affordability; it holds no economy or staff logic.
    /// </summary>
    public class HireCandidate : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Shows the candidate's role, e.g. \"GROOMER\".")]
        private TMP_Text roleLabel = null;

        [SerializeField]
        [Tooltip("Shows the one-time hire cost, e.g. \"$250\".")]
        private TMP_Text costLabel = null;

        [SerializeField]
        [Tooltip("Hire button — disabled while the candidate is unaffordable.")]
        private Button hireButton = null;

        private int optionIndex;
        private Action<int> onHire;

        /// <summary>
        /// Index of the StaffDirector hire option this candidate represents. The HireScreen uses it to
        /// look the option back up (rows and options are not index-aligned — invalid options get no row).
        /// </summary>
        public int OptionIndex
        {
            get { return optionIndex; }
        }

        /// <summary>
        /// Binds this candidate to a hire option. onHire is invoked with the option's index when the
        /// button is clicked. Safe to call again to rebind (listeners are reset first).
        /// </summary>
        public void Setup(int index, WorkerRole role, float hireCost, Action<int> onHire)
        {
            optionIndex = index;
            this.onHire = onHire;

            if (roleLabel != null)
            {
                roleLabel.text = role.ToString();
            }

            if (costLabel != null)
            {
                costLabel.text = MoneyFormatUtils.Format(hireCost);
            }

            if (hireButton != null)
            {
                hireButton.onClick.RemoveAllListeners();
                hireButton.onClick.AddListener(OnHireClicked);
            }
            else
            {
                Debug.LogError("[HireCandidate] hireButton is missing — assign it in the inspector.", this);
            }
        }

        /// <summary>Greys the button out when the player cannot afford this candidate.</summary>
        public void SetAffordable(bool affordable)
        {
            if (hireButton != null)
            {
                hireButton.interactable = affordable;
            }
        }

        private void OnHireClicked()
        {
            if (onHire != null)
            {
                onHire(optionIndex);
            }
        }
    }
}
