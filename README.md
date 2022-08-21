# YourWeatherInfo-Functions

Angular Weather App Functions Backend using API from https://www.weatherapi.com/

## Development server

###### Using Visual Studio
Open YourWeatherInfo-Functions.sln in Visual Studio and the Azurite Storage Emulator should launch automatically.
Click Start Debugging and the server should be listening on `http://localhost:7071/`.
###### Linux/CLI
Install azurite storage emulator
`npm install -g azurite`
Startup the Azurite service in the provided azurite_workspace directory
`azurite --silent --location azurite_workspace/ --debug azurite_workspace/debug.log`
Run the azure function project using the command
`func start`

## Local Settings

Included an Azure Functions localsettings file for use with the Azurite emulator which supports a single fixed account and a well-known authentication key for Shared Key authentication. 
This account and key are the only Shared Key credentials permitted for use with the emulator.

## Config

Create an account at https://www.weatherapi.com/ and add your API Key as the key query string parameter in the C# Interface file
`IWeatherApi.cs`
