using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NodeMcuDeneme.Services;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public class HomeController : Controller
{
    private readonly WeatherService _weatherService;
    //private readonly GoogleDriveService _googleDriveService;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<HomeController> _logger;
    private readonly GoogleDriveService _googleDriveService;


    public HomeController(WeatherService weatherService, IWebHostEnvironment env, IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _weatherService = weatherService;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _googleDriveService = new GoogleDriveService();
    }

    public async Task<IActionResult> Index()
    {
        
        var weatherData = await _weatherService.GetWeatherDataAsync();
        return View(weatherData);
    }

    [HttpPost]
    public async Task<IActionResult> TurnRelayOn()
    {
        await _weatherService.TurnRelayOnAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> TurnRelayOff()
    {
        await _weatherService.TurnRelayOffAsync();
        return Json(new { success = true });
    }


    [HttpGet]
  
    public async Task<IActionResult> GetWeatherData()
    {
        var weatherData = await _weatherService.GetWeatherDataAsync();
        return Json(weatherData);
    }


    [HttpPost("UploadPhoto")]
    public async Task<IActionResult> UploadPhoto()
    {
        try
        {
            var directory = new DirectoryInfo("C:\\Users\\ahmet\\source\\repos\\NodeMcuDeneme\\DownloadedImages");
            var myFile = directory.GetFiles()
                                  .OrderByDescending(f => f.LastWriteTime)
                                  .FirstOrDefault();

            if (myFile == null)
            {
                return BadRequest("No files found in the specified directory.");
            }

            var predictionResult = await PredictImage(myFile.FullName);

            return Ok(new { message = "Photo processed successfully.", prediction = predictionResult });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing photo");
            return StatusCode(500, $"Internal server error: {ex}");
        }
    }

    private async Task<string> PredictImage(string imagePath)
    {
        var client = _httpClientFactory.CreateClient();
        var form = new MultipartFormDataContent();

        using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
        {
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "path", Path.GetFileName(imagePath));

            var response = await client.PostAsync("http://0.0.0.0:5000/predict", form);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseString);

            return jsonResponse["predicted_label"].ToString();
        }
    }

}
