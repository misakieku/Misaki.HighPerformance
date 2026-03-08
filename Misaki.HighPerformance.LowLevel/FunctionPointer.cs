using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel;

/// <summary>
/// A structure that encapsulates a function pointer and provides methods to convert between
/// </summary>
/// <remarks>
/// This structure used marshalling to convert between function pointers and delegates, which is not ideal for high-performance scenarios.
/// Use this only when necessary, and prefer using <c>delegate* unmanaged</c> for better performance.
/// </remarks>
/// <typeparam name="T">The delegate type that the function pointer represents.</typeparam>
public readonly struct FunctionPointer<T>
    where T : Delegate
{
    private readonly nint _ptr;

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
    public T Delegate => Marshal.GetDelegateForFunctionPointer<T>(_ptr);

    /// <summary>
    /// Creates a new instance of this function pointer with the following native pointer.
    /// </summary>
    /// <param name="ptr"></param>
    public FunctionPointer(T func)
    {
        _ptr = Marshal.GetFunctionPointerForDelegate<T>(func);
    }
}