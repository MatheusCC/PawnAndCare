using System.Collections.Generic;
using UnityEngine;
using PawsAndCare.Core;
using PawsAndCare.Pets;

namespace PawsAndCare.Services
{
    /// <summary>
    /// Ordered waiting line for arriving pets. Assigns each pet a physical slot, ticks its
    /// patience while it waits, advances the line as pets leave (dispatched into service or
    /// timed out), and answers front-of-queue queries for ServiceDispatcher (Task 5).
    /// Owns the queue-wait patience so PetStateMachine stays primitive-driven and decoupled.
    /// </summary>
    public class ReceptionQueue : Singleton<ReceptionQueue>
    {
        [SerializeField]
        [Tooltip("Ordered front→back queue positions. Must sit on the NavMesh. Capacity = slot count.")]
        private Transform[] queueSlots = null;

        private List<QueuedPet> waitingPets;

        protected override void OnInitialize()
        {
            waitingPets = new List<QueuedPet>();
        }

        private void Update()
        {
            PetStateMachine timedOutPet = null;

            for (int i = 0; i < waitingPets.Count; i++)
            {
                QueuedPet entry = waitingPets[i];

                // Only count waiting time once the pet has parked at its slot — pets still
                // walking up the line (ARRIVING) aren't penalised for travel time.
                if (entry.Pet.CurrentState == PetState.QUEUING)
                {
                    entry.WaitTimer += Time.deltaTime;

                    if (timedOutPet == null && entry.WaitTimer >= entry.Pet.Patience)
                    {
                        timedOutPet = entry.Pet;
                    }
                }
            }

            if (timedOutPet != null)
            {
                // Out of patience — leave unhappy. Remove advances the rest; reputation
                // penalty hooks in at a later milestone.
                Remove(timedOutPet, QueueLeaveReason.ABANDONED);
                timedOutPet.LeaveFacility();
            }
        }

        /// <summary>
        /// Adds a pet to the back of the line and sends it to that slot. Returns false when the
        /// queue is full, so the caller can turn the pet away.
        /// </summary>
        public bool TryEnqueue(PetStateMachine pet)
        {
            bool enqueued = false;

            if (pet != null && !Contains(pet) && HasFreeSlot())
            {
                int slotIndex = waitingPets.Count;
                waitingPets.Add(new QueuedPet(pet));
                pet.SendToQueueSlot(queueSlots[slotIndex].position);
                EventBus.Publish(new PetEnqueuedEvent(pet, slotIndex));
                enqueued = true;
            }

            return enqueued;
        }

        /// <summary>
        /// Removes a pet (dispatched into service or left on timeout) and shifts everyone
        /// behind it one slot forward.
        /// </summary>
        public void Remove(PetStateMachine pet, QueueLeaveReason reason)
        {
            int index = IndexOf(pet);

            if (index >= 0)
            {
                waitingPets.RemoveAt(index);
                AdvanceWaitingPets();
                EventBus.Publish(new PetLeftQueueEvent(pet, reason));
            }
        }

        /// <summary>
        /// Front-to-back scan for the dispatcher: first waiting pet wanting this service, or null.
        /// </summary>
        public PetStateMachine PeekNextForService(ServiceType type)
        {
            PetStateMachine result = null;

            for (int i = 0; i < waitingPets.Count; i++)
            {
                if (waitingPets[i].Pet.DesiredService == type)
                {
                    result = waitingPets[i].Pet;
                    break;
                }
            }

            return result;
        }

        private void AdvanceWaitingPets()
        {
            for (int i = 0; i < waitingPets.Count; i++)
            {
                waitingPets[i].Pet.MoveToQueueSlot(queueSlots[i].position);
            }
        }

        private bool Contains(PetStateMachine pet)
        {
            bool found = false;

            for (int i = 0; i < waitingPets.Count; i++)
            {
                if (waitingPets[i].Pet == pet)
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private int IndexOf(PetStateMachine pet)
        {
            int index = -1;

            for (int i = 0; i < waitingPets.Count; i++)
            {
                if (waitingPets[i].Pet == pet)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        private bool HasFreeSlot()
        {
            bool hasFree = queueSlots != null && waitingPets.Count < queueSlots.Length;

            return hasFree;
        }

        // Pairs a queued pet with how long it has waited at its slot. Patience value lives on
        // the pet (PetDefinition); the queue only tracks elapsed wait time.
        private class QueuedPet
        {
            public PetStateMachine Pet { get; }
            public float WaitTimer { get; set; }

            public QueuedPet(PetStateMachine pet)
            {
                Pet = pet;
                WaitTimer = 0.0f;
            }
        }
    }
}
