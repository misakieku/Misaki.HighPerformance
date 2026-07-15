using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.Buffer
{
    public class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetAction;
        private readonly Queue<T> _pool = new();

        private bool _disposed;

        /// <summary>
        /// Gets the initial size of the object pool, which indicates how many objects were pre-created and added to the pool upon initialization.
        /// </summary>
        public int InitialSize
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the ObjectPool class with the specified factory function, reset action, and initial size.
        /// </summary>
        /// <param name="factory">The factory function used to create new instances of the pooled object.</param>
        /// <param name="resetAction">The action to invoke when an object is returned to the pool.</param>
        /// <param name="initialSize">The initial number of objects to pre-create and add to the pool.</param>
        public ObjectPool(Func<T> factory, Action<T>? resetAction, int initialSize = 0)
        {
            _factory = factory;
            _resetAction = resetAction;

            InitialSize = initialSize;

            if (initialSize > 0)
            {
                for (var i = 0; i < initialSize; i++)
                {
                    _pool.Enqueue(_factory());
                }
            }
        }

        ~ObjectPool()
        {
            Dispose();
        }

        /// <summary>
        /// Rents an object from the pool. If the pool is empty, a new instance will be created using the factory function.
        /// </summary>
        /// <returns></returns>
        public T Rent()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pool.TryDequeue(out var obj))
            {
                return obj;
            }

            var newInstance = _factory();
            return newInstance;
        }

        /// <summary>
        /// Tries to rent an object from the pool without creating a new instance if the pool is empty.
        /// </summary>
        /// <param name="obj">The object rented from the pool, or null if the pool is empty.</param>
        /// <returns>true if an object was successfully rented; otherwise, false.</returns>
        public bool TryRent([MaybeNullWhen(false)] out T obj)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_pool.TryDequeue(out obj))
            {
                return true;
            }

            obj = null;
            return false;
        }

        /// <summary>
        /// Returns an object to the pool. The reset action will be invoked on the object before it is added back to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        public void Return(T obj)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _resetAction?.Invoke(obj);
            _pool.Enqueue(obj);
        }

        /// <summary>
        /// Resets the object pool by clearing all objects from the pool and invoking the reset action on each object before clearing.
        /// </summary>
        public void Reset()
        {
            foreach (var obj in _pool)
            {
                _resetAction?.Invoke(obj);
            }

            _pool.Clear();
            GC.Collect();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Reset();

            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
