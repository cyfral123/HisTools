using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> s_subscribers = [];

    public static void Subscribe<T>(Action<T> callback)
    {
        if (!s_subscribers.TryGetValue(typeof(T), out var list))
            s_subscribers[typeof(T)] = list = [];

        list.Add(callback);
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        if (s_subscribers.TryGetValue(typeof(T), out var list))
            list.Remove(callback);
    }

    public static void Publish<T>(T eventData)
    {
        if (s_subscribers.TryGetValue(typeof(T), out var list))
        {
            foreach (var callback in list)
                ((Action<T>)callback)?.Invoke(eventData);
        }
    }

    public static bool IsSubscribed<T>(Action<T> listener)
    {
        if (!s_subscribers.TryGetValue(typeof(T), out var list))
            return false;

        return list.Contains(listener);
    }
}
