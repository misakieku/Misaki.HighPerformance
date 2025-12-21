using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.Buffer
{
    public class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly ConcurrentQueue<T> _pool = new();

        private bool _disposed;

        public uint InitialSize
        {
            get;
        }

        public uint MaxSize
        {
            get;
        }

        public ObjectPool(Func<T> factory, uint initialSize = uint.MinValue, uint maxSize = uint.MaxValue)
        {
            _factory = factory;

            InitialSize = initialSize;
            MaxSize = maxSize;

            if (initialSize != uint.MinValue)
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

        public T Rent()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pool.TryDequeue(out var obj))
            {
                return obj;
            }

            var newInstance = _factory();
            _pool.Enqueue(newInstance);

            return newInstance;
        }

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

        public void Return(T obj)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_pool.Count < MaxSize)
            {
                _pool.Enqueue(obj);
            }
        }

        public void Reset()
        {
            foreach (var obj in _pool)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
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
