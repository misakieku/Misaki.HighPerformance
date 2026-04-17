
global using unsafe JobExecutionFunc = delegate*<void*, ref Misaki.HighPerformance.Jobs.JobRanges, ref int, ref readonly Misaki.HighPerformance.Jobs.JobExecutionContext, bool>;
#if MHP_SUPPORT_MANAGED_JOB
global using unsafe ManagedJobExecutionFunc = delegate*<object, ref Misaki.HighPerformance.Jobs.JobRanges, ref int, ref readonly Misaki.HighPerformance.Jobs.JobExecutionContext, bool>;
#endif
