using System;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Common.Structs;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Postgres.DataAccessLayers;
using BirdsiteLive.DAL.Postgres.Settings;
using BirdsiteLive.Middleware;
using BirdsiteLive.Services;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Tools;
using dotMakeup.Instagram;
using dotMakeup.ipfs;
using Lamar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Grafana.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;

namespace BirdsiteLive
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            Console.WriteLine($"EnvironmentName {env.EnvironmentName}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName.ToLowerInvariant()}.json", optional: true)
                .AddEnvironmentVariables();
            if (env.IsDevelopment()) builder.AddUserSecrets<Startup>();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(builder => builder.AddService(
                    serviceName: "dotmakeup", 
                    autoGenerateServiceInstanceId: false,
                    serviceInstanceId: Environment.MachineName
                    ))
                .WithMetrics(config => config.AddMeter("DotMakeup"))
                .WithMetrics(config => config.AddMeter("Microsoft.AspNetCore.Hosting"))
                .UseGrafana(config =>
                {
                    config.Instrumentations.Remove(Instrumentation.Process);
                    config.Instrumentations.Remove(Instrumentation.NetRuntime);
                    config.Instrumentations.Remove(Instrumentation.HttpClient);
                    config.ExporterSettings.EnableTraces = Environment.MachineName == "dotmakeup-0";
                    config.ServiceInstanceId = Environment.MachineName;
                });

            services.AddControllersWithViews();

        }

        public void ConfigureContainer(ServiceRegistry services)
        {
            var instanceSettings = Configuration.GetSection("Instance").Get<InstanceSettings>();
            services.For<InstanceSettings>().Use(_ => instanceSettings);

            var dbSettings = Configuration.GetSection("Db").Get<DbSettings>();
            services.For<DbSettings>().Use(x => dbSettings);

            var logsSettings = Configuration.GetSection("Logging").Get<LogsSettings>();
            services.For<LogsSettings>().Use(x => logsSettings);

            var moderationSettings = Configuration.GetSection("Moderation").Get<ModerationSettings>();
            services.For<ModerationSettings>().Use(x => moderationSettings);

            if (string.Equals(dbSettings.Type, DbTypes.Postgres, StringComparison.OrdinalIgnoreCase))
            {
                var connString = $"Host={dbSettings.Host};Username={dbSettings.User};Password={dbSettings.Password};Port={dbSettings.Port};Database={dbSettings.Name};MinPoolSize=3;MaxPoolSize=5;";
                var postgresSettings = new PostgresSettings
                {
                    ConnString = connString
                };
                services.For<PostgresSettings>().Use(x => postgresSettings);
                
                services.For<ITwitterUserDal>().Use<TwitterUserPostgresDal>().Singleton();
                services.For<IInstagramUserDal>().Use<InstagramUserPostgresDal>().Singleton();
                services.For<IFollowersDal>().Use<FollowersPostgresDal>().Singleton();
                services.For<IDbInitializerDal>().Use<DbInitializerPostgresDal>().Singleton();
                services.For<ISettingsDal>().Use<SettingsPostgresDal>().Singleton();
                services.For<IIpfsService>().Use<DotmakeupIpfs>().Singleton();
            }
            else
            {
                throw new NotImplementedException($"{dbSettings.Type} is not supported");
            }
            
            services.For<ITwitterUserService>().DecorateAllWith<CachedTwitterUserService>();
            services.For<ITwitterUserService>().Use<TwitterUserService>().Singleton();

            services.For<ITwitterAuthenticationInitializer>().Use<TwitterAuthenticationInitializer>().Singleton();

            if (Configuration.GetSection("Instance").Get<InstanceSettings>().InstaMode) 
                services.For<ISocialMediaService>().Use<InstagramService>().Singleton();
            else
                services.For<ISocialMediaService>().Use<TwitterService>().Singleton();
            
            services.For<ICachedStatisticsService>().Use<CachedStatisticsService>().Singleton();

            services.AddHttpClient();
            services.AddHttpClient("WithProxy").AddProxySupport(instanceSettings.ProxyURL, instanceSettings.ProxyUser, instanceSettings.ProxyPassword);
            
            services.Scan(_ =>
            {
                _.Assembly("BirdsiteLive.Twitter");
                _.Assembly("BirdsiteLive.Domain");
                _.Assembly("BirdsiteLive.DAL");
                _.Assembly("BirdsiteLive.DAL.Postgres");
                _.Assembly("BirdsiteLive.Moderation");
                _.Assembly("BirdsiteLive.Pipeline");
                _.TheCallingAssembly();

                //_.AssemblyContainingType<IDal>();
                //_.Exclude(type => type.Name.Contains("Settings"));
                
                _.WithDefaultConventions();

                _.LookForRegistries();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSocialNetworkInterceptor();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
