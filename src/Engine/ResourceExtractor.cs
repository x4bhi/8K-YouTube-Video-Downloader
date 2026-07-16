using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO.Compression;

namespace YouTubeDownloader
{
    //  RESOURCE EXTRACTOR
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    public static class ResourceExtractor
    {
        public static async Task<(string ytdlpPath, string ffmpegDir)> ExtractAsync(Action<string> statusCallback)
        {
            return await Task.Run(() =>
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var ffmpegDir = Path.Combine(appData, "Microsoft", "MediaServices");

                if (!Directory.Exists(ffmpegDir))
                    Directory.CreateDirectory(ffmpegDir);

                var ytdlpPath = Path.Combine(ffmpegDir, "yt-dlp.exe");
                var ffmpegPath = Path.Combine(ffmpegDir, "ffmpeg.exe");
                var ffprobePath = Path.Combine(ffmpegDir, "ffprobe.exe");

                if (File.Exists(ytdlpPath) && File.Exists(ffmpegPath) && File.Exists(ffprobePath))
                {
                    return (ytdlpPath, ffmpegDir);
                }

                statusCallback?.Invoke("Unpacking engine tools...");
                ExtractZip("tools.zip", ffmpegDir);

                return (ytdlpPath, ffmpegDir);
            });
        }

        private static void ExtractZip(string resourceName, string targetDir)
        {
            var assembly = typeof(DownloaderForm).Assembly;
            string foundResource = null;

            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.IndexOf(resourceName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundResource = name;
                    break;
                }
            }

            if (foundResource == null)
            {
                var localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
                if (File.Exists(localZip))
                {
                    ExtractZipFile(localZip, targetDir);
                    return;
                }
                throw new FileNotFoundException($"Cannot find embedded resource or local file for: {resourceName}");
            }

            string tempZip = Path.Combine(targetDir, "temp_tools.zip");

            using (var stream = assembly.GetManifestResourceStream(foundResource))
            {
                using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }

            ExtractZipFile(tempZip, targetDir);
            File.Delete(tempZip);
        }

        private static void ExtractZipFile(string zipPath, string targetDir)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // skip directories

                    string destinationPath = Path.Combine(targetDir, entry.Name);
                    
                    // Do not overwrite existing files to preserve yt-dlp auto-updates!
                    if (!File.Exists(destinationPath))
                    {
                        entry.ExtractToFile(destinationPath);
                    }
                }
            }
        }
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

}
