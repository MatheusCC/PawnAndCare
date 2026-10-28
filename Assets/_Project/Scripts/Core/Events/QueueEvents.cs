using PawsAndCare.Pets;
using PawsAndCare.Services;

namespace PawsAndCare.Core
{
    // Published by ReceptionQueue when a pet takes a queue slot. Reputation + future UI consume this.
    public readonly struct PetEnqueuedEvent
    {
        private readonly PetStateMachine pet;
        private readonly int slotIndex;

        public PetStateMachine Pet
        {
            get { return pet; }
        }

        public int SlotIndex
        {
            get { return slotIndex; }
        }

        public PetEnqueuedEvent(PetStateMachine pet, int slotIndex)
        {
            this.pet = pet;
            this.slotIndex = slotIndex;
        }
    }

    // Published by ReceptionQueue when a pet leaves the line — pulled into service or timed out.
    public readonly struct PetLeftQueueEvent
    {
        private readonly PetStateMachine pet;
        private readonly QueueLeaveReason reason;

        public PetStateMachine Pet
        {
            get { return pet; }
        }

        public QueueLeaveReason Reason
        {
            get { return reason; }
        }

        public PetLeftQueueEvent(PetStateMachine pet, QueueLeaveReason reason)
        {
            this.pet = pet;
            this.reason = reason;
        }
    }
}
