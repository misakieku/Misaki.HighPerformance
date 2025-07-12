using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.LowLevel.Buffer;

/// <summary>
/// A dynamic memory management structure that automatically grows by creating linked arenas
/// when more space is needed.
/// </summary>
public unsafe struct DynamicArena : IDisposable
{
    private struct ArenaNode
    {
        public Arena arena;
        public ArenaNode* next;
    }

    private ArenaNode* _root;
    private ArenaNode* _current;
    private uint _initialSize;

    /// <summary>
    /// Initializes a new instance of DynamicArena with the specified initial size.
    /// </summary>
    /// <param name="initialSize">The initial size in bytes for the first arena block.</param>
    public DynamicArena(uint initialSize)
    {
        _initialSize = initialSize;
        _root = (ArenaNode*)Malloc(SizeOf<ArenaNode>());
        _root->arena = new Arena(initialSize);
        _root->next = null;
        _current = _root;
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

    private bool CreateNewNode(nuint size)
    {
        var newNode = (ArenaNode*)Malloc(SizeOf<ArenaNode>());
        try
        {
            newNode->arena = new Arena(size);
            newNode->next = null;

            _current->next = newNode;
            _current = newNode;
            return true;
        }
        catch
        {
            Free(newNode);
            return false;
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

            if (current->next == null && !CreateNewNode(Math.Max(size, _initialSize)))
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

    /// <summary>
    /// Disposes all arenas and frees associated memory.
    /// </summary>
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