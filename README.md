# YourWeatherInfo-Functions

Angular Weather App Functions Backend using API from https://www.weatherapi.com/

## Development server

Open YourWeatherInfo-Functions.sln in Visual Studio and the Azurite Storage Emulator should launch automatically.
Click Start Debugging and the server should be listening on `http://localhost:7071/`.

## Local Settings

Included an Azure Functions localsettings file for use with the Azurite emulator which supports a single fixed account and a well-known authentication key for Shared Key authentication. 
This account and key are the only Shared Key credentials permitted for use with the emulator.

## Config

Create an account at https://www.weatherapi.com/ and add your API Key as the key query string parameter in the C# Interface file
IWeatherApi.cs
