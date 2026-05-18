using UnityEngine;
using PawsAndCare.Services;

namespace PawsAndCare.Workers
{
    /// <summary>
    /// Worker template definition: role, speed, cost, and skill ratings per service type.
    /// </summary>
    [CreateAssetMenu(fileName = "Worker_", menuName = "PawsAndCare/Workers/Worker Definition")]
    public class WorkerDefinition : ScriptableObject
    {
        [System.Serializable]
        public struct SkillRating
        {
            [SerializeField]
            public ServiceType serviceType;

            [SerializeField]
            [Range(0.0f, 1.0f)]
            public float rating;
        }

        [SerializeField]
        private WorkerRole role = WorkerRole.GENERALIST;

        [SerializeField]
        private float baseMoveSpeed = 5.0f;

        [SerializeField]
        private float baseServiceSpeed = 1.0f;

        [SerializeField]
        private SkillRating[] skillRatings = null;

        [SerializeField]
        private float hireCost = 100.0f;

        [SerializeField]
        private float dailySalary = 50.0f;

        public WorkerRole Role
        {
            get { return role; }
        }

        public float BaseMoveSpeed
        {
            get { return baseMoveSpeed; }
        }

        public float BaseServiceSpeed
        {
            get { return baseServiceSpeed; }
        }

        public float HireCost
        {
            get { return hireCost; }
        }

        public float DailySalary
        {
            get { return dailySalary; }
        }

        /// <summary>
        /// Returns the worker's skill rating for the given service type, clamped to [0,1].
        /// Returns 0 if no rating is defined for that service.
        /// </summary>
        public float GetSkillRating(ServiceType serviceType)
        {
            float rating = 0.0f;

            for (int i = 0; i < skillRatings.Length; i++)
            {
                if (skillRatings[i].serviceType == serviceType)
                {
                    rating = Mathf.Clamp01(skillRatings[i].rating);
                }
            }

            return rating;
        }
    }
}
