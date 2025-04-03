using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.Buffer
{
    public class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Func<T> _factory;
        private readonly ConcurrentQueue<T> _objects = new();

        private bool _disposed;

        public uint InitialSize
        {
            get;
        }

        public uint MaxSize
        {
            get;
            private set;
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
                    _objects.Enqueue(_factory());
                }
            }
        }

        ~ObjectPool()
        {
            Dispose();
        }

        public bool TryRent([MaybeNullWhen(false)] out T obj)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_objects.IsEmpty)
            {
                return _objects.TryDequeue(out obj);
            }

            obj = null;
            return false;
        }

        public void Return(T obj)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_objects.Count < MaxSize)
            {
                _objects.Enqueue(obj);
            }
        }

        public void Reset()
        {
            foreach (var obj in _objects)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _objects.Clear();
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
