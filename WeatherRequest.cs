using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestEase;
using Azure.Data.Tables;
using Azure;

namespace YourWeatherInfo_Functions
{
    public class WeatherData
    {
        public Location Location { get; set; }
        public Current Current { get; set; }
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
            log.LogInformation("C# WeatherRequest HTTP trigger function processed a request.");
            
            //Read Zipcode from request
            string zipcode = req.Query["zipcode"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            zipcode ??= data?.zipcode;

            //Connect to Azurite Table Storage Emulator
            string endpointProtocol = Environment.GetEnvironmentVariable("DefaultEndpointsProtocol");
            string accountName = Environment.GetEnvironmentVariable("AccountName");
            string accountKey = Environment.GetEnvironmentVariable("AccountKey");
            string tableEndpoint = Environment.GetEnvironmentVariable("TableEndpoint");
            string connectionString = "DefaultEndpointsProtocol=" + endpointProtocol + ";AccountName=" + accountName + ";AccountKey=" + accountKey + ";TableEndpoint=" + tableEndpoint + ";";

            //Connect to table client
            TableServiceClient tableServiceClient = new(
                connectionString
                );

            //Connect to WeatherRecords table
            TableClient tableClient = tableServiceClient.GetTableClient(
                tableName: "WeatherRecords"
            );

            //Create WeatherRecords table if it does not exist
            await tableClient.CreateIfNotExistsAsync();

            WeatherRecord cachedData;
            //Read a single weather record into cachedData
            try
            {
                cachedData = await tableClient.GetEntityAsync<WeatherRecord>(
                    partitionKey: zipcode,
                    rowKey: ""
                );
                System.TimeSpan diff = DateTimeOffset.Now.Subtract((DateTimeOffset)cachedData.Timestamp);
                if (diff.TotalMinutes > 10)
                {
                    log.LogError("Cached time is over 10 minutes time to update");
                    WeatherRecord weatherRecord = await getWeatherRecordAsync(zipcode);
                    await tableClient.UpdateEntityAsync<WeatherRecord>(weatherRecord, cachedData.ETag);
                    log.LogInformation("Updated row at " + zipcode);
                    return new OkObjectResult(weatherRecord.WeatherRecordJson);
                }
                else
                {
                    log.LogInformation("Using cachedData it's only " + diff.TotalMinutes + " minutes old");
                    return new OkObjectResult(cachedData.WeatherRecordJson);
                }
            }
            catch (Exception e)
            {
                log.LogError("Error occured while getting cache data\n" + e);
                WeatherRecord weatherRecord = await getWeatherRecordAsync(zipcode);
                await tableClient.AddEntityAsync<WeatherRecord>(weatherRecord);
                log.LogInformation("Inserted a weather record into WeatherRecord table");
                return new OkObjectResult(weatherRecord.WeatherRecordJson);
            }
        }
        public static async Task<WeatherRecord> getWeatherRecordAsync(string zipcode)
        {
            //Fetch and store a weather record as type WeatherData from weather API
            IWeatherApi api = RestClient.For<IWeatherApi>("https://api.weatherapi.com");
            WeatherData weatherData = await api.GetWeatherDataAsync(zipcode);
            Console.WriteLine(weatherData);
            Console.WriteLine($"Name: {weatherData.Location.Name}. Location: {zipcode}.\n" +
                              $"Temperature: {weatherData.Current.Temp_f}. Wind Speed: {weatherData.Current.Wind_mph} MPH. Wind Direction: {weatherData.Current.Wind_dir}.\n" +
                              $"Cloud Coverage: {weatherData.Current.Cloud}. Humidity {weatherData.Current.Humdity}.");

            return new WeatherRecord()
            {
                PartitionKey = zipcode,
                RowKey = "",
                WeatherRecordJson = JObject.FromObject(weatherData).ToString()
            };
        }
    }
}
