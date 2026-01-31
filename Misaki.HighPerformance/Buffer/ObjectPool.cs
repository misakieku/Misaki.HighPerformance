using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.Buffer
{
    public class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T>? _resetAction;
        private readonly ConcurrentQueue<T> _pool = new();

        private bool _disposed;

        public int InitialSize
        {
            get;
        }

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

            _resetAction?.Invoke(obj);
            _pool.Enqueue(obj);
        }

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
