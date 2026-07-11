using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                handlers[typeof(T)] = (Action<T>)existing + handler;
            else
                handlers[typeof(T)] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!handlers.TryGetValue(typeof(T), out Delegate existing)) return ;
            Action<T> updated = (Action<T>)existing - handler;
            if (updated == null)
                handlers.Remove(typeof(T));
            else 
                handlers[typeof(T)] = updated;
        }

        public static void Publish<T>(T evt)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                ((Action<T>)existing)?.Invoke(evt);
        }

        public static void Clear() => handlers.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => handlers.Clear();
    }
}