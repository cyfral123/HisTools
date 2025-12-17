#nullable enable
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

    public static Option<T> Some(T value)
    {
        return value is null ? throw new ArgumentNullException(nameof(value)) : new Option<T>(value);
    }

    public static Option<T> None() => default;

    public static Option<T> FromNullable(T value) => value != null ? Some(value) : None();

    public T Unwrap() => IsSome ? _value : throw new InvalidOperationException("Called Unwrap on None option");

    public T UnwrapOr(T defaultValue) => IsSome ? _value : defaultValue;
    
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

    public Option<TResult> Map<TResult>(Func<T, TResult> map)
    {
        if (map is null) throw new ArgumentNullException(nameof(map));

        return IsSome
            ? Option<TResult>.Some(map(_value))
            : Option<TResult>.None();
    }

    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind)
    {
        if (bind is null) throw new ArgumentNullException(nameof(bind));

        return IsSome
            ? bind(_value)
            : Option<TResult>.None();
    }

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        if (some is null) throw new ArgumentNullException(nameof(some));
        if (none is null) throw new ArgumentNullException(nameof(none));

        return IsSome ? some(_value) : none();
    }

    public void Match(Action<T> some, Action none)
    {
        if (some is null) throw new ArgumentNullException(nameof(some));
        if (none is null) throw new ArgumentNullException(nameof(none));

        if (IsSome) some(_value);
        else none();
    }
    
    public Option<T> IfSome(Action<T> action)
    {
        if (IsSome) action(_value);
        return this;
    }
    
    public Option<T> IfNone(Action action)
    {
        if (IsNone) action();
        return this;
    }
}

public static class Option
{
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    public static Option<T> None<T>() => Option<T>.None();

    public static Option<T> FromNullable<T>(T? value) where T : class => value is null ? None<T>() : Some(value);
}