using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace YourWeatherInfo_Functions
{
    public class WeatherRequest
    {
        private readonly IWeatherService WeatherService;

        public WeatherRequest(IWeatherService weatherService)
        {
            WeatherService = weatherService;
        }

        [FunctionName("WeatherRequest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# WeatherRequest HTTP trigger function processed a request.");
            
            //Read Zipcode from POST request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string zipcode = data?.zipcode;

            WeatherRecord weatherRecord = await WeatherService.GetWeatherRecord(log, zipcode);
            return new OkObjectResult(weatherRecord.WeatherRecordJson);
        }
    }
}
