using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly DaprClient _daprClient;
        private static string BLOB_STORE_NAME = "periodicjobstatestore";
        private static string BLOB_KEY_NAME = "ReadinessTestBlob";

        private static string TABLE_STORE_NAME = "emaillogsstatestore";
        public HealthController(ILogger<HealthController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;

        }

        [HttpGet("Readiness")]
        public async Task<IActionResult> Readiness()
        {

            try
            {
                string? _instanceId = Environment.GetEnvironmentVariable("HOSTNAME");

                _logger.LogInformation("Readiness::Invoked readiness endpoint by instance {0}", _instanceId);

                _logger.LogInformation("Readiness::Testing state store (blob) availability by creating new dummy file by instance {0}", _instanceId);
                await _daprClient.SaveStateAsync(BLOB_STORE_NAME, BLOB_KEY_NAME, DateTime.UtcNow);

                _logger.LogInformation("Readiness::Deleting dummy file from state store (blob) by instance {0}", _instanceId);
                await _daprClient.DeleteStateAsync(BLOB_STORE_NAME, BLOB_KEY_NAME);

            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Exception while invoking Readiness prob");
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }

            return Ok();

        }
    }
}
