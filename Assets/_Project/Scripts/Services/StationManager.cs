using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;

namespace PawsAndCare.Services
{
    /// <summary>
    /// Tracks every live ServiceStation in the scene and answers availability queries.
    /// Stations self-register on Start and unregister on destroy.
    /// </summary>
    public class StationManager : Singleton<StationManager>
    {
        private List<ServiceStation> registeredStations;

        protected override void OnInitialize()
        {
            registeredStations = new List<ServiceStation>();
        }

        /// <summary>
        /// Adds a station to the registry. Called by ServiceStation.Start.
        /// </summary>
        public void Register(ServiceStation station)
        {
            if (station != null && !registeredStations.Contains(station))
            {
                registeredStations.Add(station);
            }
        }

        /// <summary>
        /// Removes a station from the registry. Called by ServiceStation.OnDestroy.
        /// </summary>
        public void Unregister(ServiceStation station)
        {
            if (station != null)
            {
                registeredStations.Remove(station);
            }
        }

        /// <summary>
        /// Returns the first unoccupied station that supports the given service type, or null if none.
        /// </summary>
        public ServiceStation GetAvailableStation(ServiceType type)
        {
            // Linear scan: station count in Phase 1 is tiny (≤ ~10), so a per-type cached lookup
            // would add complexity without measurable benefit. Revisit if profiling shows hot spot.
            ServiceStation result = null;

            for (int i = 0; i < registeredStations.Count; i++)
            {
                if (result == null)
                {
                    ServiceStation candidate = registeredStations[i];
                    if (!candidate.IsOccupied && candidate.Supports(type))
                    {
                        result = candidate;
                    }
                }
            }

            return result;
        }
    }
}
