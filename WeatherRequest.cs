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
using Azure;
using Newtonsoft.Json.Linq;

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

    // C# record type for WeatherRecord in the table
    public class WeatherRecord : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;

        public string WeatherRecordJson { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
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
            WeatherData weatherData = await api.GetWeatherDataAsync(zipcode);
            Console.WriteLine(weatherData);
            Console.WriteLine($"Name: {weatherData.Location.Name}. Location: {zipcode}.\n" +
                              $"Temperature: {weatherData.Current.Temp_f}. Wind Speed: {weatherData.Current.Wind_mph} MPH. Wind Direction: {weatherData.Current.Wind_dir}.\n" +
                              $"Cloud Coverage: {weatherData.Current.Cloud}. Humidity {weatherData.Current.Humdity}.");

            string endpointProtocol = Environment.GetEnvironmentVariable("DefaultEndpointsProtocol");
            string accountName = Environment.GetEnvironmentVariable("AccountName");
            string accountKey = Environment.GetEnvironmentVariable("AccountKey");
            string tableEndpoint = Environment.GetEnvironmentVariable("TableEndpoint");

            string connectionString = "DefaultEndpointsProtocol=" + endpointProtocol + ";AccountName=" + accountName + ";AccountKey=" + accountKey + ";TableEndpoint=" + tableEndpoint + ";";

            TableServiceClient tableServiceClient = new TableServiceClient(
                connectionString
                );

            // New instance of TableClient class referencing the server-side table
            TableClient tableClient = tableServiceClient.GetTableClient(
                tableName: "WeatherRecords"
            );

            //Create WeatherRecords table if it does not exist
            await tableClient.CreateIfNotExistsAsync();

            var weatherRecord = new WeatherRecord()
            {
                PartitionKey = zipcode,
                RowKey = "",
                WeatherRecordJson = JObject.FromObject(weatherData).ToString()
            };


            //Read a single weather record from container
            var cachedData = await tableClient.GetEntityAsync<WeatherRecord>(
                partitionKey: zipcode,
                rowKey: ""
            );

            System.TimeSpan diff = DateTimeOffset.Now.Subtract((DateTimeOffset) cachedData.Value.Timestamp);
            Console.WriteLine(diff);
            if (diff.TotalMinutes > 10) { 
                log.LogError("Cached time is over 10 minutes time to update"); 
                await tableClient.UpdateEntityAsync<WeatherRecord>(weatherRecord, cachedData.Value.ETag);
                Console.WriteLine("Updated row at " + zipcode);
            }
            else { 
                log.LogInformation("Using cachedData it's only " + diff.TotalMinutes + " minutes old"); 
                //Get JSON back to weather data
                return new OkObjectResult(cachedData.Value.WeatherRecordJson);
            }

            //Add weatherRecord to the table
            try
            {
                await tableClient.AddEntityAsync<WeatherRecord>(weatherRecord);
                Console.WriteLine("Inserted a weather record into WeatherRecord table");
            }
            catch (Exception e)
            {
                //Log without Exception
                log.LogError("Exception caught while inserting weather record into WeatherRecord table" + e);
            }

            log.LogInformation("Returned cachedData timestamp: " + cachedData.Value.Timestamp.ToString());
            log.LogInformation("Returned cachedData weatherRecord: " + cachedData.Value.WeatherRecordJson.ToString());

            return new OkObjectResult(weatherData);
        }
    }
}
