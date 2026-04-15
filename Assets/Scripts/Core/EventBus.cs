using System;
using System.Collections.Generic;

public static class EventBus
{
    
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();

        _handlers[type].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public static void Publish<T>(T eventData) where T : IEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        // ToArray để tránh lỗi khi modify list trong lúc invoke
        foreach (var handler in list.ToArray())
            (handler as Action<T>)?.Invoke(eventData);
    }

    public static void ClearAll() => _handlers.Clear();
}