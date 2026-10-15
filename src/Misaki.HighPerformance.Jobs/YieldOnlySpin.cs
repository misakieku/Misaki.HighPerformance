namespace Misaki.HighPerformance.Jobs;

public struct YieldOnlySpin
{
    public const int SLEEP_0_FREQUENCY = 4;

    private static readonly bool s_isSingleCore = Environment.ProcessorCount == 1;

    private int _counter;

    public void SpinOnce(int threshold)
    {
        if (s_isSingleCore || _counter >= threshold)
        {
            var yieldsSoFar = _counter - threshold;
            if (yieldsSoFar % SLEEP_0_FREQUENCY == SLEEP_0_FREQUENCY - 1)
            {
                Thread.Sleep(0);
            }
            else
            {
                Thread.Yield();
            }
        }
        else
        {
            Thread.SpinWait(1 << Math.Min(_counter, 5));
        }

        if (_counter < int.MaxValue)
        {
            _counter++;
        }
    }

    public void Reset()
    {
        _counter = 0;
    }
}