using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

namespace MemStache.Distributed.Telemetry
{
    public static class MemStacheTelemetryExtensions
    {
        public static IServiceCollection AddMemStacheTelemetry(this IServiceCollection services, Action<MemStacheTelemetryOptions> setupAction)
        {
            var options = new MemStacheTelemetryOptions();
            setupAction(options);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/memstache.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            // Configure OpenTelemetry
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .AddSource("MemStache.Distributed")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MemStache.Distributed"))
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();

                    if (options.UseJaeger)
                    {
                        builder.AddJaegerExporter();
                    }

                    if (options.UseZipkin)
                    {
                        builder.AddZipkinExporter();
                    }
                })
                .WithMetrics(builder =>
                {
                    builder
                        .AddMeter("MemStache.Distributed")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MemStache.Distributed"))
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();

                    if (options.UsePrometheus)
                    {
                        builder.AddPrometheusExporter();
                    }
                });

            return services;
        }
    }

    public class MemStacheTelemetryOptions
    {
        public bool UseJaeger { get; set; } = false;
        public bool UseZipkin { get; set; } = false;
        public bool UsePrometheus { get; set; } = false;
    }
}
