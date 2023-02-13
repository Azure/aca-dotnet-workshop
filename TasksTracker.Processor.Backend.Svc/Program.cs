using Microsoft.ApplicationInsights.Extensibility;

namespace TasksTracker.Processor.Backend.Svc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddDapr();

            builder.Services.AddApplicationInsightsTelemetry();
            
            builder.Services.Configure<TelemetryConfiguration>((o) => {
                o.TelemetryInitializers.Add(new AppInsightsTelemetryInitializer());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCloudEvents();

            app.MapControllers();

            app.MapSubscribeHandler();

            app.Run();
        }
    }
}