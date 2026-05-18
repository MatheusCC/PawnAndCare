namespace PawsAndCare.Interaction
{
    /// <summary>
    /// Represents an object that can be interacted with (hover, select, deselect).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The type of interactable this is (Station, Furniture, Floor).
        /// </summary>
        InteractableType Type
        {
            get;
        }

        /// <summary>
        /// Returns true if this interactable can currently be interacted with.
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// Called when the pointer enters hover range of this interactable.
        /// </summary>
        void OnHoverEnter();

        /// <summary>
        /// Called when the pointer exits hover range of this interactable.
        /// </summary>
        void OnHoverExit();

        /// <summary>
        /// Called when the player selects this interactable (e.g., left-click).
        /// </summary>
        void OnSelect();

        /// <summary>
        /// Called when the player deselects this interactable.
        /// </summary>
        void OnDeselect();
    }
}
