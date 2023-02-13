using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("ScheduledTasksManager")]
    [ApiController]
    public class ScheduledTasksManagerController : ControllerBase
    {
        private static string STORE_NAME = "periodicjobstatestore";
        private static string WATERMARK_KEY = "PeriodicSvcWatermark";
        private readonly ILogger<ScheduledTasksManagerController> _logger;
        private readonly DaprClient _daprClient;

        public ScheduledTasksManagerController(ILogger<ScheduledTasksManagerController> logger,
                                                DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpPost]
        public async Task CheckOverDueTasksJob()
        {
            var currentWatermark = DateTime.UtcNow;

            _logger.LogInformation($"ScheduledTasksManager::Timer Services triggered at: {currentWatermark}");

            var overdueTasksList = new List<TaskModel>();

            var waterMark = await _daprClient.GetStateAsync<DateTime>(STORE_NAME, WATERMARK_KEY);
            _logger.LogInformation($"ScheduledTasksManager::reading watermark from state store, watermark value: {waterMark}");

            var tasksList = await _daprClient.InvokeMethodAsync<List<TaskModel>>(HttpMethod.Get, "tasksmanager-backend-api", $"api/overduetasks?waterMark={waterMark}");
            _logger.LogInformation($"ScheduledTasksManager::completed query state store for tasks, retrieved tasks count: {tasksList.Count()}");

            foreach (var taskModel in tasksList)
            {
                if (currentWatermark.Date> taskModel.TaskDueDate.Date)
                {
                    overdueTasksList.Add(taskModel);
                }
            }

            if (overdueTasksList.Count> 0)
            {
                _logger.LogInformation($"ScheduledTasksManager::marking {overdueTasksList.Count()} as overdue tasks");
                await _daprClient.InvokeMethodAsync(HttpMethod.Post, "tasksmanager-backend-api", $"api/overduetasks/markoverdue", overdueTasksList);
            }

            _logger.LogInformation($"ScheduledTasksManager::storing watermark to state store, watermark value: {currentWatermark}");
            await _daprClient.SaveStateAsync(STORE_NAME, WATERMARK_KEY, currentWatermark);
        }
    }
}
