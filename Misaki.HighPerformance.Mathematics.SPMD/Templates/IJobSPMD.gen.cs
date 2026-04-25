using Misaki.HighPerformance.Jobs;
using System.Numerics;

namespace Misaki.HighPerformance.Mathematics.SPMD;

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
{
    void Execute<TLane0>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>;
}

internal struct SPMDJobWrapper<T, TNumber0> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
{
    void Execute<TLane0, TLane1>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
{
    void Execute<TLane0, TLane1, TLane2>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
{
    void Execute<TLane0, TLane1, TLane2, TLane3>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>
        where TLane3 : unmanaged, ISPMDLane<TLane3, TNumber3>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
{
    void Execute<TLane0, TLane1, TLane2, TLane3, TLane4>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>
        where TLane3 : unmanaged, ISPMDLane<TLane3, TNumber3>
        where TLane4 : unmanaged, ISPMDLane<TLane4, TNumber4>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
{
    void Execute<TLane0, TLane1, TLane2, TLane3, TLane4, TLane5>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>
        where TLane3 : unmanaged, ISPMDLane<TLane3, TNumber3>
        where TLane4 : unmanaged, ISPMDLane<TLane4, TNumber4>
        where TLane5 : unmanaged, ISPMDLane<TLane5, TNumber5>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
{
    void Execute<TLane0, TLane1, TLane2, TLane3, TLane4, TLane5, TLane6>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>
        where TLane3 : unmanaged, ISPMDLane<TLane3, TNumber3>
        where TLane4 : unmanaged, ISPMDLane<TLane4, TNumber4>
        where TLane5 : unmanaged, ISPMDLane<TLane5, TNumber5>
        where TLane6 : unmanaged, ISPMDLane<TLane6, TNumber6>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>, WideLane<TNumber6>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>>(loopIndex, in ctx);
    }
}

/// <summary>
/// A job interface for Single Program Multiple Data (SPMD) execution, allowing for efficient parallel processing of data across multiple lanes.
/// </summary>
/// <remarks>
/// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
/// </remarks>
/// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
/// <typeparam name="TNumber7">The first numeric type used in the SPMD job.</typeparam>
public interface IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    where TNumber7 : unmanaged, INumber<TNumber7>, IBinaryNumber<TNumber7>, IMinMaxValue<TNumber7>, IBitwiseOperators<TNumber7, TNumber7, TNumber7>
{
    void Execute<TLane0, TLane1, TLane2, TLane3, TLane4, TLane5, TLane6, TLane7>(int baseIndex, ref readonly JobExecutionContext ctx)
        where TLane0 : unmanaged, ISPMDLane<TLane0, TNumber0>
        where TLane1 : unmanaged, ISPMDLane<TLane1, TNumber1>
        where TLane2 : unmanaged, ISPMDLane<TLane2, TNumber2>
        where TLane3 : unmanaged, ISPMDLane<TLane3, TNumber3>
        where TLane4 : unmanaged, ISPMDLane<TLane4, TNumber4>
        where TLane5 : unmanaged, ISPMDLane<TLane5, TNumber5>
        where TLane6 : unmanaged, ISPMDLane<TLane6, TNumber6>
        where TLane7 : unmanaged, ISPMDLane<TLane7, TNumber7>;
}

internal struct SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    where TNumber7 : unmanaged, INumber<TNumber7>, IBinaryNumber<TNumber7>, IMinMaxValue<TNumber7>, IBitwiseOperators<TNumber7, TNumber7, TNumber7>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
        var remaining = totalIteration - baseIndex;

        if (remaining >= WideLane<TNumber0>.LaneWidth)
        {
            innerJob.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>, WideLane<TNumber6>, WideLane<TNumber7>>(baseIndex, in ctx);
        }
        else
        {
            for (var j = 0; j < remaining; j++)
            {
                innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>, ScalarLane<TNumber7>>(baseIndex + j, in ctx);
            }
        }
    }
}

internal struct SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7> : IJobParallelFor
    where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
    where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    where TNumber7 : unmanaged, INumber<TNumber7>, IBinaryNumber<TNumber7>, IMinMaxValue<TNumber7>, IBitwiseOperators<TNumber7, TNumber7, TNumber7>
{
    public T innerJob;
    public int totalIteration;

    public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
    {
        innerJob.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>, ScalarLane<TNumber7>>(loopIndex, in ctx);
    }
}


public static class IJobParallelForSPMDExtensions
{

    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2, TNumber3>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2, TNumber3>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>, WideLane<TNumber6>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }


    /// <summary>
    /// Run the SPMD job with the specified total count and job execution context directly on the calling thread.
    /// </summary>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber7">The first numeric type used in the SPMD job.</typeparam>
    /// <param name="job">The SPMD job to run.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="ctx">The job execution context providing information about the current execution environment.</param>
    public static void Run<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>(this ref T job, int totalIteration, ref readonly JobExecutionContext ctx)
        where T : struct, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    where TNumber7 : unmanaged, INumber<TNumber7>, IBinaryNumber<TNumber7>, IMinMaxValue<TNumber7>, IBitwiseOperators<TNumber7, TNumber7, TNumber7>
    {
        if (WideLane.IsSupported)
        {
            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            for (var loopIndex = 0; loopIndex < iterations; loopIndex++)
            {
                var baseIndex = loopIndex * WideLane<TNumber0>.LaneWidth;
                var remaining = totalIteration - baseIndex;

                if (remaining >= WideLane<TNumber0>.LaneWidth)
                {
                    job.Execute<WideLane<TNumber0>, WideLane<TNumber1>, WideLane<TNumber2>, WideLane<TNumber3>, WideLane<TNumber4>, WideLane<TNumber5>, WideLane<TNumber6>, WideLane<TNumber7>>(baseIndex, in ctx);
                }
                else
                {
                    for (var i = 0; i < remaining; i++)
                    {
                        job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>, ScalarLane<TNumber7>>(baseIndex + i, in ctx);
                    }
                }
            }
        }
        else
        {
            for (var loopIndex = 0; loopIndex < totalIteration; loopIndex++)
            {
                job.Execute<ScalarLane<TNumber0>, ScalarLane<TNumber1>, ScalarLane<TNumber2>, ScalarLane<TNumber3>, ScalarLane<TNumber4>, ScalarLane<TNumber5>, ScalarLane<TNumber6>, ScalarLane<TNumber7>>(loopIndex, in ctx);
            }
        }
    }

    /// <summary>
    /// Schedule the SPMD job for parallel execution across multiple threads, with the specified total count, batch size, and job execution context.
    /// </summary>
    /// <typeparam name="TNumber0">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber1">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber2">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber3">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber4">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber5">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber6">The first numeric type used in the SPMD job.</typeparam>
    /// <typeparam name="TNumber7">The first numeric type used in the SPMD job.</typeparam>
    /// <remarks>
    /// Always use TNumber0 as the primary type for determining lane width and job scheduling, even if it's not used in the job execution.
    /// </remarks>
    /// <param name="jobScheduler">The job scheduler to use for scheduling the job.</param>
    /// <param name="job">The SPMD job to schedule.</param>
    /// <param name="totalIteration">The total number of iterations to execute across all lanes.</param>
    /// <param name="batchSize">The number of iterations to execute in each batch for parallel execution.</param>
    /// <param name="preferLocal">Whether to prefer scheduling the job on the local thread for better cache locality.</param>
    /// <param name="priority">The priority of the job.</param>
    /// <param name="dependencies">Any job handles that this job depends on, which must complete before this job can start.</param>
    public static JobHandle ScheduleParallelSPDM<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>(this JobScheduler jobScheduler, ref T job, int totalIteration, int batchSize, bool preferLocal, JobPriority priority, params ReadOnlySpan<JobHandle> dependencies)
        where T : unmanaged, IJobSPMD<TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
        where TNumber0 : unmanaged, INumber<TNumber0>, IBinaryNumber<TNumber0>, IMinMaxValue<TNumber0>, IBitwiseOperators<TNumber0, TNumber0, TNumber0>
    where TNumber1 : unmanaged, INumber<TNumber1>, IBinaryNumber<TNumber1>, IMinMaxValue<TNumber1>, IBitwiseOperators<TNumber1, TNumber1, TNumber1>
    where TNumber2 : unmanaged, INumber<TNumber2>, IBinaryNumber<TNumber2>, IMinMaxValue<TNumber2>, IBitwiseOperators<TNumber2, TNumber2, TNumber2>
    where TNumber3 : unmanaged, INumber<TNumber3>, IBinaryNumber<TNumber3>, IMinMaxValue<TNumber3>, IBitwiseOperators<TNumber3, TNumber3, TNumber3>
    where TNumber4 : unmanaged, INumber<TNumber4>, IBinaryNumber<TNumber4>, IMinMaxValue<TNumber4>, IBitwiseOperators<TNumber4, TNumber4, TNumber4>
    where TNumber5 : unmanaged, INumber<TNumber5>, IBinaryNumber<TNumber5>, IMinMaxValue<TNumber5>, IBitwiseOperators<TNumber5, TNumber5, TNumber5>
    where TNumber6 : unmanaged, INumber<TNumber6>, IBinaryNumber<TNumber6>, IMinMaxValue<TNumber6>, IBitwiseOperators<TNumber6, TNumber6, TNumber6>
    where TNumber7 : unmanaged, INumber<TNumber7>, IBinaryNumber<TNumber7>, IMinMaxValue<TNumber7>, IBitwiseOperators<TNumber7, TNumber7, TNumber7>
    {
        if (WideLane.IsSupported)
        {
            var warper = new SPMDJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            var iterations = (totalIteration + WideLane<TNumber0>.LaneWidth - 1) / WideLane<TNumber0>.LaneWidth;
            return jobScheduler.ScheduleParallelFor(ref warper, iterations, batchSize, preferLocal, priority, dependencies);
        }
        else
        {
            var warper = new SPMDScalerJobWrapper<T, TNumber0, TNumber1, TNumber2, TNumber3, TNumber4, TNumber5, TNumber6, TNumber7>
            {
                innerJob = job,
                totalIteration = totalIteration,
            };

            return jobScheduler.ScheduleParallelFor(ref warper, totalIteration, batchSize, preferLocal, priority, dependencies);
        }
    }

}

