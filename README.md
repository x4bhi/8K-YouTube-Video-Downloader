# 🎥 8K YouTube Video Downloader (by Abhisht Pandey)

[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://microsoft.com/windows)
[![Build](https://img.shields.io/badge/Built%20with-C%23%20.NET-success)](#)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](#)

A beautiful, premium, and lightning-fast **8K YouTube Video Downloader** built in C# for Windows. This tool seamlessly combines the power of `yt-dlp` and `FFmpeg` behind a sleek, user-friendly graphical interface. 

It abstracts away complicated command-line arguments, offering you a simple 1-click download experience for **8K Ultra HD video** and **Lossless Audio** (FLAC/WAV).

---

## 📸 Screenshots

<p align="center">
  <img src="Photos/{3D9A4C15-B8BA-4AB1-81F6-3F03BF0DFD9A} 1.png" width="48%" />
  <img src="Photos/{5F7A7F00-E332-4C1C-91A2-7DE1A9DA9B6F} 1.png" width="48%" />
</p>
<p align="center">
  <img src="Photos/{9A89823A-3665-430C-A848-45277A71F0F0} 1.png" width="80%" />
</p>

---

## ✨ Features

- **Sleek & Modern UI**: A premium dark-mode interface built completely from scratch using custom C# WinForms rendering. No boring standard windows!
- **Zero Configuration**: No need to manually install Python, `yt-dlp`, or `ffmpeg`. The app automatically manages, embeds, and dynamically extracts its core engine files into a hidden stealth directory on the fly.
- **Ultimate Quality**: 
  - Download videos up to **8K (4320p)** with AV1, VP9, or H.264 codecs.
  - Download pristine audio at up to **320kbps MP3**, or completely **Lossless FLAC/WAV**.
- **Bulletproof Stability**:
  - Thread-safe UI updates.
  - An intelligent **Tree-Kill** mechanism that ensures background FFmpeg processes are perfectly killed without ghosting if you cancel a download.
  - Automated temporary file cleanup (`.part`, `.ytdl`) if a download is aborted.
  - A real-time Activity Log that scrubs out sensitive paths to protect user privacy.
- **Single Portable File**: Compiles down into a single `NexTube.exe`.

## 🚀 How to Build

If you want to compile this project yourself:
1. Ensure you have the .NET Framework 4.7.2 SDK installed (or Visual Studio).
2. Clone this repository.
3. Run **`setup_tools.bat`** (This will automatically download and extract `yt-dlp`, `ffmpeg`, and `ffprobe` into a `tools/` folder).
4. Run the included `build.bat` script.
5. The output will be located at `Release/NexTube.exe`.

*Note: The build script automatically compresses your `tools/` folder into `tools.zip` and embeds it directly into the executable as a hidden resource.*

## 🧑‍💻 Developer

Created with ❤️ by **Abhisht Pandey**.

## 🙌 Acknowledgements

This GUI application serves as a frontend wrapper and is powered entirely by the incredible work of the open-source community.
Special thanks to the core engine:
- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)**: The core download engine driving the media extraction. Without their amazing project, this tool would not be possible!
- **[FFmpeg](https://ffmpeg.org/)**: Used for high-quality audio and video stream multiplexing.

## 📝 License
This project is open-source and licensed under the [MIT License](LICENSE.txt).
