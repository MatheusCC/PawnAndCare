using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Static publish/subscribe event bus. Decouples systems by letting them
    /// communicate through events without holding direct references.
    /// </summary>
    // Convention: event types are structs (zero-allocation publishing).
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Registers a handler to be called whenever an event of type T is published.
        /// Null handlers and duplicate subscriptions are silently ignored.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler != null)
            {
                Type eventType = typeof(T);

                if (!handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    list = new List<Delegate>();
                    handlers[eventType] = list;
                }

                if (!list.Contains(handler))
                {
                    list.Add(handler);
                }
            }
        }

        /// <summary>
        /// Removes a previously-registered handler. No-op if the handler was never subscribed.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler != null)
            {
                Type eventType = typeof(T);

                if (handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    list.Remove(handler);
                }
            }
        }

        /// <summary>
        /// Sends an event to all handlers subscribed to type T.
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            Type eventType = typeof(T);

            if (handlers.TryGetValue(eventType, out List<Delegate> list))
            {
                // Snapshot the list so a handler that calls Subscribe/Unsubscribe
                // during dispatch doesn't trigger "Collection modified" exceptions.
                Delegate[] snapshot = list.ToArray();

                for (int i = 0; i < snapshot.Length; i++)
                {
                    // Safe cast: only handlers added through Subscribe<T> end up
                    // in this drawer, so they're all Action<T>.
                    Action<T> typedHandler = (Action<T>)snapshot[i];

                    try
                    {
                        typedHandler.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        // Log and continue: one bad subscriber must not break the
                        // chain for everyone else listening to this event.
                        Debug.LogException(ex);
                    }
                }
            }
        }

        // Clears the handler dictionary on every Play start.
        // Required when "Enter Play Mode Options" disables domain reload for fast iteration:
        // without this, subscriptions from the previous play session persist as stale delegates
        // pointing to destroyed GameObjects, and the next Publish would NRE when invoking them.
        // Free insurance with domain reload enabled (the default); essential when it's disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHandlers()
        {
            handlers.Clear();
        }
    }
}
