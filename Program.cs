using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using NodeMcuDeneme.Controllers;
using NodeMcuDeneme.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddTransient<WeatherService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Google");
    options.ClientId = googleAuthNSection["ClientId"];
    options.ClientSecret = googleAuthNSection["ClientSecret"];
    options.CallbackPath = "/signin-google";
});

builder.Services.AddSingleton<GoogleDriveService>();

var app = builder.Build();

// Resolve necessary services
var logger = app.Services.GetRequiredService<ILogger<HomeController>>();
var weatherService = app.Services.GetRequiredService<WeatherService>();
var env = app.Services.GetRequiredService<IWebHostEnvironment>();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

// Create instance of HomeController
var homeController = new HomeController(weatherService, env, httpClientFactory, logger);

// Start the background task
var thread = new Thread(() => StartBackgroundTask(homeController, app.Services.GetRequiredService<GoogleDriveService>()));
thread.Start();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Background task function
void StartBackgroundTask(HomeController homeController, GoogleDriveService service)
{
    while (true)
    {
        // Wait for a certain period
        Thread.Sleep(TimeSpan.FromSeconds(5));
        string folderId = "1MI-biSbcfcQQ5iWEimj3vChoiPoxnW07"; // Klasör ID'sini buraya yazýn
        string credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        string tokenPath = Path.Combine(Directory.GetCurrentDirectory(), "token.json");
        string filePath = service.DownloadLatestImageAsync(folderId, credentialsPath, tokenPath).GetAwaiter().GetResult();
        if (filePath != null)
        {
            Console.WriteLine($"Downloaded file to {filePath}");
            // Make prediction
            homeController.PredictLatestImage().GetAwaiter().GetResult();
        }
        else
        {
            Console.WriteLine("No files found.");
        }
    }
}
