using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.SPMD;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Misaki.HighPerformance.Mathematics.math;

//using TFloat = Misaki.HighPerformance.Mathematics.SPMD.WideLane<float>;
//using TInt = Misaki.HighPerformance.Mathematics.SPMD.WideLane<int>;

namespace Misaki.HighPerformance.Test.Benchmark;

internal unsafe struct MipLevel
{
    public float* data;
    public uint width;
    public uint height;
    public int offset;
    public float roughness;
}

//internal unsafe struct GGXMipGenerationJobSPMD : IJobParallelFor
internal unsafe struct GGXMipGenerationJobSPMD<TFloat, TInt> : IJobParallelFor
    where TFloat : unmanaged, ISPMDLane<TFloat, float>
    where TInt : unmanaged, ISPMDLane<TInt, int>
{
    public const uint SAMPLE_COUNT = 1024u;

    public ImageResultFloat image;
    public MipLevel* pMipLevels;
    public float* radicalInverse_VdCLut;
    public int numMipLevels;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float RadicalInverse_VdC(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f; // bits / 0x100000000
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector2<TFloat, float> Hammersley(TFloat i, uint N, float* lut)
    {
        var x = i / N;
        var y = TFloat.Load(lut + (int)i[0]);
        return MathV.Create<TFloat, float>(x, y);
    }

    // --- GGX Importance Sampling ---
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector3<TFloat, float> ImportanceSampleGGX(Vector2<TFloat, float> Xi, Vector3<TFloat, float> N, float roughness)
    {
        var a = roughness * roughness; // Disney/Epic remap roughness for better visual linearity

        var phi = 2.0f * PI * Xi.x;

        // Clamp the inside of the cosTheta Sqrt to prevent NaN on division precision edges
        var cosThetaInner = TFloat.Max((1.0f - Xi.y) / (1.0f + (a * a - 1.0f) * Xi.y), TFloat.Zero);
        var cosTheta = TFloat.Sqrt(cosThetaInner);

        // Clamp the inside of sinTheta to prevent sqrt of negative floating-point errors
        var sinThetaInner = TFloat.Max(1.0f - cosTheta * cosTheta, TFloat.Zero);
        var sinTheta = TFloat.Sqrt(sinThetaInner);

        // Spherical to Cartesian coordinates (Halfway vector)
        var (sinPhi, cosPhi) = TFloat.SinCos(phi);
        var H = MathV.Create<TFloat, float>(cosPhi * sinTheta, sinPhi * sinTheta, cosTheta);

        // Tangent space to World space
        var mask = TFloat.Abs(N.z) < 0.999f;
        var up = MathV.Select(mask, MathV.Create<TFloat, float>(0.0f, 0.0f, 1.0f), MathV.Create<TFloat, float>(1.0f, 0.0f, 0.0f));

        var tangent = MathV.Normalize(MathV.Cross(up, N));
        var bitangent = MathV.Cross(N, tangent);

        var sampleVec = (tangent * H.x) + (bitangent * H.y) + (N * H.z);
        return MathV.Normalize(sampleVec);
    }

    // --- Image Sampling Helpers ---
    // Maps a 3D direction vector to 2D equirectangular UVs
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector2<TFloat, float> DirToEquirectangularUV(Vector3<TFloat, float> dir)
    {
        var u = TFloat.Atan2(dir.z, dir.x);
        var v = TFloat.Asin(dir.y);

        u = u / (2.0f * PI) + 0.5f;
        v = v / PI + 0.5f;
        return MathV.Create<TFloat, float>(u, v);
    }

    // Samples the source HDR image using bilinear interpolation (simplified to nearest neighbor for brevity here)
    // Do not inline this function to avoid register pressure and code bloat in the main sampling loop, as this is a relatively heavy operation and we want to keep it separate for better instruction cache usage.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector3<TFloat, float> SampleEquirectangularMap(float* img, int w, int h, Vector3<TFloat, float> dir)
    {
        var uv = DirToEquirectangularUV(dir);

        // Nearest neighbor pixel coordinates
        var px = (uv.x * (w - 1.0f)).Cast<TInt, int>();
        var py = (uv.y * (h - 1.0f)).Cast<TInt, int>();

        // Clamp
        px = TInt.Clamp(px, TInt.Zero, w - 1);
        py = TInt.Clamp(py, TInt.Zero, h - 1);

        // Assuming float RGB array format
        var idx = (py * w + px) * 3;
        return MathV.GatherVector3<TFloat, float>(img, idx.GetUnsafePtr(), 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var m = 0;
        while (m < numMipLevels - 1 && loopIndex >= pMipLevels[m + 1].offset)
        {
            m++;
        }

        var span = new ReadOnlySpan<MipLevel>(pMipLevels, numMipLevels);
        var pLevel = &pMipLevels[m];

        var w = (int)pLevel->width;
        var h = (int)pLevel->height;
        var pData = pLevel->data;

        var local_i = loopIndex - pLevel->offset;
        var x = local_i % w;
        var y = local_i / w;
        var u = (float)x / (w - 1);
        var v = (float)y / (h - 1);

        var phi = (u - 0.5f) * 2.0f * PI;
        var theta = (v - 0.5f) * PI;

        sincos(theta, out var sinTheta, out var cosTheta);
        sincos(phi, out var sinPhi, out var cosPhi);
        var N = float3(cosTheta * cosPhi, sinTheta, cosTheta * sinPhi);
        N = normalize(N);

        // For split-sum, we assume View and Reflection directions equal the Normal
        var V = N;
        var R = N;

        var vN = MathV.Create<TFloat, float>(
            TFloat.Create(N.x),
            TFloat.Create(N.y),
            TFloat.Create(N.z)
        );

        var vV = MathV.Create<TFloat, float>(
            TFloat.Create(V.x),
            TFloat.Create(V.y),
            TFloat.Create(V.z)
        );

        var vPrefilteredColor = Vector3<TFloat, float>.Zero;
        var vTotalWeight = TFloat.Zero;

        // 3. Monte Carlo Integration Loop

        var dynamicSampleCount = (uint)max(1.0f, SAMPLE_COUNT * pLevel->roughness);
        var vDynamicSampleCount = TFloat.Create(dynamicSampleCount);
        var vLumaVector = MathV.Create<TFloat, float>(0.2126f, 0.7152f, 0.0722f);

        for (var i = 0u; i < dynamicSampleCount; i += (uint)TFloat.LaneWidth)
        {
            var laneIndices = TFloat.Sequence(i, 1.0f);
            var validLaneMask = laneIndices < vDynamicSampleCount;

            // Generate a Hammersley random sequence point
            var Xi = Hammersley(laneIndices, dynamicSampleCount, radicalInverse_VdCLut);

            // Get the halfway vector based on GGX NDF
            var H = ImportanceSampleGGX(Xi, vN, pLevel->roughness);

            // Calculate Light direction
            var L = MathV.Reflect(-vV, H);
            L = MathV.Normalize(L);

            var NdotL = TFloat.Max(MathV.Dot(vN, L), TFloat.Zero);
            var sampleColor = SampleEquirectangularMap(image.Data, (int)image.Width, (int)image.Height, L);

            NdotL &= validLaneMask;

            // The Karis Average Weight: 1 / (1 + luma)
            // A normal sky pixel (luma 1.0) gets a weight of 0.5.
            // A sun pixel (luma 1000.0) gets a tiny weight of ~0.001, naturally suppressing it.
            // This introduce bias, but significantly reduces fireflies without needing solid angle sampling or cdf inversion.
            // And since this is a mip generation step, a little bias is acceptable for much better performance and stability.
            var luma = MathV.Dot(sampleColor, vLumaVector);
            var fireflyWeight = TFloat.One / (TFloat.One + luma);
            var finalWeight = NdotL * fireflyWeight;

            vPrefilteredColor += sampleColor * finalWeight;
            vTotalWeight += finalWeight;
        }

        var totalWeight = 0.0f;
        var prefilteredColor = float3(0, 0, 0);

        for (var i = 0; i < TFloat.LaneWidth; i++)
        {
            prefilteredColor.x += vPrefilteredColor.x[i];
            prefilteredColor.y += vPrefilteredColor.y[i];
            prefilteredColor.z += vPrefilteredColor.z[i];
            totalWeight += vTotalWeight[i];
        }

        // 4. Average the result
        if (totalWeight > 0.0f)
        {
            prefilteredColor *= 1.0f / totalWeight;
        }

        // Write to output mip array
        var out_idx = (y * w + x) * 3;
        pData[out_idx] = prefilteredColor.x;
        pData[out_idx + 1] = prefilteredColor.y;
        pData[out_idx + 2] = prefilteredColor.z;
    }
}

internal unsafe struct GGXMipGenerationJob : IJobParallelFor
{
    public const uint SAMPLE_COUNT = 1024u;

    public ImageResultFloat image;
    public MipLevel* pMipLevels;
    public float* radicalInverse_VdCLut;
    public int numMipLevels;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadicalInverse_VdC(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f; // bits / 0x100000000
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float2 Hammersley(uint i, uint N, float* lut)
    {
        return float2((float)i / N, lut[i]);
    }

    // --- GGX Importance Sampling ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 ImportanceSampleGGX(float2 Xi, float3 N, float roughness)
    {
        var a = roughness * roughness; // Disney/Epic remap roughness for better visual linearity

        var phi = 2.0f * PI * Xi.x;
        var cosTheta = sqrt((1.0f - Xi.y) / (1.0f + (a * a - 1.0f) * Xi.y));
        var sinTheta = sqrt(1.0f - cosTheta * cosTheta);

        // Spherical to Cartesian coordinates (Halfway vector)
        sincos(phi, out var sinPhi, out var cosPhi);
        var H = float3(cosPhi * sinTheta, sinPhi * sinTheta, cosTheta);

        // Tangent space to World space
        var up = abs(N.z) < 0.999f ? float3(0.0f, 0.0f, 1.0f) : float3(1.0f, 0.0f, 0.0f);
        var tangent = normalize(cross(up, N));
        var bitangent = cross(N, tangent);

        var sampleVec = (tangent * H.x) + (bitangent * H.y) + (N * H.z);
        return normalize(sampleVec);
    }

    // --- Image Sampling Helpers ---
    // Maps a 3D direction vector to 2D equirectangular UVs
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float2 DirToEquirectangularUV(float3 dir)
    {
        var uv = float2(atan2(dir.z, dir.x), asin(dir.y));
        uv.x = uv.x / (2.0f * PI) + 0.5f;
        uv.y = uv.y / PI + 0.5f;
        return uv;
    }

    // Samples the source HDR image using bilinear interpolation (simplified to nearest neighbor for brevity here)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 SampleEquirectangularMap(float* img, int w, int h, float3 dir)
    {
        var uv = DirToEquirectangularUV(dir);

        // Nearest neighbor pixel coordinates
        var px = (int)(uv.x * (w - 1));
        var py = (int)(uv.y * (h - 1));

        // Clamp
        px = clamp(px, 0, w - 1);
        py = clamp(py, 0, h - 1);

        // Assuming float RGB array format
        var idx = (py * w + px) * 3;
        return float3(img[idx], img[idx + 1], img[idx + 2]);
    }

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var m = 0;
        while (m < numMipLevels - 1 && loopIndex >= pMipLevels[m + 1].offset)
        {
            m++;
        }

        var pLevel = &pMipLevels[m];

        var w = (int)pLevel->width;
        var h = (int)pLevel->height;
        var pData = pLevel->data;

        var local_i = loopIndex - pLevel->offset;
        var x = local_i % w;
        var y = local_i / w;
        var u = (float)x / (w - 1);
        var v = (float)y / (h - 1);

        var phi = (u - 0.5f) * 2.0f * PI;
        var theta = (v - 0.5f) * PI;

        sincos(theta, out var sinTheta, out var cosTheta);
        sincos(phi, out var sinPhi, out var cosPhi);
        var N = float3(cosTheta * cosPhi, sinTheta, cosTheta * sinPhi);
        N = normalize(N);

        // For split-sum, we assume View and Reflection directions equal the Normal
        var V = N;
        var R = N;

        var prefilteredColor = float3(0, 0, 0);
        var totalWeight = 0.0f;

        // 3. Monte Carlo Integration Loop
        var dynamicSampleCount = (uint)max(1.0f, SAMPLE_COUNT * pLevel->roughness);
        for (var i = 0u; i < dynamicSampleCount; i++)
        {
            // Generate a Hammersley random sequence point
            var Xi = Hammersley(i, dynamicSampleCount, radicalInverse_VdCLut);

            // Get the halfway vector based on GGX NDF
            var H = ImportanceSampleGGX(Xi, N, pLevel->roughness);

            // Calculate Light direction
            var L = reflect(-V, H);
            L = normalize(L);

            var NdotL = max(dot(N, L), 0.0f);

            // If light is above the horizon
            if (NdotL > 0.0f)
            {
                var sampleColor = SampleEquirectangularMap(image.Data, (int)image.Width, (int)image.Height, L);

                prefilteredColor += sampleColor * NdotL;
                totalWeight += NdotL;
            }
        }

        // 4. Average the result
        if (totalWeight > 0.0f)
        {
            prefilteredColor *= 1.0f / totalWeight;
        }

        // Write to output mip array
        var out_idx = (y * w + x) * 3;
        pData[out_idx] = prefilteredColor.x;
        pData[out_idx + 1] = prefilteredColor.y;
        pData[out_idx + 2] = prefilteredColor.z;
    }
}

[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 0, iterationCount: 1, invocationCount: 1, id: "QuickRun")]
public unsafe class GGXMipGenerationBenchmark
{
    private ImageResultFloat _image;
    private int _mipLevels;
    private int _totalPixel;
    private float** _pResult;
    private MipLevel* _pMipLevels;
    private float* _radicalInverse_VdCLut;

    private JobScheduler _jobScheduler = null!;

    [GlobalSetup]
    public void Setup()
    {
        //const string imagePath = "F:\\c\\SimpleRayTracer\\native\\assets\\hdri\\golden_gate_hills_1k.hdr";
        const string imagePath = "C:\\Users\\Misaki\\Downloads\\grasslands_sunset_4k.hdr";
        using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        _image = ImageResultFloat.FromStream(stream, ColorComponents.RGB);

        _mipLevels = (int)Math.Floor(Math.Log2(Math.Max(_image.Width, _image.Height))) + 1;

        _pResult = (float**)NativeMemory.Alloc((nuint)(_mipLevels * sizeof(float*)));
        _pMipLevels = (MipLevel*)NativeMemory.Alloc((nuint)(_mipLevels * sizeof(MipLevel)));

        uint w, h;

        for (var i = 0; i < _mipLevels; i++)
        {
            w = Math.Max(1, _image.Width >> i);
            h = Math.Max(1, _image.Height >> i);

            var sizeInBytes = (nuint)(w * h * 3 * sizeof(float));
            _pResult[i] = (float*)NativeMemory.Alloc(sizeInBytes);

            _pMipLevels[i] = new MipLevel
            {
                width = w,
                height = h,
                offset = _totalPixel,
                data = _pResult[i],
                roughness = (float)i / (_mipLevels - 1) // Linear roughness from 0 to 1 across mip levels
            };

            _totalPixel += (int)(w * h);
        }

        var desc = new JobSchedulerDesc
        {
            DependencyChainCapacity = 16,
            ThreadCount = Environment.ProcessorCount - 1,
            ThreadPriority = ThreadPriority.Normal,
        };

        _radicalInverse_VdCLut = (float*)NativeMemory.Alloc(GGXMipGenerationJob.SAMPLE_COUNT * sizeof(float));
        for (var i = 0u; i < GGXMipGenerationJob.SAMPLE_COUNT; i++)
        {
            _radicalInverse_VdCLut[i] = GGXMipGenerationJob.RadicalInverse_VdC(i);
        }

        _jobScheduler = new JobScheduler(in desc);
    }

    public void DumpMipLevelToPng(float* pData, int width, int height, string filePath)
    {
        // Create a standard 32-bit RGBA bitmap
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);

        // Get a pointer to the SkiaSharp pixel buffer
        var pPixels = (byte*)bitmap.GetPixels();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                // Your data is tightly packed floats: R, G, B
                var inIdx = (y * width + x) * 3;
                var r = pData[inIdx];
                var g = pData[inIdx + 1];
                var b = pData[inIdx + 2];

                // Basic Tone Mapping (Exposure + Gamma Correction) so we can see HDR values on a normal screen
                // Gamma 2.2 = roughly pow(color, 1.0/2.2)
                r = MathF.Pow(MathF.Max(0, r), 1.0f / 2.2f);
                g = MathF.Pow(MathF.Max(0, g), 1.0f / 2.2f);
                b = MathF.Pow(MathF.Max(0, b), 1.0f / 2.2f);

                // Convert 0.0-1.0 to 0-255 byte
                var rByte = (byte)Math.Clamp(r * 255.0f, 0, 255);
                var gByte = (byte)Math.Clamp(g * 255.0f, 0, 255);
                var bByte = (byte)Math.Clamp(b * 255.0f, 0, 255);

                // Write to Skia's buffer (RGBA)
                var outIdx = (y * width + x) * 4;
                pPixels[outIdx] = rByte;
                pPixels[outIdx + 1] = gByte;
                pPixels[outIdx + 2] = bByte;
                pPixels[outIdx + 3] = 255; // Alpha
            }
        }

        // Save out the preview
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
#if false
        for (var i = 0; i < _mipLevels; i++)
        {
            DumpMipLevelToPng(_pResult[i], (int)_pMipLevels[i].width, (int)_pMipLevels[i].height, $"C:\\Users\\Misaki\\Downloads\\Im\\mip_level_{i}.png");
        }
