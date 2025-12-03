using System;

namespace HisTools.Utils;

public readonly struct Option<T>
{
    private readonly T _value;
    public bool IsSome { get; }
    public bool IsNone => !IsSome;

    private Option(T value)
    {
        _value = value!;
        IsSome = true;
    }

    public static Option<T> Some(T value) => new(value);
    public static Option<T> None() => new();

    public static Option<T> FromNullable(T value) =>
        value != null ? Some(value) : None();

    public T Unwrap() =>
        IsSome ? _value : throw new InvalidOperationException("Called Unwrap on None option");

    public T UnwrapOr(T fallback) =>
        IsSome ? _value : fallback;
    
    public bool TryGet(out T value)
    {
        if (IsSome)
        {
            value = _value;
            return true;
        }

        value = default!;
        return false;
    }
}