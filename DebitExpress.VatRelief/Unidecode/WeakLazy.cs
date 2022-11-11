using System;

namespace DebitExpress.VatRelief.Unidecode;

public abstract class WeakLazy<T> where T : class
{
    protected readonly Func<T> ValueFactory = null!;

    protected internal WeakLazy(Func<T>? valueFactory)
    {
        if (valueFactory != null) ValueFactory = valueFactory;
    }

    public abstract T Value { get; }
}

public sealed class StaticWeakLazy<T> : WeakLazy<T> where T : class
{
    private readonly WeakReference<T> _reference = null!;

    public StaticWeakLazy(Func<T>? valueFactory) : base(valueFactory)
    {
        if (valueFactory != null) _reference = new WeakReference<T>(null);
    }

    public override T Value
    {
        get
        {
            if (!_reference.TryGetTarget(out var value))
            {
                value = ValueFactory();
                _reference.SetTarget(value);
            }
            return value;
        }
    }
}