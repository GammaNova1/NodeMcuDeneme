﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeMcuDeneme.Services
{
    public class GoogleDriveService
    {
        static string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "ESP32-CAM Image Downloader";

        public async Task<string> DownloadLatestImageAsync(string folderId, string credentialsPath, string tokenPath)
        {
            UserCredential credential;

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true));
            }

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents";
            request.OrderBy = "createdTime desc";
            request.PageSize = 1;
            request.Fields = "files(id, name, createdTime)";

            var result = await request.ExecuteAsync();
            var file = result.Files.FirstOrDefault();

            if (file != null)
            {
                var fileId = file.Id;
                var fileName = file.Name;
                var outputPath = Path.Combine(Environment.CurrentDirectory, "DownloadedImages", fileName);
                var wwwrootPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "images", fileName);

                var getRequest = service.Files.Get(fileId);
                using (var stream = new MemoryStream())
                {
                    await getRequest.DownloadAsync(stream);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    await File.WriteAllBytesAsync(outputPath, stream.ToArray());
                    Directory.CreateDirectory(Path.GetDirectoryName(wwwrootPath));
                    await File.WriteAllBytesAsync(wwwrootPath, stream.ToArray());
                    return outputPath;
                }
            }
            else
            {
                return null;
            }
        }

    }
}