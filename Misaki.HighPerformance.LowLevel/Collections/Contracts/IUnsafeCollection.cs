using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.LowLevel.Collections.Contracts;

public unsafe interface IUnsafeCollection : IDisposable
{
    /// <summary>
    /// Indicates whether the object has been created. Returns true if the object is created, otherwise false.
    /// </summary>
    /// <remarks>
    /// If MHP_ENABLE_STACKTRACE is not defined, this property will only check if the underlying pointer is not null, which may not be sufficient to determine if the collection is fully initialized and ready for use.
    /// </remarks>
    bool IsCreated
    {
        get;
    }

    /// <summary>
    /// Removes all elements from the collection. The collection will be empty after this operation.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns a pointer to an unmanaged memory location. This pointer can be used for low-level memory operations.
    /// </summary>
    /// <returns>The method returns a void pointer to the unsafe memory location.</returns>
    void* GetUnsafePtr();
}

public interface IUnsafeCollection<T> : IUnsafeCollection
    where T : unmanaged
{
    /// <summary>
    /// Gets the number of elements in a collection.
    /// </summary>
    int Count
    {
        get;
    }

    /// <summary>
    /// Changes the size of a collection to the specified value.
    /// </summary>
    /// <remarks>This is to adjust the element count of the collection, not the size of the underlying buffer in memory.</remarks>
    /// <param name="newSize">Specifies the new size to which the collection should be adjusted.</param>
    /// <param name="option">Specifies allocation options that may affect how memory is managed during the resize operation.</param>
    void Resize(int newSize, AllocationOption option);
}

public interface IUnsafeHashCollection<T> : IDisposable
    where T : unmanaged
{
    /// <summary>
    /// Indicates whether the object has been created. Returns true if the object is created, otherwise false.
    /// </summary>
    bool IsCreated
    {
        get;
    }

    /// <summary>
    /// Gets the number of elements in a collection. The value is read-only.
    /// </summary>
    int Count
    {
        get;
    }

    /// <summary>
    /// Removes all elements from the collection. The collection will be empty after this operation.
    /// </summary>
    void Clear();

    /// <summary>
    /// Changes the size of a collection to the specified value.
    /// </summary>
    /// <remarks>This is to adjust the element count of the collection, not the size of the underlying buffer in memory.</remarks>
    /// <param name="newSize">Specifies the new size to which the collection should be adjusted.</param>
    /// <param name="option">Specifies allocation options that may affect how memory is managed during the resize operation.</param>
    void Resize(int newSize, AllocationOption option);
}