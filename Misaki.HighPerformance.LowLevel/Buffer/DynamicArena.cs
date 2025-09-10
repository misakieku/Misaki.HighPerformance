using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A dynamic memory management structure that automatically grows by creating linked arenas
/// when more space is needed.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 128)]
public unsafe struct DynamicArena : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ArenaNode
    {
        public Arena arena;
        public ArenaNode* next;
    }

    [FieldOffset(0)]
    private ArenaNode* _root;
    [FieldOffset(8)]
    private ArenaNode* _current;
    [FieldOffset(16)]
    private uint _initialSize;

    [FieldOffset(20)]
    private volatile int _nodeCreationLock;

    /// <summary>
    /// Initializes a new instance of DynamicArena with the specified initial size.
    /// </summary>
    /// <param name="initialSize">The initial size in bytes for the first arena block.</param>
    public DynamicArena(uint initialSize)
    {
        Initialize(initialSize);
    }

    public void Initialize(uint initialSize)
    {
        if (_root != null)
        {
            return;
        }

        _initialSize = initialSize;
        _root = (ArenaNode*)Malloc(SizeOf<ArenaNode>());
        _root->arena = new Arena(initialSize);
        _root->next = null;
        _current = _root;
    }

    private bool TryCreateNewNode(nuint size)
    {
        while (Interlocked.CompareExchange(ref _nodeCreationLock, 1, 0) != 0)
        {
            Thread.SpinWait(1);
        }

        try
        {
            var current = _current;
            if (current->next != null)
            {
                // Another thread created a node while we were waiting
                _current = current->next;
                return true;
            }

            var newNode = (ArenaNode*)Malloc(SizeOf<ArenaNode>());
            try
            {
                newNode->arena = new Arena(size);
                newNode->next = null;

                // Atomically link the new node
                current->next = newNode;

                // Update current pointer
                _current = newNode;
                return true;
            }
            catch
            {
                Free(newNode);
                return false;
            }
        }
        finally
        {
            // Release the spinlock
            Interlocked.Exchange(ref _nodeCreationLock, 0);
        }
    }

    /// <summary>
    /// Allocates a block of memory with specified size and alignment. Creates a new arena if current one is full.
    /// </summary>
    /// <param name="size">Size of the memory block to allocate in bytes.</param>
    /// <param name="alignment">Alignment requirement for the memory block.</param>
    /// <returns>Pointer to the allocated memory block.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void* Allocate(nuint size, nuint alignment, AllocationOption allocationOption)
    {
        if (_root == null)
        {
            return null;
        }

        void* result = null;
        var current = _current;

        while (current != null)
        {
            result = current->arena.Allocate(size, alignment, allocationOption);
            if (result != null)
            {
                return result;
            }

            if (current->next == null && !TryCreateNewNode(Math.Max(size, _initialSize)))
            {
                return null;
            }

            current = current->next;
        }

        _current = current;
        return result;
    }

    /// <summary>
    /// Resets all arenas in the chain, optionally clearing their memory.
    /// </summary>
    /// <param name="clear">If true, memory will be cleared during reset.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the arena has been disposed.</exception>
    public void Reset()
    {
        var current = _root;
        while (current != null)
        {
            current->arena.Reset();
            current = current->next;
        }

        _current = _root;
    }

    public void Dispose()
    {
        if (_root == null)
        {
            return;
        }

        var current = _root;
        while (current != null)
        {
            var next = current->next;
            current->arena.Dispose();
            Free(current);
            current = next;
        }

        _root = null;
        _current = null;
    }
}