#endif

        _image.Dispose();
        for (var i = 0; i < _mipLevels; i++)
        {
            NativeMemory.Free(_pResult[i]);
        }

        NativeMemory.Free(_pResult);
        NativeMemory.Free(_pMipLevels);
        NativeMemory.Free(_radicalInverse_VdCLut);

        _jobScheduler.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void JobGGX()
    {
        JobHandle handle;
        if (WideLane.IsSupported)
        {
            var job = new GGXMipGenerationJobSPMD<WideLane<float>, WideLane<int>>
            {
                image = _image,
                pMipLevels = _pMipLevels,
                numMipLevels = _mipLevels,
                radicalInverse_VdCLut = _radicalInverse_VdCLut
            };

            handle = _jobScheduler.ScheduleParallelFor(in job, _totalPixel, 64);
        }
        else
        {
            var job = new GGXMipGenerationJobSPMD<ScalarLane<float>, ScalarLane<int>>
            {
                image = _image,
                pMipLevels = _pMipLevels,
                numMipLevels = _mipLevels,
                radicalInverse_VdCLut = _radicalInverse_VdCLut
            };

            handle = _jobScheduler.ScheduleParallelFor(in job, _totalPixel, 64);
        }

        _jobScheduler.Wait(handle);
    }

    //[Benchmark]
    public void ParallelGGX()
    {
        var job = new GGXMipGenerationJob
        {
            image = _image,
            pMipLevels = _pMipLevels,
            numMipLevels = _mipLevels,
            radicalInverse_VdCLut = _radicalInverse_VdCLut
        };

        Parallel.For(0, _totalPixel, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, i =>
        {
            var localJob = job;
            var ctx = new JobExecutionContext();
            localJob.Execute(i, in ctx);
        });
    }

    [Benchmark]
    public void SingleThreadGGX()
    {
        var job = new GGXMipGenerationJob
        {
            image = _image,
            pMipLevels = _pMipLevels,
            numMipLevels = _mipLevels,
            radicalInverse_VdCLut = _radicalInverse_VdCLut
        };

        var ctx = new JobExecutionContext();
        job.Run(_totalPixel, in ctx);
    }
}
