using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.LowLevel;

/// <summary>
/// Represents a strongly-typed, read-only pointer to an unmanaged value of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// When a pointer is wrapped in this struct, it indicates that the code does not intend to manage the lifetime of the data being pointed to.
/// </remarks>
/// <typeparam name="T">The unmanaged type to which the pointer refers.</typeparam>
public readonly unsafe struct SharedPtr<T> : IEquatable<SharedPtr<T>>
    where T : unmanaged
{
    private readonly T* _value;

    public SharedPtr(T* value)
    {
        _value = value;
    }

    public T* Get()
    {
        return _value;
    }

    public ref T GetRef()
    {
        return ref *_value;
    }

    public bool Equals(SharedPtr<T> other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SharedPtr<T> ptr && Equals(ptr);
    }

    public override int GetHashCode()
    {
        return ((nint)_value).GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(SharedPtr<T> ptr)
    {
        return ptr._value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SharedPtr<T>(T* value)
    {
        return new SharedPtr<T>(value);
    }

    public static bool operator ==(SharedPtr<T> left, SharedPtr<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SharedPtr<T> left, SharedPtr<T> right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Provides exclusive ownership and management of an unmanaged pointer to a value of type <typeparamref name="T"/>.
///     Ensures that the pointer is not shared and can be safely transferred or detached.
/// </summary>
/// <remarks>
/// <see cref="UniquePtr{T}"/> is designed to encapsulate a raw pointer, enforcing unique ownership semantics similar to C++'s std::unique_ptr.
/// </remarks>
/// <typeparam name="T">The unmanaged type of the value to which the pointer refers.</typeparam>
[NonCopyable]
public unsafe struct UniquePtr<T> : IEquatable<UniquePtr<T>>
    where T : unmanaged
{
    private T* _value;

    public UniquePtr(T* value)
    {
        _value = value;
    }

    public readonly T* Get()
    {
        return _value;
    }

    public ref T GetRef()
    {
        return ref *_value;
    }

    public readonly SharedPtr<T> Share()
    {
        return new SharedPtr<T>(_value);
    }

    public void Attach(T* value)
    {
        Debug.Assert(_value == null);
        _value = value;
    }

    public T* Detach()
    {
        var temp = _value;
        _value = null;
        return temp;
    }

    public readonly bool Equals(UniquePtr<T> other)
    {
        return _value == other._value;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is SharedPtr<T> ptr && Equals(ptr);
    }

    public override readonly int GetHashCode()
    {
        return ((nint)_value).GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(UniquePtr<T> ptr)
    {
        return ptr._value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator UniquePtr<T>(T* value)
    {
        return new UniquePtr<T>(value);
    }

    public static bool operator ==(UniquePtr<T> left, UniquePtr<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UniquePtr<T> left, UniquePtr<T> right)
    {
        return !(left == right);
    }
}

[NonCopyable]
public unsafe struct DisposablePtr<T> : IDisposable, IEquatable<DisposablePtr<T>>
    where T : unmanaged, IDisposable
{
    private T* _value;

    public DisposablePtr(T* value)
    {
        _value = value;
    }

    public readonly T* Get()
    {
        return _value;
    }

    public ref T GetRef()
    {
        return ref *_value;
    }

    public readonly SharedPtr<T> Share()
    {
        return new SharedPtr<T>(_value);
    }

    public void Dispose()
    {
        if (_value != null)
        {
            _value->Dispose();
            _value = null;
        }
    }

    public readonly bool Equals(DisposablePtr<T> other)
    {
        return other._value == _value;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is DisposablePtr<T> ptr && Equals(ptr);
    }

    public override readonly int GetHashCode()
    {
        return ((nint)_value).GetHashCode();
    }

    public static bool operator ==(DisposablePtr<T> left, DisposablePtr<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DisposablePtr<T> left, DisposablePtr<T> right)
    {
        return !(left == right);
    }
}

public ref struct Ref<T> : IEquatable<Ref<T>>
{
    private ref T _value;

    public Ref(ref T value)
    {
        _value = ref value;
    }

    public ref T Get()
    {
        return ref _value;
    }

    public bool Equals(Ref<T> other)
    {
        return Unsafe.AreSame(ref _value, ref other._value);
    }

    [Obsolete("Equals() on Ref will always throw an exception. Use the equality operator instead.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override bool Equals(object? obj)
    {
        throw new NotSupportedException();
    }

    [Obsolete("GetHashCode() on Ref will always throw an exception.")]
    public override int GetHashCode()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new NotSupportedException();
    }

    public static bool operator ==(Ref<T> left, Ref<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Ref<T> left, Ref<T> right)
    {
        return !(left == right);
    }
}