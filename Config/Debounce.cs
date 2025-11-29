using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Config;

public static class Debounce
{
    private static readonly Dictionary<string, Coroutine> s_running = [];

    public static void Run(MonoBehaviour runner, string key, float delaySeconds, Action action)
    {
        if (s_running.TryGetValue(key, out var c))
        {
            runner.StopCoroutine(c);
        }

        var coroutine = runner.StartCoroutine(DebounceRoutine(key, delaySeconds, action));
        s_running[key] = coroutine;
    }

    private static IEnumerator DebounceRoutine(string key, float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
        s_running.Remove(key);
    }
}