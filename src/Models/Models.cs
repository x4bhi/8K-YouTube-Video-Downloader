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
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  DATA MODELS
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    /// <summary>Represents a video/audio format with complete metadata.</summary>
    public class MediaFormat
    {
        public string FormatId { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Fps { get; set; }
        public int Bitrate { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public string Extension { get; set; }
        public bool IsAudioOnly { get; set; }
        public string DisplayLabel { get; set; }
        public int QualityScore { get; set; } // For sorting

        public override string ToString() => DisplayLabel;

        /// <summary>Calculate quality score for intelligent sorting.</summary>
        public void CalculateQualityScore()
        {
            if (IsAudioOnly)
            {
                QualityScore = Bitrate;
            }
            else
            {
                // Video score: resolution Ã— fps Ã— codec multiplier
                int codecMultiplier = VideoCodec switch
                {
                    "av01" or "av1" => 3,      // AV1 = best
                    "vp9" or "vp09" => 2,      // VP9 = good
                    "avc1" or "h264" => 1,     // H.264 = baseline
                    _ => 1
                };
                QualityScore = Height * (Fps > 0 ? Fps : 30) * codecMultiplier;
            }
        }
    }

    /// <summary>Download progress with detailed stage tracking.</summary>
    public class DownloadProgress
    {
        public int Percentage { get; set; }
        public string Speed { get; set; }
        public string ETA { get; set; }
        public string Stage { get; set; }
        public string StatusMessage { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
    }

    /// <summary>Result container for operations.</summary>
    public class OperationResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }

        public static OperationResult<T> Ok(T data) => new OperationResult<T> { Success = true, Data = data };
        public static OperationResult<T> Fail(string error) => new OperationResult<T> { Success = false, ErrorMessage = error };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

}
