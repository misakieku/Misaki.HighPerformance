using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel;

public readonly struct FunctionPointer<T>
    where T : Delegate
{
    private readonly nint _ptr;
    private readonly T _delegate;

    /// <summary>
    /// Gets the native function pointer associated with this function pointer instance.
    /// </summary>
    public readonly nint Pointer => _ptr;

    /// <summary>
    /// Gets the delegate instance associated with the specified function pointer.
    /// </summary>
    /// <remarks>This property uses <see
    /// cref="Marshal.GetDelegateForFunctionPointer{TDelegate}"/> to convert the function
    /// pointer to a delegate. Ensure that the function pointer is valid and compatible with the delegate type
    /// <typeparamref name="T"/>.</remarks>
    public T Invoke => _delegate;

    /// <summary>
    /// Creates a new instance of this function pointer with the following native pointer.
    /// </summary>
    /// <param name="ptr"></param>
    public FunctionPointer(nint ptr)
    {
        _ptr = ptr;
        _delegate = Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}