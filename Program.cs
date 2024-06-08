using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodeMcuDeneme.Services;
using System;
using System.IO;
using System.Threading;
using NodeMcuDeneme.Services;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddHttpClient();
        services.AddTransient<WeatherService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddGoogle(options =>
        {
            IConfigurationSection googleAuthNSection = Configuration.GetSection("Google");
            options.ClientId = googleAuthNSection["ClientId"];
            options.ClientSecret = googleAuthNSection["ClientSecret"];
            options.CallbackPath = "/signin-google";
        });

        services.AddSingleton<GoogleDriveService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, GoogleDriveService googleDriveService)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        var thread = new Thread(() => StartBackgroundTask(googleDriveService));
        thread.Start();
    }

    private void StartBackgroundTask(GoogleDriveService service)
    {
        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            string folderId = "1MI-biSbcfcQQ5iWEimj3vChoiPoxnW07"; // Klasör ID'sini buraya yazýn
            string credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
            string tokenPath = Path.Combine(Directory.GetCurrentDirectory(), "token.json");
            string filePath = service.DownloadLatestImageAsync(folderId, credentialsPath, tokenPath).GetAwaiter().GetResult();
            if (filePath != null)
            {
                Console.WriteLine($"Downloaded file to {filePath}");
            }
            else
            {
                Console.WriteLine("No files found.");
            }
        }
    }
}
