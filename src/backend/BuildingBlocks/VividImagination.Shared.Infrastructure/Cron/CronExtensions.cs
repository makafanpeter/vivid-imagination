using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace VividImagination.Shared.Infrastructure.Cron
{
    public static class CronExtensions
    {
        public static IServiceCollection AddCron(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = configuration.GetSection(CronOptions.SectionName)
                .Get<CronOptions>() ?? new CronOptions();

            services.AddQuartz(q =>
            {
                q.ConfigureScheduler(scheduler =>
                {
                    scheduler.InstanceName = options.SchedulerName;
                    scheduler.InstanceId = "AUTO";
                });

                if (!options.UsePersistentStore)
                {
                    q.UseInMemoryStore();
                    return;
                }

                var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
                    ?? throw new InvalidOperationException(
                        $"Connection string '{options.ConnectionStringName}' is required when {CronOptions.SectionName}:UsePersistentStore is true.");

                q.UsePersistentStore(store =>
                {
                    store.UsePostgres(connectionString);

                    store.ConfigureStore(storeOptions =>
                    {
                        storeOptions.TablePrefix = options.TablePrefix;

                        // Equivalent to Quartz 3's UseProperties = true:
                        // stores JobDataMap values as strings.
                        storeOptions.StoreJobDataAsStrings = true;
                    });

                    store.UseSystemTextJsonSerializer();

                    if (options.Clustered)
                    {
                        store.UseClustering(cluster =>
                        {
                            cluster.CheckinInterval = options.CheckinInterval;
                        });
                    }
                });
            });

            services.AddQuartzHostedService(hostedService =>
            {
                hostedService.WaitForJobsToComplete = true;
            });

            return services;
        }



        public static IServiceCollection AddCronJob<TJob>(
        this IServiceCollection services,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        CronMisfirePolicy misfirePolicy = CronMisfirePolicy.DoNothing,
        bool requestRecovery = false)
        where TJob : class, IJob
        {
            if (!CronExpression.TryParse(cronExpression, out _))
            {
                throw new ArgumentException(
                    $"Invalid Quartz cron expression for {typeof(TJob).Name}: '{cronExpression}'",
                    nameof(cronExpression));
            }

            var name = typeof(TJob).FullName!;
            var timeZoneInfo = timeZone ?? TimeZoneInfo.Utc;

            services.AddQuartz(q =>
            {
                q.ScheduleJob<TJob>(
                    trigger => trigger
                        .WithIdentity($"{name}.trigger")
                        .WithDescription(cronExpression)
                        .WithCronSchedule(cronExpression, cron =>
                        {
                            cron.InTimeZone(timeZoneInfo);

                            cron.WithMisfireInstruction(misfirePolicy switch
                            {
                                CronMisfirePolicy.FireOnceNow =>
                                    CronTriggerMisfireInstruction.FireAndProceed,

                                CronMisfirePolicy.IgnoreMisfires =>
                                    CronTriggerMisfireInstruction.IgnoreMisfires,

                                _ => CronTriggerMisfireInstruction.DoNothing
                            });
                        }),
                    job => job
                        .WithIdentity(name)
                        .WithDescription(typeof(TJob).Name)
                        .RequestRecovery(requestRecovery));
            });

            return services;
        }
    }

}