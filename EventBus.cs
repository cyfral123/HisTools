using System;
using System.Collections.Generic;

namespace HisTools;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> Subscribers = [];

    public static void Subscribe<T>(Action<T> callback)
    {
        if (!Subscribers.TryGetValue(typeof(T), out var list))
            Subscribers[typeof(T)] = list = [];

        list.Add(callback);
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        if (Subscribers.TryGetValue(typeof(T), out var list))
            list.Remove(callback);
    }

    public static void Publish<T>(T eventData)
    {
        if (!Subscribers.TryGetValue(typeof(T), out var list)) return;

        foreach (var callback in list)
            ((Action<T>)callback)?.Invoke(eventData);
    }
}