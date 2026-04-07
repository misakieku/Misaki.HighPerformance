using BenchmarkDotNet.Running;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Test.Benchmark;

//BenchmarkRunner.Run<SPMDBenchmark>();

var faceDirection = math.normalize(float3.zero - new float3(0.0f, 0.0f, 5.0f));
var test = quaternion.LookRotation(faceDirection, math.up());
var rotation = quaternion.EulerXYZ(new float3(0, math.radians(180.0f), 0));

Console.WriteLine(test);
Console.WriteLine(rotation);