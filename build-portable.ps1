# KDP Category Ranker - Portable Build Script
# Creates a single-file executable that requires no installation

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests = $false
)

Write-Host "🚀 Building KDP Category Ranker (Portable Version)" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow

# Ensure we're in the right directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Clean previous builds
Write-Host "`n🧹 Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "dist") {
    Remove-Item "dist" -Recurse -Force
}
New-Item -ItemType Directory -Path "dist" | Out-Null

# Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed! Build aborted." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ All tests passed!" -ForegroundColor Green
}

# Build the application
Write-Host "`n🔨 Building application..." -ForegroundColor Yellow
dotnet publish src/KDP-CATEGORY-RANKERApp `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output "dist/portable" `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=false `
    /p:PublishTrimmed=false `
    /p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Copy additional files
Write-Host "`n📁 Copying additional files..." -ForegroundColor Yellow

# Copy sample data
$sampleDataDir = "dist/portable/SampleData"
New-Item -ItemType Directory -Path $sampleDataDir -Force | Out-Null

# Create sample data files if they don't exist
$sampleCategories = @{
    "categories" = @(
        @{
            "id" = 1
            "name" = "Business > Entrepreneurship > Small Business"
            "path" = "Kindle Store > Kindle eBooks > Business & Money > Entrepreneurship > Small Business"
            "bestseller_rank_requirement" = 2500
            "estimated_daily_sales" = 25
            "difficulty_score" = 35
            "last_updated" = (Get-Date).ToString("yyyy-MM-dd")
        },
        @{
            "id" = 2
            "name" = "Health > Self-Help > Personal Transformation"
            "path" = "Kindle Store > Kindle eBooks > Health, Fitness & Dieting > Mental Health > Personal Transformation"
            "bestseller_rank_requirement" = 1800
            "estimated_daily_sales" = 45
            "difficulty_score" = 55
            "last_updated" = (Get-Date).ToString("yyyy-MM-dd")
        }
    )
}

$sampleKeywords = @{
    "keywords" = @(
        @{
            "keyword" = "make money online"
            "search_volume" = 5400
            "competition_score" = 78
            "suggested_bid" = 1.25
        },
        @{
            "keyword" = "personal development"
            "search_volume" = 8900
            "competition_score" = 65
            "suggested_bid" = 0.95
        }
    )
}

$sampleBooks = @{
    "books" = @(
        @{
            "asin" = "B08EXAMPLE1"
            "title" = "The Complete Guide to Online Success"
            "author" = "Sample Author"
            "price" = 9.99
            "rank" = 1250
            "category" = "Business > Entrepreneurship"
            "publication_date" = "2023-01-15"
        }
    )
}

# Write sample files
$sampleCategories | ConvertTo-Json -Depth 10 | Out-File "$sampleDataDir/sample-categories.json" -Encoding UTF8
$sampleKeywords | ConvertTo-Json -Depth 10 | Out-File "$sampleDataDir/sample-keywords.json" -Encoding UTF8
$sampleBooks | ConvertTo-Json -Depth 10 | Out-File "$sampleDataDir/sample-books.json" -Encoding UTF8

# Copy configuration files
Copy-Item "src/KDP-CATEGORY-RANKERApp/appsettings.json" "dist/portable/" -Force

# Create README for portable version
$portableReadme = @"
# KDP Category Ranker - Portable Version

This is a portable version of KDP Category Ranker that requires no installation.

## How to Run
1. Simply double-click **KDP-CATEGORY-RANKERApp.exe** to start
2. No installation or admin rights required
3. All data is stored in the application folder

## Features
- Complete category analysis with 19,000+ KDP categories
- AI-powered category recommendations  
- Bestseller planning with daily sales requirements
- Keyword research and competition analysis
- Works offline with sample data included

## System Requirements
- Windows 10/11 (64-bit)
- No additional software required (includes .NET runtime)

## Data Storage
- Application data: Same folder as executable
- User preferences: %APPDATA%\KDP Category Ranker\
- Database: kdp-data.db (created automatically)

## First Run
The application will show a setup wizard on first launch to configure your preferences.

## Support
For questions or issues, visit: https://github.com/your-repo/kdp-category-ranker

---
Version: $(Get-Date -Format "yyyy.MM.dd")
Build: Portable Single-File
"@

$portableReadme | Out-File "dist/portable/README.txt" -Encoding UTF8

# Create a simple launcher batch file
$launcherBat = @"
@echo off
echo Starting KDP Category Ranker...
start "" "KDP-CATEGORY-RANKERApp.exe"
"@

$launcherBat | Out-File "dist/portable/Launch KDP Category Ranker.bat" -Encoding ASCII

# Get file size information
$exeFile = Get-Item "dist/portable/KDP-CATEGORY-RANKERApp.exe"
$folderSize = (Get-ChildItem "dist/portable" -Recurse | Measure-Object -Property Length -Sum).Sum

Write-Host "`n✅ Portable build completed successfully!" -ForegroundColor Green
Write-Host "📍 Location: dist/portable/" -ForegroundColor Cyan
Write-Host "📏 Executable size: $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "📦 Total folder size: $([math]::Round($folderSize / 1MB, 2)) MB" -ForegroundColor Cyan

Write-Host "`n📋 Portable package contents:" -ForegroundColor Yellow
Get-ChildItem "dist/portable" | ForEach-Object {
    $size = if ($_.PSIsContainer) { "(folder)" } else { "$([math]::Round($_.Length / 1KB, 1)) KB" }
    Write-Host "  - $($_.Name) $size" -ForegroundColor Gray
}

Write-Host "`n🎯 To test the portable version:" -ForegroundColor Yellow
Write-Host "  1. Navigate to: dist/portable/" -ForegroundColor Gray
Write-Host "  2. Double-click: KDP-CATEGORY-RANKERApp.exe" -ForegroundColor Gray
Write-Host "  3. Or run: Launch KDP Category Ranker.bat" -ForegroundColor Gray

Write-Host "`n🚀 Portable build ready for distribution!" -ForegroundColor Green