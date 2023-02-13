using Dapr.Client;
using Microsoft.ApplicationInsights.Extensibility;
using TasksTracker.TasksManager.Backend.Api;
using TasksTracker.TasksManager.Backend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());

//builder.Services.AddSingleton<ITasksManager, FakeTasksManager>();

builder.Services.AddSingleton<ITasksManager, TasksStoreManager>();

builder.Services.AddControllers();

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<TelemetryConfiguration>((o) => {
    o.TelemetryInitializers.Add(new AppInsightsTelemetryInitializer());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
