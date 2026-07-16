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
    //  YT-DLP ENGINE (Fully Robust Version)
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    public class YtDlpEngine : IDisposable
    {
        private readonly string _ytdlpPath;
        private readonly string _ffmpegPath;
        private readonly string _ffmpegDir;
        private Process _currentProcess;
        private readonly object _processLock = new object();
        private readonly List<string> _tempFiles = new List<string>();

        public event EventHandler<DownloadProgress> ProgressChanged;
        public event EventHandler<string> LogReceived;

        public YtDlpEngine(string ytdlpPath, string ffmpegDir)
        {
            _ytdlpPath = ytdlpPath;
            _ffmpegDir = ffmpegDir;
            _ffmpegPath = Path.Combine(ffmpegDir, "ffmpeg.exe");
        }

        /// <summary>Validates that yt-dlp and FFmpeg are functional.</summary>
        public async Task<OperationResult<bool>> ValidateToolsAsync()
        {
            try
            {
                // Check yt-dlp
                if (!File.Exists(_ytdlpPath))
                    return OperationResult<bool>.Fail("Engine not found at: " + _ytdlpPath);

                var ytdlpCheck = await RunProcessAsync("--version", CancellationToken.None, captureOutput: true, timeoutSeconds: 10);
                if (!ytdlpCheck.Success || ytdlpCheck.Data == null)
                    return OperationResult<bool>.Fail($"Engine failed version check.\n\nError: {ytdlpCheck.ErrorMessage}\n\nThis usually means your PC is missing the Microsoft Visual C++ Redistributable, or your Antivirus blocked the engine.");

                // Check FFmpeg
                if (!File.Exists(_ffmpegPath))
                    return OperationResult<bool>.Fail("Media encoder not found at: " + _ffmpegPath);

                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit(5000);
                    if (proc.ExitCode != 0)
                        return OperationResult<bool>.Fail("ffmpeg.exe validation failed");
                }

                return OperationResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Fail("Tool validation error: " + ex.Message);
            }
        }

        /// <summary>Analyzes URL and returns available formats with full metadata.</summary>
        public async Task<OperationResult<(List<MediaFormat> video, List<MediaFormat> audio)>> 
            AnalyzeFormatsAsync(string url, CancellationToken ct)
        {
            try
            {
                var args = $"--no-playlist -F \"{url}\"";
                var result = await RunProcessAsync(args, ct, captureOutput: true, timeoutSeconds: 60);

                if (!result.Success || result.Data == null)
                    return OperationResult<(List<MediaFormat>, List<MediaFormat>)>.Fail(
                        result.ErrorMessage ?? "Failed to retrieve format list");

                var (videoFormats, audioFormats) = ParseFormatsFromOutput(result.Data);

                if (videoFormats.Count == 0 && audioFormats.Count == 0)
                    return OperationResult<(List<MediaFormat>, List<MediaFormat>)>.Fail(
                        "No formats detected. The URL may be invalid or region-locked.");

                return OperationResult<(List<MediaFormat>, List<MediaFormat>)>.Ok((videoFormats, audioFormats));
            }
            catch (OperationCanceledException)
            {
                return OperationResult<(List<MediaFormat>, List<MediaFormat>)>.Fail("Analysis cancelled");
            }
            catch (Exception ex)
            {
                return OperationResult<(List<MediaFormat>, List<MediaFormat>)>.Fail(
                    "Analysis error: " + ex.Message);
            }
        }

        /// <summary>Downloads media with full progress tracking.</summary>
        public async Task<OperationResult<bool>> DownloadAsync(
            string url,
            string savePath,
            bool isAudio,
            MediaFormat selectedVideoFormat,
            MediaFormat selectedAudioFormat,
            bool isPlaylist,
            CancellationToken ct)
        {
            try
            {
                var args = BuildDownloadArguments(url, savePath, isAudio,
                    selectedVideoFormat, selectedAudioFormat, isPlaylist);

                LogReceived?.Invoke(this, $"[CMD] yt-dlp {args}");

                var result = await RunProcessAsync(args, ct, captureOutput: false, timeoutSeconds: 0);

                if (result.Success)
                    return OperationResult<bool>.Ok(true);
                else
                    return OperationResult<bool>.Fail(result.ErrorMessage ?? "Download failed");
            }
            catch (OperationCanceledException)
            {
                return OperationResult<bool>.Fail("Download cancelled");
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Fail("Download error: " + ex.Message);
            }
        }

        /// <summary>Updates yt-dlp to latest version.</summary>
        public async Task<OperationResult<bool>> UpdateAsync(CancellationToken ct)
        {
            try
            {
                LogReceived?.Invoke(this, "[UPDATE] Checking for updates...");
                var result = await RunProcessAsync("-U", ct, captureOutput: true, timeoutSeconds: 120);

                if (result.Success && result.Data != null)
                {
                    LogReceived?.Invoke(this, result.Data);
                    return OperationResult<bool>.Ok(true);
                }
                else
                {
                    return OperationResult<bool>.Fail(result.ErrorMessage ?? "Update failed");
                }
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Fail("Update error: " + ex.Message);
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  CORE PROCESS RUNNER (Race-condition free)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private async Task<OperationResult<string>> RunProcessAsync(
            string arguments,
            CancellationToken ct,
            bool captureOutput,
            int timeoutSeconds)
        {
            var tcs = new TaskCompletionSource<OperationResult<string>>();
            var outputBuilder = captureOutput ? new System.Text.StringBuilder() : null;
            Process process = null;
            bool isTimeout = false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ytdlpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                process = new Process { StartInfo = psi };

                // FIX: Hook events BEFORE starting to prevent race condition
                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        if (captureOutput)
                            outputBuilder.AppendLine(e.Data);
                        else
                            ProcessOutputLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        if (captureOutput)
                            outputBuilder.AppendLine(e.Data);
                        else
                            ProcessOutputLine(e.Data);
                    }
                };

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    try
                    {
                        if (ct.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled();
                            return;
                        }

                        var exitCode = process.ExitCode;
                        if (captureOutput)
                        {
                            var output = outputBuilder.ToString();
                            if (exitCode == 0)
                                tcs.TrySetResult(OperationResult<string>.Ok(output));
                            else
                                tcs.TrySetResult(OperationResult<string>.Fail($"Process exited with code {exitCode}"));
                        }
                        else
                        {
                            if (exitCode == 0 && !isTimeout)
                                tcs.TrySetResult(OperationResult<string>.Ok("success"));
                            else if (isTimeout)
                                tcs.TrySetResult(OperationResult<string>.Fail("Operation timed out"));
                            else
                                tcs.TrySetResult(OperationResult<string>.Fail($"Process failed with code {exitCode}"));
                        }
                    }
                    catch
                    {
                        tcs.TrySetResult(OperationResult<string>.Fail("Process exit handler error"));
                    }
                };

                lock (_processLock)
                {
                    _currentProcess = process;
                }

                // Register cancellation
                ct.Register(() =>
                {
                    try
                    {
                        lock (_processLock)
                        {
                            if (_currentProcess != null && !_currentProcess.HasExited)
                            {
                                // Kill the entire process tree (yt-dlp and its ffmpeg children)
                                try
                                {
                                    using (var killer = Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/T /F /PID {_currentProcess.Id}",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    }))
                                    {
                                        killer.WaitForExit();
                                    }
                                }
                                catch
                                {
                                    // Fallback to basic kill
                                    _currentProcess.Kill();
                                }
                                
                                tcs.TrySetCanceled();
                            }
                        }
                    }
                    catch { }
                });

                // Start process (events already hooked)
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Implement timeout if specified
                Task timeoutTask = null;
                if (timeoutSeconds > 0)
                {
                    timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                }

                if (timeoutTask != null)
                {
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                    if (completedTask == timeoutTask && !ct.IsCancellationRequested)
                    {
                        isTimeout = true;
                        lock (_processLock)
                        {
                            if (_currentProcess != null && !_currentProcess.HasExited)
                                _currentProcess.Kill();
                        }
                        return OperationResult<string>.Fail($"Operation timed out after {timeoutSeconds} seconds");
                    }
                }

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                return OperationResult<string>.Fail("Operation cancelled");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail("Process error: " + ex.Message);
            }
            finally
            {
                lock (_processLock)
                {
                    try
                    {
                        if (process != null && !process.HasExited)
                            process.Kill();
                    }
                    catch { }

                    process?.Dispose();
                    _currentProcess = null;
                }
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  ENHANCED FORMAT PARSER (No deduplication issues)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private (List<MediaFormat> video, List<MediaFormat> audio) ParseFormatsFromOutput(string output)
        {
            var videoList = new List<MediaFormat>();
            var audioList = new List<MediaFormat>();

            // FIX: Use Lists, not Dictionaries - preserve ALL formats
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith("[") ||
                    line.Contains("Available formats") ||
                    line.Contains("â”€â”€â”€") ||
                    line.StartsWith("ID ") ||
                    line.StartsWith("---") ||
                    line.Contains("format code"))
                    continue;

                // Audio-only formats
                if (line.Contains("audio only"))
                {
                    var format = ParseAudioFormat(line);
                    if (format != null)
                    {
                        format.CalculateQualityScore();
                        audioList.Add(format);
                    }
                }
                // Video formats (may include audio)
                else
                {
                    var format = ParseVideoFormat(line);
                    if (format != null && format.Height > 0)
                    {
                        format.CalculateQualityScore();
                        videoList.Add(format);
                    }
                }
            }

            // FIX: Smart deduplication - keep BEST format per resolution
            var uniqueVideo = videoList
                .GroupBy(f => f.Height)
                .Select(g => g.OrderByDescending(f => f.QualityScore).First())
                .OrderByDescending(f => f.QualityScore)
                .ToList();

            var uniqueAudio = audioList
                .GroupBy(f => f.Bitrate)
                .Select(g => g.OrderByDescending(f => f.QualityScore).First())
                .OrderByDescending(f => f.Bitrate)
                .ToList();

            return (uniqueVideo, uniqueAudio);
        }

        private MediaFormat ParseVideoFormat(string line)
        {
            try
            {
                // Extract format ID (first column)
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return null;

                string formatId = parts[0];

                // Extract resolution
                int height = 0, width = 0;
                var resMatch = Regex.Match(line, @"\b(\d{3,4})x(\d{3,4})\b");
                if (resMatch.Success)
                {
                    int.TryParse(resMatch.Groups[1].Value, out width);
                    int.TryParse(resMatch.Groups[2].Value, out height);
                }
                else
                {
                    var pMatch = Regex.Match(line, @"\b(\d{3,4})[pP]\b");
                    if (pMatch.Success)
                        int.TryParse(pMatch.Groups[1].Value, out height);
                }

                if (height == 0) return null;

                // Extract FPS
                int fps = 30; // default
                var fpsMatch = Regex.Match(line, @"(\d+)fps");
                if (fpsMatch.Success)
                    int.TryParse(fpsMatch.Groups[1].Value, out fps);

                // Extract codec
                string codec = "h264"; // default
                if (line.Contains("av01") || line.Contains("av1"))
                    codec = "av01";
                else if (line.Contains("vp9") || line.Contains("vp09"))
                    codec = "vp9";
                else if (line.Contains("avc1") || line.Contains("h264"))
                    codec = "avc1";

                return new MediaFormat
                {
                    FormatId = formatId,
                    Height = height,
                    Width = width,
                    Fps = fps,
                    VideoCodec = codec,
                    IsAudioOnly = false,
                    DisplayLabel = HeightToLabel(height, codec, fps)
                };
            }
            catch
            {
                return null;
            }
        }

        private MediaFormat ParseAudioFormat(string line)
        {
            try
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return null;

                string formatId = parts[0];

                // Enhanced bitrate detection
                var bitrateMatch = Regex.Match(line, @"~?(\d+)[kK]");
                if (!bitrateMatch.Success) return null;

                int bitrate = int.Parse(bitrateMatch.Groups[1].Value);

                // Accurate codec detection
                string codec = "aac";
                if (line.IndexOf("opus", StringComparison.OrdinalIgnoreCase) >= 0)
                    codec = "opus";
                else if (line.IndexOf("vorbis", StringComparison.OrdinalIgnoreCase) >= 0)
                    codec = "vorbis";
                else if (line.IndexOf("mp4a", StringComparison.OrdinalIgnoreCase) >= 0)
                    codec = "aac";

                return new MediaFormat
                {
                    FormatId = formatId,
                    Bitrate = bitrate,
                    AudioCodec = codec,
                    IsAudioOnly = true,
                    DisplayLabel = $"MP3 â€” {bitrate} kbps  (source: {codec.ToUpper()})"
                };
            }
            catch
            {
                return null;
            }
        }

        private string HeightToLabel(int height, string codec, int fps)
        {
            string codecLabel = codec switch
            {
                "av01" => "AV1",
                "vp9" or "vp09" => "VP9",
                _ => "H.264"
            };

            string fpsLabel = fps > 30 ? $" {fps}fps" : "";

            return height switch
            {
                4320 => $"8K Ultra HD  (4320p{fpsLabel}, {codecLabel})",
                2160 => $"4K Ultra HD  (2160p{fpsLabel}, {codecLabel})",
                1440 => $"2K Quad HD   (1440p{fpsLabel}, {codecLabel})",
                1080 => $"Full HD      (1080p{fpsLabel}, {codecLabel})",
                720 => $"HD           (720p{fpsLabel}, {codecLabel})",
                480 => $"SD           (480p{fpsLabel}, {codecLabel})",
                360 => $"Low          (360p{fpsLabel}, {codecLabel})",
                240 => $"Very Low     (240p{fpsLabel}, {codecLabel})",
                144 => $"Lowest       (144p{fpsLabel}, {codecLabel})",
                _ => $"Custom       ({height}p{fpsLabel}, {codecLabel})"
            };
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  FIXED ARGUMENT BUILDER
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private string BuildDownloadArguments(
            string url,
            string savePath,
            bool isAudio,
            MediaFormat selectedVideoFormat,
            MediaFormat selectedAudioFormat,
            bool isPlaylist)
        {
            var outputTemplate = $"-o \"{savePath}\\%(title)s.%(ext)s\"";
            var playlistFlag = isPlaylist ? "--yes-playlist" : "--no-playlist";
            var ffmpegFlag = $"--ffmpeg-location \"{_ffmpegDir}\"";

            if (isAudio)
            {
                var audioLabel = selectedAudioFormat?.DisplayLabel ?? "Best Audio (Auto)";

                // Lossless FLAC
                if (audioLabel.Contains("FLAC"))
                    return $"-x --audio-format flac --audio-quality 0 {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";

                // Lossless WAV
                if (audioLabel.Contains("WAV"))
                    return $"-x --audio-format wav --audio-quality 0 {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";

                // FIX #2: Best Audio (Auto) now ALWAYS converts to MP3 to match UI expectations
                if (audioLabel.Contains("Best Audio (Auto)"))
                    return $"-x --audio-format mp3 --audio-quality 0 {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";

                // Specific bitrate MP3
                var bitrateMatch = Regex.Match(audioLabel, @"(\d+)\s*kbps");
                if (bitrateMatch.Success)
                {
                    var quality = bitrateMatch.Groups[1].Value + "K";
                    return $"-x --audio-format mp3 --audio-quality {quality} {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";
                }

                // Fallback: best MP3
                return $"-x --audio-format mp3 --audio-quality 0 {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";
            }
            else
            {
                var videoLabel = selectedVideoFormat?.DisplayLabel ?? "Best Video + Best Audio (Auto)";
                string formatArg = "bv*+ba/b";

                // If user selected specific quality, use height-based selector
                var heightMatch = Regex.Match(videoLabel, @"\((\d+)p");
                if (heightMatch.Success)
                {
                    var height = heightMatch.Groups[1].Value;
                    formatArg = $"bestvideo[height<={height}]+bestaudio/best";
                }

                return $"-f \"{formatArg}\" {playlistFlag} \"{url}\" {outputTemplate} {ffmpegFlag}";
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  ENHANCED PROGRESS PARSER
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ProcessOutputLine(string line)
        {
            // Emit to log for user-friendly lines
            if (line.Contains("[download]") || line.Contains("[Merger]") ||
                line.Contains("[ExtractAudio]") || line.Contains("[ffmpeg]") ||
                line.Contains("[info]") || line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LogReceived?.Invoke(this, line);
            }

            var progress = new DownloadProgress();

            // Download progress with enhanced parsing
            if (line.Contains("[download]"))
            {
                var destMatch = Regex.Match(line, @"Destination:\s+(.+)");
                if (destMatch.Success)
                {
                    lock (_processLock) { _tempFiles.Add(destMatch.Groups[1].Value.Trim()); }
                }

                var pctMatch = Regex.Match(line, @"(\d+(?:\.\d+)?)%");
                if (pctMatch.Success)
                {
                    float.TryParse(pctMatch.Groups[1].Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float pct);
                    progress.Percentage = Math.Min(100, (int)pct);

                    var speedMatch = Regex.Match(line, @"at\s+(\S+)\s+ETA\s+(\S+)");
                    if (speedMatch.Success)
                    {
                        progress.Speed = speedMatch.Groups[1].Value;
                        progress.ETA = speedMatch.Groups[2].Value;
                        progress.StatusMessage = $"Downloading  {pct:F1}%   Â·   {progress.Speed}   Â·   ETA {progress.ETA}";
                    }
                    else
                    {
                        progress.StatusMessage = $"Downloading  {pct:F1}%";
                    }
                    progress.Stage = "download";
                }
                else if (line.Contains("has already been downloaded"))
                {
                    progress.Percentage = 100;
                    progress.StatusMessage = "File already exists (skipped).";
                    progress.Stage = "complete";
                }
                else if (line.Contains("Destination") || line.Contains("Downloading"))
                {
                    progress.Percentage = 0;
                    progress.StatusMessage = "Initializing download...";
                    progress.Stage = "starting";
                }
            }
            // Merging stage
            else if (line.Contains("[Merger]") || line.Contains("Merging"))
            {
                var mergeMatch = Regex.Match(line, @"Merging formats into\s+""([^""]+)""");
                if (mergeMatch.Success)
                {
                    lock (_processLock) { _tempFiles.Add(mergeMatch.Groups[1].Value.Trim()); }
                }

                progress.Percentage = 95;
                progress.StatusMessage = "Merging video + audio streams...";
                progress.Stage = "merge";
            }
            // Audio extraction
            else if (line.Contains("[ExtractAudio]") || line.Contains("Extracting audio"))
            {
                progress.Percentage = 90;
                progress.StatusMessage = "Extracting and converting audio...";
                progress.Stage = "extract";
            }
            // FFmpeg processing
            else if (line.Contains("[ffmpeg]"))
            {
                progress.Percentage = 85;
                progress.StatusMessage = "Post-processing with FFmpeg...";
                progress.Stage = "ffmpeg";
            }

            if (!string.IsNullOrEmpty(progress.StatusMessage))
                ProgressChanged?.Invoke(this, progress);
        }

        public void CleanupTempFiles()
        {
            lock (_processLock)
            {
                foreach (var file in _tempFiles.Distinct())
                {
                    try
                    {
                        if (File.Exists(file)) File.Delete(file);
                        if (File.Exists(file + ".part")) File.Delete(file + ".part");
                        if (File.Exists(file + ".ytdl")) File.Delete(file + ".ytdl");
                    }
                    catch { }
                }
                _tempFiles.Clear();
            }
        }

        public void Dispose()
        {
            lock (_processLock)
            {
                try
                {
                    if (_currentProcess != null && !_currentProcess.HasExited)
                        _currentProcess.Kill();
                }
                catch { }

                _currentProcess?.Dispose();
                _currentProcess = null;
            }
        }
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

}
