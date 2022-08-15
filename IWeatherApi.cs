using RestEase;
using System.Threading.Tasks;
using System;

namespace YourWeatherInfo_Functions
{
    public interface IWeatherApi
    {
        //set api key from weatherapi.com as the key query string parameter in URL below
        [Get("v1/current.json?key=5e8bd5b4258b4f05a6111158220707&q={zipcode}&aqi=no")]
        Task<WeatherData> GetWeatherDataAsync([Path] string zipcode);
    }
}
