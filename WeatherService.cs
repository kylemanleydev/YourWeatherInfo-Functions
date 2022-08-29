using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestEase;
using System;
using System.Threading.Tasks;

namespace YourWeatherInfo_Functions
{
    public interface IWeatherService
    {
        Task<WeatherRecord> GetWeatherRecord(ILogger log, string zipcode);
    }

    public class WeatherService : IWeatherService
    {
        public async Task<WeatherRecord> GetWeatherRecord(ILogger log, string zipcode)
        {
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
                    WeatherRecord weatherRecord = await GetWeatherRecordAsync(zipcode);
                    await tableClient.UpdateEntityAsync<WeatherRecord>(weatherRecord, cachedData.ETag);
                    log.LogInformation("Updated row at " + zipcode);
                    return weatherRecord;
                }
                else
                {
                    log.LogInformation("Using cachedData it's only " + diff.TotalMinutes + " minutes old");
                    return cachedData;
                }
            }
            catch (Exception e)
            {
                log.LogError("Error occured while getting cache data\n" + e);
                WeatherRecord weatherRecord = await GetWeatherRecordAsync(zipcode);
                await tableClient.AddEntityAsync<WeatherRecord>(weatherRecord);
                log.LogInformation("Inserted a weather record into WeatherRecord table");
                return weatherRecord;
            }
        }

        private async Task<WeatherRecord> GetWeatherRecordAsync(string zipcode)
        {
            //Fetch and store a weather record as type WeatherData from weather API
            IWeatherApi api = RestClient.For<IWeatherApi>("https://api.weatherapi.com");
            WeatherData weatherData = await api.GetWeatherDataAsync(zipcode);

            return new WeatherRecord()
            {
                PartitionKey = zipcode,
                RowKey = "",
                WeatherRecordJson = JObject.FromObject(weatherData).ToString()
            };
        }
    }
}
