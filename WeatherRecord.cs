using Azure;
using Azure.Data.Tables;
using System;

namespace YourWeatherInfo_Functions
{
    public class WeatherRecord : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public string WeatherRecordJson { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
