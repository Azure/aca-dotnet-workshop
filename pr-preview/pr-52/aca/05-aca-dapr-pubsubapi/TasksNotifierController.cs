using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("api/tasksnotifier")]
    [ApiController]
    public class TasksNotifierController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        public TasksNotifierController(IConfiguration config, ILogger<TasksNotifierController> logger)
        {
            _config = config;
            _logger = logger;
        }

        public IActionResult Get()
        {
            return Ok();
        }

        [Dapr.Topic("taskspubsub", "tasksavedtopic")]
        [HttpPost("tasksaved")]
        public async Task<IActionResult> TaskSaved([FromBody] TaskModel taskModel)
        {
            _logger.LogInformation("Started processing message with Task Name '{0}'", taskModel.TaskName);

             //Do the actual sending of emails here, return 200 ok to consumer of the message
             return Ok();
            //In case we need to return message back to the topic, return http 400 bad request
            //return BadRequest();
        }
    }
}