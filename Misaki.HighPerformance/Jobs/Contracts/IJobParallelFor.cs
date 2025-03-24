namespace Misaki.HighPerformance.Jobs;

public interface IJobParallelFor
{
    public void Execute(int index);
}