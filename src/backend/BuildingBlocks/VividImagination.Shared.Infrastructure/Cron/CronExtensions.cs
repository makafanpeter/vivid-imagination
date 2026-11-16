using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace VividImagination.Shared.Infrastructure.Cron
{
    public static class CronExtensions
    {
        /// <summary>
        /// Registers Quartz with an optional PostgreSQL persistent/clustered store and the hosted service.
        /// Call once, from the host. Modules then call <see cref="AddCronJob{TJob}"/>.
        /// </summary>
        public static IServiceCollection AddCron(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(CronOptions.SectionName).Get<CronOptions>() ?? new CronOptions();

            services.AddQuartz(q =>
            {
                q.SchedulerName = options.SchedulerName;
                q.SchedulerId = "AUTO"; // unique per instance, required for clustering

                if (!options.UsePersistentStore)
                    return; // RAMJobStore

                var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
                                       ?? throw new InvalidOperationException(
                                           $"Connection string '{options.ConnectionStringName}' is required when Cron:UsePersistentStore is true.");

                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true; // JobDataMap holds strings only
                    store.UsePostgres(pg =>
                    {
                        pg.ConnectionString = connectionString;
                        pg.TablePrefix = options.TablePrefix;
                    });
                    store.UseSystemTextJsonSerializer();

                    if (options.Clustered)
                    {
                        store.UseClustering(c => c.CheckinInterval = options.CheckinInterval);
                    }
                });
            });

            services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

            return services;
        }

        /// <summary>
        /// Registers a job type and its cron trigger. Throws at startup if the expression is invalid.
        /// Quartz cron has a seconds field: "0 0/5 * * * ?" = every 5 minutes.
        /// </summary>
        public static IServiceCollection AddCronJob<TJob>(
            this IServiceCollection services,
            string cronExpression,
            TimeZoneInfo? timeZone = null,
            CronMisfirePolicy misfirePolicy = CronMisfirePolicy.DoNothing,
            bool requestRecovery = false)
            where TJob : class, IJob
        {
            if (!CronExpression.IsValidExpression(cronExpression))
            {
                throw new ArgumentException(
                    $"Invalid Quartz cron expression for {typeof(TJob).Name}: '{cronExpression}'",
                    nameof(cronExpression));
            }

            var name = typeof(TJob).FullName!;
            var tz = timeZone ?? TimeZoneInfo.Utc;

            services.AddQuartz(q => q.ScheduleJob<TJob>(
                trigger => trigger
                    .WithIdentity($"{name}.trigger")
                    .WithDescription(cronExpression)
                    .WithCronSchedule(cronExpression, cron =>
                    {
                        cron.InTimeZone(tz);
                        switch (misfirePolicy)
                        {
                            case CronMisfirePolicy.FireOnceNow:
                                cron.WithMisfireHandlingInstructionFireAndProceed();
                                break;
                            case CronMisfirePolicy.IgnoreMisfires:
                                cron.WithMisfireHandlingInstructionIgnoreMisfires();
                                break;
                            default:
                                cron.WithMisfireHandlingInstructionDoNothing();
                                break;
                        }
                    }),
                job => job
                    .WithIdentity(name)
                    .WithDescription(typeof(TJob).Name)
                    .RequestRecovery(requestRecovery)));

            return services;
        }
    }
}