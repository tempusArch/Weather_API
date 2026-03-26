using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Text.Json;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase {
    private readonly ILogger<WeatherController> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IDatabase _redisDatabase;
    
    public WeatherController(ILogger<WeatherController> logger,
        HttpClient httpClient,
        IConfiguration config,
        IConnectionMultiplexer redisConnectionMultiplexer) 
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config;
        _redisDatabase = redisConnectionMultiplexer.GetDatabase();
    }

    [HttpGet(Name = "GetWeather")]
    public async Task<IActionResult> Get([FromQuery(Name = "city")] string city) {
        try {
            string result = string.Empty;
            
            if (string.IsNullOrEmpty(city))
                return BadRequest("City name can not be empty.");
            
            if (_redisDatabase.KeyExists($"wether:{city}")) {
                var cachedWeatherIntel = await _redisDatabase.StringGetAsync($"weather:{city}");
                if (cachedWeatherIntel.HasValue)
                    return Ok(JsonSerializer.Deserialize<object>(cachedWeatherIntel));

            } else {
                var query = new Dictionary<string, string> 
                { {
                    "key", _config["WEATHER_API_KEY"] ?? string.Empty
                } };

                var url = QueryHelpers.AddQueryString(
                    $"https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/{city}",
                    query
                );

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return StatusCode(500);
                
                result = await response.Content.ReadAsStringAsync();

                await _redisDatabase.StringSetAsync($"weather:{city}", result, TimeSpan.FromHours(12));
            }

            return Ok(JsonSerializer.Deserialize<object>(result));

        } catch (Exception e) {
            _logger.LogError(e.Message);
            throw;
        }
    }
}
