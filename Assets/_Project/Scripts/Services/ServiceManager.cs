using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Pets;
using PawsAndCare.Workers;

namespace PawsAndCare.Services
{
    /// <summary>
    /// Owns the lifecycle of active ServiceSessions: creation, per-frame progress,
    /// and completion broadcasting. Knows nothing about navigation or station occupancy —
    /// those belong to WorkerServiceRunner. This manager only advances session math.
    /// </summary>
    public class ServiceManager : Singleton<ServiceManager>
    {
        private List<ServiceSession> activeSessions;
        private int nextSessionId;

        protected override void OnInitialize()
        {
            activeSessions = new List<ServiceSession>();
            nextSessionId = 1;
        }

        /// <summary>
        /// Creates and begins tracking a new session. Publishes ServiceStartedEvent.
        /// Returns the created session, or null if any argument was invalid.
        /// </summary>
        public ServiceSession StartService(ServiceStation station, ServiceData service, Worker worker, PetStateMachine pet)
        {
            ServiceSession session = null;

            if (station != null && service != null && worker != null && pet != null)
            {
                session = new ServiceSession(nextSessionId, service, station, worker, pet);
                nextSessionId++;
                activeSessions.Add(session);
                EventBus.Publish(new ServiceStartedEvent(session));
            }
            else
            {
                Debug.LogError("[ServiceManager] StartService called with a null station, service, worker, or pet — no session created.", this);
            }

            return session;
        }

        private void Update()
        {
            // Iterate backward so completed sessions can be removed in place.
            for (int i = activeSessions.Count - 1; i >= 0; i--)
            {
                ServiceSession session = activeSessions[i];
                session.UpdateProgress(Time.deltaTime);

                if (session.Status == ServiceStatus.COMPLETED)
                {
                    EventBus.Publish(new ServiceCompletedEvent(session, session.Quality));
                    activeSessions.RemoveAt(i);
                }
            }
        }
    }
}
