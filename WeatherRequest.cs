using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestEase;
using Azure.Data.Tables;
using Azure.Storage;

namespace YourWeatherInfo_Functions
{
    // We receive a JSON response, so define a class to deserialize the json into
    public class WeatherData
    {
        public Location Location { get; set; }
        public Current Current { get; set; }

        /*
        // This is deserialized using Json.NET, so use attributes as necessary
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        */
    }
    public class Location
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Tz_id { get; set; }
        public double Localtime_epoch { get; set; }
        public string Localtime { get; set; }

    }
    public class Current
    {
        public double Temp_f { get; set; }
        public Condition Condition { get; set; }
        public double Wind_mph { get; set; }
        public string Wind_dir { get; set; }
        public double Humdity { get; set; }
        public double Cloud { get; set; }
        public double Uv { get; set; }
    }
    public class Condition { }

    // Defined an interface representing the API
    //[Header("User-Agent", "RestEase")]
    public interface IWeatherApi
    {
        // The [Get] attribute marks this method as a GET request
        [Get("v1/current.json?key=5e8bd5b4258b4f05a6111158220707&q={zipcode}&aqi=no")]
        Task<WeatherData> GetWeatherDataAsync([Path] string zipcode);
    }

    public static class WeatherRequest
    {
        [FunctionName("WeatherRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string zipcode = req.Query["zipcode"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            zipcode = zipcode ?? data?.zipcode;

            string responseMessage = string.IsNullOrEmpty(zipcode)
                ? "This HTTP triggered function executed successfully. Pass a zipcode in the query string or in the request body for a personalized response."
                : $"Hello, {zipcode}. This HTTP triggered function executed successfully.";

            // Create an implementation of that interface
            // We'll pass in the base URL for the API
            IWeatherApi api = RestClient.For<IWeatherApi>("https://api.weatherapi.com");

            // Now we can simply call methods on it
            // Normally you'd await the request, but this is a console app
            WeatherData weatherData = api.GetWeatherDataAsync(zipcode).Result;
            Console.WriteLine(weatherData);
            Console.WriteLine($"Name: {weatherData.Location.Name}. Location: {zipcode}.\n" +
                              $"Temperature: {weatherData.Current.Temp_f}. Wind Speed: {weatherData.Current.Wind_mph} MPH. Wind Direction: {weatherData.Current.Wind_dir}.\n" +
                              $"Cloud Coverage: {weatherData.Current.Cloud}. Humidity {weatherData.Current.Humdity}.");

            // With connection string
            var tableclient = new TableClient(
                "DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=https://127.0.0.1:10002/devstoreaccount1;", "WeatherTable"
              );

            // With account name and key
            var tableclient2 = new TableClient(
                new Uri("https://127.0.0.1:10002/devstoreaccount1/WeatherTable"),
                "WeatherTable",
                new TableSharedKeyCredential("devstoreaccount1", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==")
              );

            DateTime val = DateTime.Now;

            DateTimeOffset value = new DateTimeOffset(val);
            value.ToUnixTimeMilliseconds().ToString(); //ROWKEY Zipcode PRIMARYKEY

            //HTTP Connection Strings
            string connectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/" +
                "KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
            Console.WriteLine("connection string: " + connectionString);

            string connectionTable = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/" +
                "KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";
            Console.WriteLine("connection string: " + connectionTable);

            //zipcode weatherData
            await tableclient.CreateIfNotExistsAsync();
            //await tableclient.


            return new OkObjectResult(weatherData);
        }
    }
}
