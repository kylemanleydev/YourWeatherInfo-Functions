using RestEase;
using System.Threading.Tasks;

namespace YourWeatherInfo_Functions
{
    public interface IWeatherApi
    {
        [Get("v1/current.json?key=5e8bd5b4258b4f05a6111158220707&q={zipcode}&aqi=no")]
        Task<WeatherData> GetWeatherDataAsync([Path] string zipcode);
    }
}
