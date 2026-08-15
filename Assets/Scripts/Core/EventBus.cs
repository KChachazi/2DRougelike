using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 按事件类型分发的进程内静态事件总线，用于跨模块通知。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();
        /// <summary>
        /// 对事件类型 T 进行订阅。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                handlers[typeof(T)] = (Action<T>)existing + handler;
            else
                handlers[typeof(T)] = handler;
        }
        /// <summary>
        /// 取消对事件类型 T 的订阅。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!handlers.TryGetValue(typeof(T), out Delegate existing)) return ;
            Action<T> updated = (Action<T>)existing - handler;
            if (updated == null)
                handlers.Remove(typeof(T));
            else 
                handlers[typeof(T)] = updated;
        }
        /// <summary>
        /// 发布事件以通知所有订阅该事件的模块。
        /// </summary>
        public static void Publish<T>(T evt)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                ((Action<T>)existing)?.Invoke(evt);
        }
        /// <summary>
        /// 清空订阅。
        /// </summary>
        public static void Clear() => handlers.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => handlers.Clear();
    }
}