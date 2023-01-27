﻿using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

using Bloxstrap.Models;
using System;

namespace Bloxstrap.Helpers.Integrations
{
    internal class RbxFpsUnlocker
    {
        public const string ApplicationName = "rbxfpsunlocker";
        public const string ProjectRepository = "axstin/rbxfpsunlocker";

        // default settings but with QuickStart set to true and CheckForUpdates set to false
        private static readonly string Settings = 
            "UnlockClient=true\n" +
            "UnlockStudio=false\n" +
            "FPSCapValues=[30.000000, 60.000000, 75.000000, 120.000000, 144.000000, 165.000000, 240.000000, 360.000000]\n" +
            "FPSCapSelection=0\n" +
            "FPSCap=0.000000\n" +
            "CheckForUpdates=false\n" +
            "NonBlockingErrors=true\n" +
            "SilentErrors=false\n" +
            "QuickStart=true\n";

        public static void CheckIfRunning()
        {
            Process[] processes = Process.GetProcessesByName(ApplicationName);

            if (processes.Length == 0)
                return;
            
            try
            {
                // try/catch just in case process was closed before prompt was answered

                foreach (Process process in processes)
                {
                    if (process.MainModule is null || process.MainModule.FileName is null)
                        continue;

                    if (!process.MainModule.FileName.Contains(App.BaseDirectory))
                        continue;

                    process.Kill();
                    process.Close();
                }
            }
            catch (Exception) { }
        }

        public static async Task CheckInstall()
        {
            if (App.BaseDirectory is null)
                return;

            string folderLocation = Path.Combine(App.BaseDirectory, "Integrations\\rbxfpsunlocker");
            string fileLocation = Path.Combine(folderLocation, "rbxfpsunlocker.exe");
            string settingsLocation = Path.Combine(folderLocation, "settings");

            if (!App.Settings.RFUEnabled)
            {
                if (Directory.Exists(folderLocation))
                {
                    CheckIfRunning();
                    Directory.Delete(folderLocation, true);
                }

                return;
            }

            var releaseInfo = await Utilities.GetJson<GithubRelease>($"https://api.github.com/repos/{ProjectRepository}/releases/latest");

            if (releaseInfo is null || releaseInfo.Assets is null)
                return;

            string downloadUrl = releaseInfo.Assets[0].BrowserDownloadUrl;

            Directory.CreateDirectory(folderLocation);

            if (File.Exists(fileLocation))
            {
                // no new release published, return
                if (App.Settings.RFUVersion == releaseInfo.TagName)
                    return;

                CheckIfRunning();
                File.Delete(fileLocation);
            }

            Debug.WriteLine("Installing/Updating rbxfpsunlocker...");

            {
                byte[] bytes = await App.HttpClient.GetByteArrayAsync(downloadUrl);

                using MemoryStream zipStream = new(bytes);
                using ZipArchive archive = new(zipStream);

                archive.ExtractToDirectory(folderLocation, true);
            }

            if (!File.Exists(settingsLocation))
                await File.WriteAllTextAsync(settingsLocation, Settings);

            App.Settings.RFUVersion = releaseInfo.TagName;
        }
    }
}
