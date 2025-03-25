using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Misaki.HighPerformance.Buffer
{
    public class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Func<T> _factory;
        private readonly ConcurrentQueue<T> _objects = new();

        private readonly bool _autoCleanup;
        private readonly int _autoCleanupInterval;

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

        public ObjectPool(Func<T> factory, uint initialSize = uint.MinValue, uint maxSize = uint.MaxValue, bool autoCleanup = false, int autoCleanupInterval = 1000 * 60 * 5)
        {
            _factory = factory;

            _autoCleanup = autoCleanup;
            _autoCleanupInterval = autoCleanupInterval;

            InitialSize = initialSize;
            MaxSize = maxSize;

            if (initialSize != uint.MinValue)
            {
                for (var i = 0; i < initialSize; i++)
                {
                    _objects.Enqueue(_factory());
                }
            }

            SetupAutoCleanup();
        }

        private void PoolCleanup()
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

        private void SetupAutoCleanup()
        {
            if (!_autoCleanup)
            {
                return;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(_autoCleanupInterval);
                    PoolCleanup();
                }
            });
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

        public void Dispose()
        {
            PoolCleanup();

            _disposed = true;
        }
    }
}
