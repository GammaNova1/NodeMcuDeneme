namespace NodeMcuDeneme.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NodeMcuDeneme.Models;
using NodeMcuDeneme.Services;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Logging;
using System.Linq;
using MimeKit;
using MailKit.Net.Smtp;

public class HomeController : Controller
{
    private readonly WeatherService _weatherService;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private static string _customerMail = "ouzxboy25@gmail.com";

    private readonly ILogger<HomeController> _logger;
    private readonly GoogleDriveService _googleDriveService;
    private readonly HttpClient _httpClient;
    private static string _predictedLabel = "No prediction yet."; // Predicted label
    private string previousPredict = "";
    private static string _latestImagePath = null; // En son indirilen resmin yolu

    public HomeController(WeatherService weatherService, IWebHostEnvironment env, IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _weatherService = weatherService;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _googleDriveService = new GoogleDriveService();
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("http://localhost:5000/");
    }

    public async Task<IActionResult> Index()
    {
      
        ViewBag.PredictedLabel = _predictedLabel;
        ViewBag.LatestImage = _latestImagePath != null ? $"/images/{Path.GetFileName(_latestImagePath)}" : null; // Resim yolu

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

    private IActionResult SendMail(string passwordCode)
    {
        try
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress("Admin", "yibifur2002@gmail.com"));
            mimeMessage.To.Add(new MailboxAddress("User", _customerMail));
            mimeMessage.Subject = "Bitkiniz Hastalandı!";
            mimeMessage.Body = new TextPart("plain")
            {
                Text = $"Bitkinizde problem var. Lütfen kontrol ediniz! \n\n Tespit edilen hastalık : {_predictedLabel}"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("yibifur2002@gmail.com", "pkhw mbjl rrkv hdeo");
                client.Send(mimeMessage);
                client.Disconnect(true);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email: {ex.Message}");
            return StatusCode(500, "An error occurred while sending email.");
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetWeatherData()
    {
        var weatherData = await _weatherService.GetWeatherDataAsync();
        return Json(weatherData);
    }
    private string GetLatestFile(string directoryPath)
    {
        var directory = new DirectoryInfo(directoryPath);
        var latestFile = directory.GetFiles()
                                  .OrderByDescending(f => f.LastWriteTime)
                                  .FirstOrDefault();
       // ViewBag.LatestFile = latestFile;
        return latestFile?.FullName;
    }


    public async Task PredictLatestImage()
    {
        string imagePath = GetLatestFile("C:\\Users\\ahmet\\source\\repos\\NodeMcuDeneme\\DownloadedImages\\");

        if (imagePath != null)
        {
            _latestImagePath = imagePath;
            using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fileStream), "image", Path.GetFileName(imagePath));
                try
                {
                    HttpResponseMessage response = await _httpClient.PostAsync("predict", content);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        _predictedLabel = result;
                       // ViewBag.predictedLabel = _predictedLabel;
                        // Eğer tahmin "Tomato___healthy" dışında bir şeyse mail gönder
                        if (_predictedLabel != "{\"predicted_label\":\"Tomato___healthy\"}\n")
                        {
                            if(previousPredict != _predictedLabel)
                            {

                                SendMail("YourPasswordCode");// PasswordCode buraya gelecek
                                previousPredict = _predictedLabel;
                            }
                            
                        }
                    }
                    else
                    {
                        _predictedLabel = "Error: Unable to get prediction.";
                    }
                }
                catch (Exception)
                {
                    // Log exception if necessary
                }
            }
        }
        else
        {
            _predictedLabel = "No image found.";
        }
    }

    [HttpGet]
    public IActionResult GetPredictedLabel()
    {
        return Content(_predictedLabel);
    }

    [HttpGet]
    public IActionResult GetLatestImage()
    {
        string latestImage = _latestImagePath != null ? $"/images/{Path.GetFileName(_latestImagePath)}" : null;
        return Content(latestImage);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
