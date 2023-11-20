using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("ExternalTasksProcessor")]
    [ApiController]
    public class ExternalTasksProcessorController : ControllerBase
    {
        private readonly ILogger<ExternalTasksProcessorController> _logger;
        private readonly DaprClient _daprClient;

        public ExternalTasksProcessorController(ILogger<ExternalTasksProcessorController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessTaskAndStore([FromBody] TaskModel taskModel)
        {
            try
            {
                _logger.LogInformation("Started processing external task message from storage queue. Task Name: '{0}'", taskModel.TaskName);

                taskModel.TaskId = Guid.NewGuid();
                taskModel.TaskCreatedOn = DateTime.UtcNow;

                //Dapr SideCar Invocation (save task to a state store)
                await _daprClient.InvokeMethodAsync(HttpMethod.Post, "tasksmanager-backend-api", $"api/tasks", taskModel);

                _logger.LogInformation("Saved external task to the state store successfully. Task name: '{0}', Task Id: '{1}'", taskModel.TaskName, taskModel.TaskId);

                //ToDo: code to invoke external binding and store queue message content into blob file in Azure storage

                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}