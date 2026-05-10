using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public unsafe class TestUnsafeChunkedList
{
    private UnsafeChunkedList<int>* _list;

    [TestInitialize]
    public void Initialize()
    {
        _list = (UnsafeChunkedList<int>*)NativeMemory.Alloc((nuint)sizeof(UnsafeChunkedList<int>));
        *_list = new UnsafeChunkedList<int>(3, AllocationHandle.Persistent);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_list->IsCreated)
        {
            _list->Dispose();
        }

        NativeMemory.Free(_list);
    }

    [TestMethod]
    public void TestAdd()
    {
        _list->Add(1);
        _list->Add(2);
        _list->Add(3);
        Assert.AreEqual(3, _list->Count);
        Assert.AreEqual(1, (*_list)[0]);
        Assert.AreEqual(2, (*_list)[1]);
        Assert.AreEqual(3, (*_list)[2]);
    }

    [TestMethod]
    public void TestAddMultiChunk()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        Assert.AreEqual(10, _list->Count);
        Assert.IsTrue(_list->Capacity >= 10);
        Assert.IsTrue(_list->ChunkCount >= 4);

        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(i, (*_list)[i]);
        }
    }

    [TestMethod]
    public void TestRemoveAt()
    {
        _list->Add(0);
        _list->Add(1);
        _list->Add(2);
        _list->RemoveAt(1);
        Assert.AreEqual(2, _list->Count);
        Assert.AreEqual(0, (*_list)[0]);
        Assert.AreEqual(2, (*_list)[1]);
    }

    [TestMethod]
    public void TestRemoveAtSwapBack()
    {
        _list->Add(10);
        _list->Add(11);
        _list->Add(12);
        _list->Add(13);

        _list->RemoveAtSwapBack(1);

        Assert.AreEqual(3, _list->Count, "Count should be 3");
        Assert.AreEqual(10, (*_list)[0], "Index 0 should be 10");
        Assert.AreEqual(13, (*_list)[1], "Index 1 should be 13");
        Assert.AreEqual(12, (*_list)[2], "Index 2 should be 12");
    }

    [TestMethod]
    public void TestRemoveAtAcrossChunks()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        _list->RemoveAt(3);

        Assert.AreEqual(6, _list->Count);
        Assert.AreEqual(0, (*_list)[0]);
        Assert.AreEqual(1, (*_list)[1]);
        Assert.AreEqual(2, (*_list)[2]);
        Assert.AreEqual(4, (*_list)[3]);
        Assert.AreEqual(5, (*_list)[4]);
        Assert.AreEqual(6, (*_list)[5]);
    }

    [TestMethod]
    public void TestRemoveAtSwapBackAcrossChunks()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        _list->RemoveAtSwapBack(1);

        Assert.AreEqual(6, _list->Count, "Count should be 6");
        Assert.AreEqual(0, (*_list)[0], "Index 0 should be 0");
        Assert.AreEqual(6, (*_list)[1], "Index 1 should be 6 (swapped from last)");
        Assert.AreEqual(2, (*_list)[2], "Index 2 should be 2");
        Assert.AreEqual(3, (*_list)[3], "Index 3 should be 3");
        Assert.AreEqual(4, (*_list)[4], "Index 4 should be 4");
        Assert.AreEqual(5, (*_list)[5], "Index 5 should be 5");
    }

    [TestMethod]
    public void TestClear()
    {
        _list->Add(1);
        _list->Add(2);
        _list->Clear();
        Assert.AreEqual(0, _list->Count);
        Assert.AreEqual(0, _list->ChunkCount);
    }

    [TestMethod]
    public void TestAddRange()
    {
        int[] values = { 10, 20, 30 };
        _list->AddRange(values);
        Assert.AreEqual(3, _list->Count);
        Assert.AreEqual(10, (*_list)[0]);
        Assert.AreEqual(20, (*_list)[1]);
        Assert.AreEqual(30, (*_list)[2]);
    }

    [TestMethod]
    public void TestAddRangeMultiChunk()
    {
        int[] values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        _list->AddRange(values);
        Assert.AreEqual(10, _list->Count);
        Assert.IsTrue(_list->ChunkCount >= 4);

        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(i, (*_list)[i]);
        }
    }

    [TestMethod]
    public void TestEnumerator()
    {
        _list->Add(1);
        _list->Add(2);
        _list->Add(3);
        var sum = 0;
        foreach (var item in *_list)
        {
            sum += item;
        }

        Assert.AreEqual(6, sum);
    }

    [TestMethod]
    public void TestEnumeratorMultiChunk()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        var sum = 0;
        foreach (var item in *_list)
        {
            sum += item;
        }

        Assert.AreEqual(45, sum);
    }

    [TestMethod]
    public void TestCopyTo()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        var dest = new int[7];
        _list->CopyTo(dest);

        for (var i = 0; i < 7; i++)
        {
            Assert.AreEqual(i, dest[i]);
        }
    }

    [TestMethod]
    public void TestCopyToPartial()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        var dest = new int[5];
        _list->CopyTo(dest);

        for (var i = 0; i < 5; i++)
        {
            Assert.AreEqual(i, dest[i]);
        }
    }

    [TestMethod]
    public void TestAddNoResize()
    {
        _list->EnsureCapacity(5);
        _list->AddNoResize(1);
        _list->AddNoResize(2);
        _list->AddNoResize(3);
        _list->AddNoResize(4);
        _list->AddNoResize(5);

        Assert.AreEqual(5, _list->Count);
        Assert.AreEqual(1, (*_list)[0]);
        Assert.AreEqual(5, (*_list)[4]);
    }

    [TestMethod]
    public void TestChunksFreedOnShrink()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        Assert.IsTrue(_list->ChunkCount >= 4);

        _list->RemoveRange(3, 7);

        Assert.AreEqual(3, _list->Count);
        Assert.IsTrue(_list->ChunkCount <= 1);
    }

    [TestMethod]
    public void TestChunksFreedOnClear()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        Assert.IsTrue(_list->ChunkCount >= 4);
        _list->Clear();
        Assert.AreEqual(0, _list->ChunkCount);
    }

    [TestMethod]
    public void TestParallelReader()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        var reader = _list->AsParallelReader();
        Assert.AreEqual(7, reader.Count);

        for (var i = 0; i < 7; i++)
        {
            Assert.AreEqual(i, reader[i]);
        }
    }

    [TestMethod]
    public void TestParallelWriterSingleThreaded()
    {
        _list->EnsureCapacity(5);
        var writer = _list->AsParallelWriter();
        writer.Add(10);
        writer.Add(20);
        writer.Add(30);

        Assert.AreEqual(3, _list->Count);
        Assert.AreEqual(10, (*_list)[0]);
        Assert.AreEqual(20, (*_list)[1]);
        Assert.AreEqual(30, (*_list)[2]);
    }

    [TestMethod]
    public void TestParallelWriterAddRange()
    {
        _list->EnsureCapacity(6);
        int[] values = { 1, 2, 3, 4, 5, 6 };
        var writer = _list->AsParallelWriter();
        writer.AddRange(values);

        Assert.AreEqual(6, _list->Count);
        for (var i = 0; i < 6; i++)
        {
            Assert.AreEqual(i + 1, (*_list)[i]);
        }
    }

    [TestMethod]
    public void TestParallelWriterAutoAllocatesChunks()
    {
        _list->EnsureCapacity(10);
        var writer = _list->AsParallelWriter();
        for (var i = 0; i < 10; i++)
        {
            writer.Add(i);
        }

        Assert.AreEqual(10, _list->Count);
        Assert.IsTrue(_list->ChunkCount >= 4);

        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(i, (*_list)[i]);
        }
    }

    [TestMethod]
    public void TestToList()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        var managedList = _list->ToList();
        Assert.AreEqual(7, managedList.Count);

        for (var i = 0; i < 7; i++)
        {
            Assert.AreEqual(i, managedList[i]);
        }
    }

    [TestMethod]
    public void TestCopyToRange()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        var dest = new int[8];
        _list->CopyTo(dest, 2, 1, 5);

        Assert.AreEqual(2, dest[1]);
        Assert.AreEqual(3, dest[2]);
        Assert.AreEqual(4, dest[3]);
        Assert.AreEqual(5, dest[4]);
        Assert.AreEqual(6, dest[5]);
    }

    [TestMethod]
    public void TestCopyFrom()
    {
        int[] source = { 100, 200, 300, 400, 500 };
        _list->CopyFrom(source);

        Assert.AreEqual(5, _list->Count);
        Assert.AreEqual(100, (*_list)[0]);
        Assert.AreEqual(500, (*_list)[4]);
    }

    [TestMethod]
    public void TestCopyFromRange()
    {
        _list->Resize(5);
        int[] source = { 0, 0, 10, 20, 30 };
        _list->CopyFrom(source, 2, 2, 3);

        Assert.AreEqual(10, (*_list)[2]);
        Assert.AreEqual(20, (*_list)[3]);
        Assert.AreEqual(30, (*_list)[4]);
    }

    [TestMethod]
    public void TestGetUnsafePtrSingleChunk()
    {
        _list->Add(42);
        _list->Add(99);

        var ptr = (int*)_list->GetUnsafePtr();
        Assert.AreEqual(42, ptr[0]);
        Assert.AreEqual(99, ptr[1]);
    }

    [TestMethod]
    public void TestGetUnsafePtrMultiChunkThrows()
    {
        for (var i = 0; i < 7; i++)
        {
            _list->Add(i);
        }

        Assert.ThrowsExactly<InvalidOperationException>(() => _list->GetUnsafePtr());
    }

    [TestMethod]
    public void TestResize()
    {
        _list->Resize(5);
        Assert.AreEqual(5, _list->Count);

        (*_list)[0] = 10;
        (*_list)[4] = 50;

        Assert.AreEqual(10, (*_list)[0]);
        Assert.AreEqual(50, (*_list)[4]);
    }

    [TestMethod]
    public void TestRemoveRangeSwapBack()
    {
        for (var i = 0; i < 10; i++)
        {
            _list->Add(i);
        }

        _list->RemoveRangeSwapBack(2, 3);

        Assert.AreEqual(7, _list->Count);
        Assert.AreEqual(0, (*_list)[0]);
        Assert.AreEqual(1, (*_list)[1]);
        Assert.AreEqual(7, (*_list)[2]);
        Assert.AreEqual(8, (*_list)[3]);
        Assert.AreEqual(9, (*_list)[4]);
        Assert.AreEqual(5, (*_list)[5]);
        Assert.AreEqual(6, (*_list)[6]);
    }

    [TestMethod]
    public void TestChunkCapacityProperty()
    {
        Assert.AreEqual(3, _list->ChunkCapacity);
    }

    [TestMethod]
    public void TestAddAfterClear()
    {
        for (var i = 0; i < 5; i++)
        {
            _list->Add(i);
        }

        _list->Clear();

        _list->Add(100);
        _list->Add(200);

        Assert.AreEqual(2, _list->Count);
        Assert.AreEqual(100, (*_list)[0]);
        Assert.AreEqual(200, (*_list)[1]);
    }

    [TestMethod]
    public unsafe void TestConcurrentAddsNoCorruption()
    {
        const int threadCount = 4;
        const int perThread = 250;
        const int totalCount = threadCount * perThread;

        _list->EnsureCapacity(totalCount);
        var tasks = new Task[threadCount];

        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(() =>
            {
                var writer = _list->AsParallelWriter();
                for (var i = 0; i < perThread; i++)
                {
                    writer.Add(threadId * perThread + i);
                }
            });
        }

        Task.WaitAll(tasks);

        Assert.AreEqual(totalCount, _list->Count);

        var found = new bool[totalCount];
        for (var i = 0; i < totalCount; i++)
        {
            var value = (*_list)[i];
            Assert.IsTrue(value >= 0 && value < totalCount, $"Value {value} out of range at index {i}");
            Assert.IsFalse(found[value], $"Duplicate value {value} at index {i}");
            found[value] = true;
        }

        for (var i = 0; i < totalCount; i++)
        {
            Assert.IsTrue(found[i], $"Value {i} was never written");
        }
    }

    [TestMethod]
    public unsafe void TestConcurrentAddRangeNoCorruption()
    {
        const int threadCount = 4;
        const int perThread = 250;
        const int totalCount = threadCount * perThread;

        _list->EnsureCapacity(totalCount);
        var tasks = new Task[threadCount];

        for (var t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(() =>
            {
                var values = new int[perThread];
                for (var i = 0; i < perThread; i++)
                {
                    values[i] = threadId * perThread + i;
                }

                var writer = _list->AsParallelWriter();
                writer.AddRange(values);
            });
        }

        Task.WaitAll(tasks);

        Assert.AreEqual(totalCount, _list->Count);

        var found = new bool[totalCount];
        for (var i = 0; i < totalCount; i++)
        {
            var value = (*_list)[i];
            Assert.IsTrue(value >= 0 && value < totalCount);
            Assert.IsFalse(found[value]);
            found[value] = true;
        }

        for (var i = 0; i < totalCount; i++)
        {
            Assert.IsTrue(found[i]);
        }
    }

    [TestMethod]
    public unsafe void TestReaderDropsStaleCountAfterWrite()
    {
        _list->Add(1);
        _list->Add(2);
        _list->Add(3);

        var writer = _list->AsParallelWriter();
        writer.Add(4);
        writer.Add(5);

        var reader = _list->AsParallelReader();
        Assert.AreEqual(5, reader.Count);
        Assert.AreEqual(1, reader[0]);
        Assert.AreEqual(2, reader[1]);
        Assert.AreEqual(3, reader[2]);
        Assert.AreEqual(4, reader[3]);
        Assert.AreEqual(5, reader[4]);
    }

    [TestMethod]
    public unsafe void TestParallelWriterAutoAllocatesChunksConcurrently()
    {
        _list->EnsureCapacity(20);

        var tasks = new Task[2];

        tasks[0] = Task.Run(() =>
        {
            var writer = _list->AsParallelWriter();
            for (var i = 0; i < 10; i++)
            {
                writer.Add(i);
            }
        });

        tasks[1] = Task.Run(() =>
        {
            var writer = _list->AsParallelWriter();
            for (var i = 10; i < 20; i++)
            {
                writer.Add(i);
            }
        });

        Task.WaitAll(tasks);

        Assert.AreEqual(20, _list->Count);
        Assert.IsTrue(_list->ChunkCount >= 7);

        var found = new bool[20];
        for (var i = 0; i < 20; i++)
        {
            found[(*_list)[i]] = true;
        }

        for (var i = 0; i < 20; i++)
        {
            Assert.IsTrue(found[i]);
        }
    }

    [TestMethod]
    public unsafe void TestIndexerDoesNotCrashDuringConcurrentWrite()
    {
        const int prePopulate = 3;
        for (var i = 0; i < prePopulate; i++)
        {
            _list->Add(i * 10);
        }

        _list->EnsureCapacity(prePopulate + 100);

        Exception? readException = null;

        var readTask = Task.Run(() =>
        {
            try
            {
                var reader = _list->AsParallelReader();
                for (var iteration = 0; iteration < 1000; iteration++)
                {
                    var count = reader.Count;
                    for (var i = 0; i < Math.Min(count, prePopulate); i++)
                    {
                        _ = reader[i];
                    }
                }
            }
            catch (Exception ex)
            {
                readException = ex;
            }
        });

        var writeTask = Task.Run(() =>
        {
            var writer = _list->AsParallelWriter();
            for (var i = 0; i < 100; i++)
            {
                writer.Add(i + 100);
            }
        });

        Task.WaitAll(readTask, writeTask);

        Assert.IsNull(readException, $"Reader threw: {readException?.Message}");
        Assert.AreEqual(prePopulate + 100, _list->Count);
    }
}
