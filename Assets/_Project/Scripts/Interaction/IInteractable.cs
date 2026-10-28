namespace PawsAndCare.Interaction
{
    // Represents an object that can be interacted with (hover, select, deselect).
    public interface IInteractable
    {
        // The type of interactable this is (Station, Furniture, Floor).
        InteractableType Type
        {
            get;
        }

        // Returns true if this interactable can currently be interacted with.
        bool CanInteract();

        // Called when the pointer enters hover range of this interactable.
        void OnHoverEnter();

        // Called when the pointer exits hover range of this interactable.
        void OnHoverExit();

        // Called when the player selects this interactable (e.g., left-click).
        void OnSelect();

        // Called when the player deselects this interactable.
        void OnDeselect();
    }
}
