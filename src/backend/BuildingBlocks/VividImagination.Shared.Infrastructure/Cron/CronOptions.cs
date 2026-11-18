namespace VividImagination.Shared.Infrastructure.Cron;

public class CronOptions
{
    public const string SectionName = "Cron";

    /// <summary>Quartz scheduler name. All clustered nodes must share the same name.</summary>
    public string SchedulerName { get; set; } = "LocalChampScheduler";

    /// <summary>false = in-memory store (local dev / tests). true = PostgreSQL ADO store.</summary>
    public bool UsePersistentStore { get; set; } = false;

    /// <summary>Name under ConnectionStrings:{name}. Use a dedicated connection, not the app DbContext's.</summary>
    public string ConnectionStringName { get; set; } = "Quartz";

    /// <summary>Must match the prefix used in the schema script, e.g. "scheduler.qrtz_" or "qrtz_".</summary>
    public string TablePrefix { get; set; } = "scheduler.qrtz_";

    /// <summary>Run as a cluster so each trigger fires on only one node.</summary>
    public bool Clustered { get; set; } = true;

    public TimeSpan CheckinInterval { get; set; } = TimeSpan.FromSeconds(10);
}

public enum CronMisfirePolicy
{
    /// <summary>Skip missed fire times and wait for the next scheduled one. Safest default.</summary>
    DoNothing,

    /// <summary>Fire once immediately for the missed window(s), then resume the schedule.</summary>
    FireOnceNow,

    /// <summary>Fire every missed execution as soon as possible. Only for idempotent jobs.</summary>
    IgnoreMisfires
}