using Microsoft.ApplicationInsights.Extensibility;
using TasksTracker.TasksManager.Backend.Api.Services;
using TasksTracker.TasksManager.Backend.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.Configure<TelemetryConfiguration>((o) => {
    o.TelemetryInitializers.Add(new AppInsightsTelemetryInitializer());
});
builder.Services.AddDaprClient();
builder.Services.AddSingleton<ITasksManager, TasksStoreManager>();
//builder.Services.AddSingleton<ITasksManager, FakeTasksManager>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
