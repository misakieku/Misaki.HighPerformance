using Misaki.HighPerformance.LowLevel.Buffer;

namespace Misaki.HighPerformance.LowLevel.Collections.Contracts;

public unsafe interface IUnsafeCollection : IDisposable
{
    /// <summary>
    /// Indicates whether the object has been created. Returns true if the object is created, otherwise false.
    /// </summary>
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

public unsafe interface IUnsafeCollection<T> : IUnsafeCollection, IEnumerable<T>
    where T : unmanaged
{
    /// <summary>
    /// Gets the number of elements in a collection. The value is read-only.
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

public unsafe interface IUnTypedCollection : IUnsafeCollection
{
    /// <summary>
    /// The total size of the buffer in bytes.
    /// </summary>
    uint Size
    {
        get;
    }

    ref T GetElementAt<T>(uint index)
        where T : unmanaged;
}