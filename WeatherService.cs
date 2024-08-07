﻿using Microsoft.AspNetCore.Mvc;
using NodeMcuDeneme;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private float temperature;
    private float humidity;
    private float soilMoisture;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherData> GetWeatherDataAsync()
    {
        string url = "http://192.168.43.87";
        var response = await _httpClient.GetStringAsync(url);
        Console.WriteLine(response); // Gelen cevabı kontrol etme

        var lines = response.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var temperatureLine = lines.FirstOrDefault(line => line.Contains("Temperature:"));
        var humidityLine = lines.FirstOrDefault(line => line.Contains("Humidity:"));
        var soilMoistureLine = lines.FirstOrDefault(line => line.Contains("Soil Moisture:"));


        if (temperatureLine != null && humidityLine != null && soilMoistureLine != null)
        {
            string temperatureString = temperatureLine.Split(' ')[1].Replace('Â', ' ').Trim();
            string humidityString = humidityLine.Split(' ')[1].Replace('Â', ' ').Trim();

            // Soil moisture line may have unexpected characters, clean them
            string soilMoistureString = soilMoistureLine.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries)[2];
            soilMoistureString = soilMoistureString.Replace('½', ' ').Trim();

            temperature = float.Parse(temperatureString, CultureInfo.InvariantCulture);
            humidity = float.Parse(humidityString, CultureInfo.InvariantCulture);
            soilMoisture = float.Parse(soilMoistureString, CultureInfo.InvariantCulture);

            return new WeatherData
            {
                Temperature = temperature,
                Humidity = humidity,
                SoilMoisture = soilMoisture
            };
        }

        return null;
    }

    public async Task TurnRelayOnAsync()
    {
        string url = "http://192.168.43.87/relay/on";
        await _httpClient.GetStringAsync(url);
    }

    public async Task TurnRelayOffAsync()
    {
        string url = "http://192.168.43.87/relay/off";
        await _httpClient.GetStringAsync(url);
    }

    public async Task TurnBuzzerOnAsync()
    {
            string url = "http://192.168.43.87/buzzer/on";
            await _httpClient.GetStringAsync(url);
    }

    public async Task TurnBuzzerOffAsync()
    {

        string url = "http://192.168.43.87/buzzer/off";
        await _httpClient.GetStringAsync(url);

    }

    public async Task TurnRedLedOffAsync()
    {

        string url = "http://192.168.43.87/redled/off";
        await _httpClient.GetStringAsync(url);

    }
    public async Task TurnRedLedOnAsync()
    {

        string url = "http://192.168.43.87/redled/on";
        await _httpClient.GetStringAsync(url);

    }
    public async Task TurnGreenLedOnAsync()
    {

        string url = "http://192.168.43.87/greenled/on";
        await _httpClient.GetStringAsync(url);

    }

    public async Task TurnGreenLedOffAsync()
    {

        string url = "http://192.168.43.87/greenled/off";
        await _httpClient.GetStringAsync(url);

    }

}