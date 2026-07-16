Write-Host "Setting up NexTube build tools..." -ForegroundColor Cyan

if (-not (Test-Path "tools")) {
    New-Item -ItemType Directory -Force -Path "tools" | Out-Null
}

$ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$ytDlpPath = "tools\yt-dlp.exe"
if (-not (Test-Path $ytDlpPath)) {
    Write-Host "Downloading yt-dlp.exe..."
    Invoke-WebRequest -Uri $ytDlpUrl -OutFile $ytDlpPath
} else {
    Write-Host "yt-dlp.exe already exists." -ForegroundColor Green
}

$ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
$ffmpegZip = "tools\ffmpeg.zip"
$ffmpegTemp = "tools\ffmpeg_temp"

$ffmpegPath = "tools\ffmpeg.exe"
$ffprobePath = "tools\ffprobe.exe"

if (-not (Test-Path $ffmpegPath) -or -not (Test-Path $ffprobePath)) {
    Write-Host "Downloading FFmpeg (this may take a minute)..."
    Invoke-WebRequest -Uri $ffmpegUrl -OutFile $ffmpegZip
    
    Write-Host "Extracting FFmpeg..."
    Expand-Archive -Path $ffmpegZip -DestinationPath $ffmpegTemp -Force
    
    $binFolder = (Get-ChildItem -Path $ffmpegTemp -Directory | Select-Object -First 1).FullName + "\bin"
    
    Copy-Item "$binFolder\ffmpeg.exe" "tools\ffmpeg.exe" -Force
    Copy-Item "$binFolder\ffprobe.exe" "tools\ffprobe.exe" -Force
    
    Write-Host "Cleaning up FFmpeg temp files..."
    Remove-Item $ffmpegZip -Force
    Remove-Item $ffmpegTemp -Recurse -Force
} else {
    Write-Host "ffmpeg.exe and ffprobe.exe already exist." -ForegroundColor Green
}

Write-Host "All tools are successfully downloaded and placed in the tools/ folder!" -ForegroundColor Green
Write-Host "You can now safely run build.bat" -ForegroundColor Cyan
