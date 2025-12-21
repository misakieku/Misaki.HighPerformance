using Misaki.HighPerformance.Image;

//BenchmarkDotNet.Running.BenchmarkRunner.Run<Misaki.HighPerformance.Test.Benchmark.ParallelNoiseBenchmark>();

//using Misaki.HighPerformance.Collections;
//using Misaki.HighPerformance.LowLevel.Buffer;
//using Misaki.HighPerformance.LowLevel.Collections;

//AllocationManager.EnableDebugLayer();
//using var csm = new UnsafeSlotMap<int>(4, Allocator.Persistent);
//AllocationManager.Dispose();

//using Misaki.HighPerformance.Mathematics;
//using System.Numerics;

//var a = new Misaki.HighPerformance.Mathematics.float4x4(
//    1, 2, 3, 4,
//    5, 6, 7, 8,
//    9, 10, 11, 12,
//    13, 14, 15, 16);
//var b = new Misaki.HighPerformance.Mathematics.float4x4(
//    16, 15, 14, 13,
//    12, 11, 10, 9,
//    8, 7, 6, 5,
//    4, 3, 2, 1);

//Console.WriteLine(math.mul(a, b));

//var ma = new Matrix4x4(
//        1, 2, 3, 4,
//        5, 6, 7, 8,
//        9, 10, 11, 12,
//        13, 14, 15, 16);
//var mb = new Matrix4x4(
//        16, 15, 14, 13,
//        12, 11, 10, 9,
//        8, 7, 6, 5,
//        4, 3, 2, 1);
//Console.WriteLine(Matrix4x4.Multiply(ma, mb));


const string _IMAGE_PATH = "C:/Users/Misaki/Downloads/Im/119683453_p2.jpg";

using var stream = File.OpenRead(_IMAGE_PATH);
var imageInfo = ImageInfo.FromStream(stream);
using var image = ImageResult.FromStream(stream);

Console.WriteLine($"{imageInfo.Width}x{imageInfo.Height} {imageInfo.ColorComponents}");
