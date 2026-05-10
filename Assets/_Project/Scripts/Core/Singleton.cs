using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Generic singleton base class for persistent manager systems.
    /// Inherit from this to get a scene-persistent, globally accessible instance.
    /// Usage: public class MyManager : Singleton&lt;MyManager&gt;
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = (T)this;
                DontDestroyOnLoad(gameObject);
                OnInitialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called once after the singleton instance is established.
        /// Override in subclasses to run setup logic instead of using Awake.
        /// </summary>
        protected virtual void OnInitialize() { }

        // Clears the static Instance when this singleton is destroyed (including on play-mode stop).
        // Prevents a stale reference from carrying into the next play session when "Enter Play Mode
        // Options" disables domain reload for fast iteration — without this, the next session's
        // Awake would see Instance != null and destroy the new GameObject, silently leaving the
        // game with no singleton instance.
        // The Instance == this guard ensures duplicate GameObjects (which destroy themselves in
        // Awake) don't accidentally clear the real instance's reference on their way out.
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
