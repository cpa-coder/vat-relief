using System;
using System.Diagnostics.Contracts;

namespace DebitExpress.VatRelief;

public enum ResultState : byte
{
    Faulted,
    Success
}

public readonly struct Result
{
    private readonly ResultState _state;
    private readonly Exception? _exception;

    /// <summary>
    ///     Constructor of a concrete value
    /// </summary>
    public Result()
    {
        _state = ResultState.Success;
        _exception = null;
    }

    /// <summary>
    ///     Constructor of an error value
    /// </summary>
    /// <param name="e"></param>
    public Result(Exception e)
    {
        _state = ResultState.Faulted;
        _exception = e;
    }

    private Result(ResultState state)
    {
        _state = state;
        _exception = null;
    }

    /// <summary>
    ///     True if the result is faulted
    /// </summary>
    [Pure]
    public bool IsFaulted => _state == ResultState.Faulted;

    /// <summary>
    ///     True if the struct is in an success
    /// </summary>
    [Pure]
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>
    ///     Returns a faulted result
    /// </summary>
    public static Result Faulted => new(ResultState.Faulted);

    [Pure]
    public override string ToString() =>
        IsFaulted
            ? _exception?.Message ?? "Faulted"
            : "Success";
}

public readonly struct Result<T>
{
    private readonly ResultState _state;
    private readonly Exception? _exception;
    private readonly T? _value;

    /// <summary>
    ///     Constructor of a concrete value
    /// </summary>
    /// <param name="value"></param>
    public Result(T value)
    {
        _state = ResultState.Success;
        _value = value;
        _exception = null;
    }

    /// <summary>
    ///     Constructor of an error value
    /// </summary>
    /// <param name="e"></param>
    public Result(Exception e)
    {
        _state = ResultState.Faulted;
        _exception = e;
        _value = default;
    }

    private Result(ResultState state)
    {
        _state = state;
        _exception = null;
        _value = default;
    }

    /// <summary>
    ///     Implicit conversion operator from T to Result<T>
    /// </summary>
    /// <param name="value">Value</param>
    [Pure]
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    ///     Implicit conversion operator from Result<T> to T
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    [Pure]
    public static implicit operator T(Result<T> result) => result._value ?? throw new NullReferenceException();

    /// <summary>
    ///     Returns the value if the result is success.<br />
    ///     <br />
    ///     WARNING!!: If the result is faulted, this will throw an exception.
    ///     Use only when implicit conversion is not possible.
    /// </summary>
    /// <exception cref="NullReferenceException">Throws when the result is faulted</exception>
    public T Value => _value ?? throw new NullReferenceException();

    /// <summary>
    ///     True if the result is faulted
    /// </summary>
    [Pure]
    public bool IsFaulted => _state == ResultState.Faulted;

    /// <summary>
    ///     True if the struct is in an success
    /// </summary>
    [Pure]
    public bool IsSuccess => _state == ResultState.Success;

    /// <summary>
    ///     Returns a faulted result
    /// </summary>
    public static Result<T> Faulted => new(ResultState.Faulted);

    [Pure]
    public override string ToString() =>
        IsFaulted
            ? _exception?.Message ?? "Faulted"
            : _value?.ToString() ?? "Success";
}