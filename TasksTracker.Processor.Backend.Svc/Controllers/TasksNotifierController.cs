using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("api/tasksnotifier")]
    [ApiController]
    public class TasksNotifierController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private readonly DaprClient _daprClient;

        public TasksNotifierController(IConfiguration config, ILogger<TasksNotifierController> logger, DaprClient daprClient)
        {
            _config = config;
            _logger = logger;
            _daprClient = daprClient;
        }

        public IActionResult Get()
        {
            return Ok();
        }

        [Dapr.Topic("dapr-pubsub-servicebus", "tasksavedtopic")]
        [HttpPost("tasksaved")]
        public async Task<IActionResult> TaskSaved([FromBody] TaskModel taskModel)
        {
            _logger.LogInformation("Started processing message with Task Name '{0}'", taskModel.TaskName);

            var sendGridResponse = await SendEmail(taskModel);

            if (sendGridResponse)
            {
                return Ok();
            }

            return BadRequest("Failed to send an email");
        }

        private async Task<bool> SendEmail(TaskModel taskModel)
        {
            var integrationEnabled = _config.GetValue<bool>("SendGrid:IntegrationEnabled");
            var sendEmailResponse = true;
            var subject = $"Task '{taskModel.TaskName}' is assigned to you!";
            var plainTextContent = $"Task '{taskModel.TaskName}' is assigned to you. Task should be completed by the end of: {taskModel.TaskDueDate.ToString("dd/MM/yyyy")}";

            try
            {
                //Send actual email using Dapr SendGrid Outbound Binding (Disabled when running load test)
                if (integrationEnabled)
                {
                    IReadOnlyDictionary<string, string> metaData = new Dictionary<string, string>()
                {
                    { "emailTo", taskModel.TaskAssignedTo },
                    { "emailToName", taskModel.TaskAssignedTo },
                    { "subject", subject }
                };
                    await _daprClient.InvokeBindingAsync("sendgrid", "create", plainTextContent, metaData);
                }
                else
                {
                    //Introduce artificial delay to slow down message processing
                    _logger.LogInformation("Simulate slow processing for email sending for Email with Email subject '{0}' Email to: '{1}'", subject, taskModel.TaskAssignedTo);
                    Thread.Sleep(1000);
                }

                if (sendEmailResponse)
                {
                    _logger.LogInformation("Email with subject '{0}' sent to: '{1}' successfully", subject, taskModel.TaskAssignedTo);
                }
            }
            catch (System.Exception ex)
            {
                sendEmailResponse = false;
                _logger.LogError(ex, "Failed to send email with subject '{0}' To: '{1}'.", subject, taskModel.TaskAssignedTo);
                throw;
            }
            return sendEmailResponse;
        }
    }
}
