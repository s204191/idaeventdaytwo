using Microsoft.AspNetCore.Mvc;

namespace AzureWebApiSolution.Controllers
{    
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;
        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetString([FromQuery] string str)
        {
            _logger.LogInformation("Called http REST endpoint GetString from Swagger");
            return Ok($"Received {str} from http request. timestamp: {DateTime.Now}");
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public IActionResult GetInt([FromQuery] int nr1, int nr2)
        {
            _logger.LogInformation("Called http REST endpoint GetInt from Swagger");
            return Ok($"Received {nr1} & {nr2} from http request. {nr1} + {nr2} = {nr1 + nr2} Timestamp: {DateTime.Now}");
        }

        //Lav endpoint til at modtage billeder
        //Gør klar til endpoint der skal sætte metadata 
    }
}
