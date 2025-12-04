using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HisTools.Utils;

public static class Debounce
{
    private static readonly Dictionary<string, Coroutine> Running = [];

    public static void Run(MonoBehaviour runner, string key, float delaySeconds, Action action)
    {
        if (Running.TryGetValue(key, out var c))
        {
            runner.StopCoroutine(c);
        }

        var coroutine = runner.StartCoroutine(DebounceRoutine(key, delaySeconds, action));
        Running[key] = coroutine;
    }

    private static IEnumerator DebounceRoutine(string key, float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
        Running.Remove(key);
    }
}