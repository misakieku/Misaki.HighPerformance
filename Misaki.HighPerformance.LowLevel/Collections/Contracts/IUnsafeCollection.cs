namespace Misaki.HighPerformance.LowLevel.Collections.Contracts;

public unsafe interface IUnsafeCollection<T> : IEnumerable<T>, IDisposable
    where T : unmanaged
{
    /// <summary>
    /// Gets the number of elements in a collection. The value is read-only.
    /// </summary>
    public int Count
    {
        get;
    }

    /// <summary>
    /// Indicates whether the object has been created. Returns true if the object is created, otherwise false.
    /// </summary>
    public bool IsCreated
    {
        get;
    }

    /// <summary>
    /// Removes all elements from the collection. The collection will be empty after this operation.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Changes the size of a collection or array to the specified value.
    /// </summary>
    /// <param name="newSize">Specifies the new size to which the collection or array should be adjusted.</param>
    public void Resize(int newSize);

    /// <summary>
    /// Returns a pointer to an unmanaged memory location. This pointer can be used for low-level memory operations.
    /// </summary>
    /// <returns>The method returns a void pointer to the unsafe memory location.</returns>
    public void* GetUnsafePtr();
}