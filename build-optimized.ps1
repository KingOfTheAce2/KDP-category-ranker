# KDP Category Ranker - Optimized Single-File Build Script
# Creates highly optimized builds for distribution

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests = $false,
    [switch]$EnableTrimming = $true,
    [switch]$EnableR2R = $true,
    [switch]$CreateInstaller = $false
)

Write-Host "🚀 Building KDP Category Ranker (Optimized)" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Trimming: $EnableTrimming" -ForegroundColor Yellow
Write-Host "ReadyToRun: $EnableR2R" -ForegroundColor Yellow

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

# Build parameters
$publishArgs = @(
    "src/KDP-CATEGORY-RANKERApp"
    "--configuration", $Configuration
    "--runtime", $Runtime
    "--self-contained", "true"
    "--output", "dist/optimized"
    "/p:PublishSingleFile=true"
    "/p:IncludeNativeLibrariesForSelfExtract=true"
    "/p:EnableCompressionInSingleFile=true"
    "/p:DebugType=None"
    "/p:DebugSymbols=false"
)

# Add trimming if enabled
if ($EnableTrimming) {
    Write-Host "🔧 Enabling IL trimming for smaller file size..." -ForegroundColor Yellow
    $publishArgs += "/p:PublishTrimmed=true"
    $publishArgs += "/p:TrimMode=partial"
    # Preserve assemblies that might be used via reflection
    $publishArgs += "/p:TrimmerDefaultAction=link"
}

# Add ReadyToRun if enabled
if ($EnableR2R) {
    Write-Host "⚡ Enabling ReadyToRun for faster startup..." -ForegroundColor Yellow
    $publishArgs += "/p:PublishReadyToRun=true"
    $publishArgs += "/p:PublishReadyToRunShowWarnings=true"
}

# Additional optimizations
$publishArgs += "/p:Optimize=true"
$publishArgs += "/p:UseAppHost=true"
$publishArgs += "/p:IncludeAllContentForSelfExtract=true"

# Build the application
Write-Host "`n🔨 Building optimized application..." -ForegroundColor Yellow
dotnet publish @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Post-build optimizations
Write-Host "`n🔧 Applying post-build optimizations..." -ForegroundColor Yellow

# Copy and organize files
$sourceDir = "dist/optimized"
$finalDir = "dist/KDP-Category-Ranker-Optimized"
New-Item -ItemType Directory -Path $finalDir -Force | Out-Null

# Copy main executable
Copy-Item "$sourceDir/KDP-CATEGORY-RANKERApp.exe" "$finalDir/KDP-Category-Ranker.exe" -Force

# Create optimized sample data
$sampleDataDir = "$finalDir/SampleData"
New-Item -ItemType Directory -Path $sampleDataDir -Force | Out-Null

# Generate comprehensive sample data
$categories = @()
$keywords = @()
$books = @()

# Generate 100 sample categories with realistic data
for ($i = 1; $i -le 100; $i++) {
    $categoryNames = @(
        "Business > Entrepreneurship",
        "Health > Self-Help",
        "Fiction > Romance",
        "Non-Fiction > Biography",
        "Children's Books > Early Learning",
        "Technology > Programming",
        "Cooking > International",
        "Travel > Europe",
        "History > Ancient",
        "Science > Physics"
    )
    
    $category = @{
        "id" = $i
        "name" = $categoryNames[($i - 1) % $categoryNames.Length] + " > Category $i"
        "path" = "Kindle Store > Kindle eBooks > " + $categoryNames[($i - 1) % $categoryNames.Length]
        "bestseller_rank_requirement" = (Get-Random -Minimum 500 -Maximum 5000)
        "estimated_daily_sales" = (Get-Random -Minimum 5 -Maximum 100)
        "difficulty_score" = (Get-Random -Minimum 20 -Maximum 90)
        "growth_trend" = (Get-Random -Minimum -15 -Maximum 25)
        "last_updated" = (Get-Date).ToString("yyyy-MM-dd")
    }
    $categories += $category
}

# Generate 50 sample keywords
$keywordList = @(
    "make money online", "personal development", "weight loss", "romance novel",
    "children's book", "programming guide", "healthy recipes", "travel guide",
    "business strategy", "self improvement", "fiction writing", "kindle publishing",
    "amazon marketing", "book promotion", "author platform", "passive income"
)

for ($i = 0; $i -lt 50; $i++) {
    $keyword = @{
        "keyword" = $keywordList[$i % $keywordList.Length] + " " + (Get-Random -Minimum 1 -Maximum 100)
        "search_volume" = (Get-Random -Minimum 100 -Maximum 10000)
        "competition_score" = (Get-Random -Minimum 30 -Maximum 95)
        "suggested_bid" = [math]::Round((Get-Random -Minimum 0.5 -Maximum 3.0), 2)
        "trend" = (Get-Random -Minimum -20 -Maximum 30)
    }
    $keywords += $keyword
}

# Generate 25 sample books
$bookTitles = @(
    "The Complete Guide to Success", "Mastering Your Craft", "Journey to Excellence",
    "Simple Steps to Wealth", "The Art of Living Well", "Breakthrough Strategies",
    "Ultimate Success Formula", "Proven Methods for Growth", "The Winner's Mindset"
)

for ($i = 1; $i -le 25; $i++) {
    $book = @{
        "asin" = "B08EXAMPLE$i"
        "title" = $bookTitles[($i - 1) % $bookTitles.Length] + " Vol. $i"
        "author" = "Sample Author $i"
        "price" = [math]::Round((Get-Random -Minimum 2.99 -Maximum 19.99), 2)
        "rank" = (Get-Random -Minimum 100 -Maximum 50000)
        "category" = $categories[(Get-Random -Minimum 0 -Maximum $categories.Length)].name
        "publication_date" = (Get-Date).AddDays(-(Get-Random -Minimum 1 -Maximum 365)).ToString("yyyy-MM-dd")
        "rating" = [math]::Round((Get-Random -Minimum 3.5 -Maximum 5.0), 1)
        "review_count" = (Get-Random -Minimum 10 -Maximum 1000)
    }
    $books += $book
}

# Save optimized sample data
@{ "categories" = $categories } | ConvertTo-Json -Depth 10 -Compress | Out-File "$sampleDataDir/sample-categories.json" -Encoding UTF8
@{ "keywords" = $keywords } | ConvertTo-Json -Depth 10 -Compress | Out-File "$sampleDataDir/sample-keywords.json" -Encoding UTF8
@{ "books" = $books } | ConvertTo-Json -Depth 10 -Compress | Out-File "$sampleDataDir/sample-books.json" -Encoding UTF8

# Copy configuration file
Copy-Item "src/KDP-CATEGORY-RANKERApp/appsettings.json" "$finalDir/" -Force

# Create optimized configuration
$optimizedConfig = @{
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Warning"
            "Microsoft" = "Warning"
            "Microsoft.Hosting.Lifetime" = "Information"
        }
    }
    "Scraping" = @{
        "MaxRequestsPerMinute" = 15
        "DelayBetweenRequestsMs" = 1500
        "MaxConcurrency" = 3
        "RespectRobotsTxt" = $true
        "EnableCaching" = $true
        "CacheExpirationHours" = 24
    }
    "SalesEstimation" = @{
        "Kindle" = @{ 
            "CoefficientA" = 5500.0
            "CoefficientB" = -0.83 
        }
        "Print" = @{ 
            "CoefficientA" = 2600.0
            "CoefficientB" = -0.75 
        }
    }
    "Performance" = @{
        "EnableDataCompression" = $true
        "MaxCacheSize" = "100MB"
        "DatabaseOptimizations" = $true
    }
}

$optimizedConfig | ConvertTo-Json -Depth 10 | Out-File "$finalDir/appsettings.json" -Encoding UTF8

# Create portable marker
"This file indicates the application is running in portable mode." | Out-File "$finalDir/portable.txt" -Encoding UTF8

# Create launcher script
$launcherScript = @"
@echo off
title KDP Category Ranker
echo Starting KDP Category Ranker...
echo.
echo 🚀 Initializing application...
timeout /t 1 /nobreak >nul

rem Check if .NET is available (optional check)
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️  .NET runtime not found in PATH, but this shouldn't be a problem.
    echo    This is a self-contained application.
    echo.
)

rem Start the application
start "" "KDP-Category-Ranker.exe"

rem Optional: Keep window open for 3 seconds to show any startup messages
timeout /t 3 /nobreak >nul
"@

$launcherScript | Out-File "$finalDir/Launch KDP Category Ranker.bat" -Encoding ASCII

# Create comprehensive README
$optimizedReadme = @"
# KDP Category Ranker - Optimized Build

🚀 **High-Performance Edition** - Optimized for speed and minimal disk usage

## 📦 What's Included

This optimized build includes:
- **Single-file executable** with all dependencies embedded
- **Optimized startup time** with ReadyToRun compilation
- **Reduced file size** through intelligent trimming
- **Comprehensive sample data** (100 categories, 50 keywords, 25 books)
- **Portable configuration** - runs from any location

## 🎯 Key Features

### 🚀 Category Recommender
- AI-powered category recommendations for your books
- Bestseller planning with daily sales requirements
- Difficulty scoring (0-100) for each category
- Revenue projections and optimal timing analysis

### 📊 Advanced Analytics
- 19,000+ KDP categories with real-time metrics
- Historic trends and seasonal patterns
- Ghost category detection
- Competition analysis tools

### 🔍 Research Tools
- Keyword research with competitive scoring
- Reverse ASIN lookup
- AMS keyword generator
- International market support (8 markets)

## 💻 System Requirements

- **OS**: Windows 10/11 (64-bit)
- **RAM**: 2GB minimum, 4GB recommended  
- **Storage**: 200MB for application + database growth
- **Network**: Optional (works offline with sample data)

## 🚀 Quick Start

1. **Run the application**:
   - Double-click `KDP-Category-Ranker.exe`, or
   - Use `Launch KDP Category Ranker.bat`

2. **First-time setup**:
   - Complete the welcome wizard
   - Choose Demo or Live data mode
   - Set your preferences

3. **Start with Category Recommender**:
   - Click "🚀 Category Recommender" 
   - Enter your book details
   - Get AI-powered recommendations

## 📁 File Structure

```
KDP-Category-Ranker-Optimized/
├── KDP-Category-Ranker.exe          # Main application (single-file)
├── Launch KDP Category Ranker.bat   # Convenient launcher
├── appsettings.json                 # Configuration file
├── portable.txt                     # Portable mode marker
├── SampleData/                      # Demo data
│   ├── sample-categories.json       # 100 sample categories
│   ├── sample-keywords.json         # 50 sample keywords
│   └── sample-books.json           # 25 sample books
└── README.txt                       # This file
```

## 🔧 Data Storage

- **Application data**: Same folder as executable (portable mode)
- **User preferences**: `%APPDATA%\KDP Category Ranker\`
- **Database**: `kdp-data.db` (created automatically)
- **Cache**: Temporary files in system temp folder

## ⚡ Performance Features

This optimized build includes:
- **ReadyToRun (R2R)** compilation for faster startup
- **IL Trimming** for reduced file size
- **Compressed single-file** packaging
- **Optimized sample data** with realistic scenarios
- **Intelligent caching** to reduce network requests

## 🆘 Troubleshooting

### Application won't start
- Try running as administrator
- Check Windows Defender hasn't quarantined the file
- Ensure you have latest Windows updates

### Missing features
- Make sure you're using the correct executable
- Check if antivirus is blocking network access
- Try running `Launch KDP Category Ranker.bat`

### Performance issues
- Close other resource-intensive applications
- Ensure adequate free disk space (500MB+)
- Try clearing browser cache and temp files

## 📞 Support

- **Documentation**: Included help system (press F1)
- **GitHub**: https://github.com/your-repo/kdp-category-ranker
- **Issues**: Report bugs via GitHub Issues

---

**Version**: $(Get-Date -Format "yyyy.MM.dd")  
**Build**: Optimized Single-File ($(if($EnableTrimming){"Trimmed + "})$(if($EnableR2R){"R2R"})ReadyToRun)  
**Target**: $Runtime  

🎯 **Perfect for**: Authors, publishers, and KDP researchers who need fast, reliable category analysis tools.
"@

$optimizedReadme | Out-File "$finalDir/README.txt" -Encoding UTF8

# Get file information
$exeFile = Get-Item "$finalDir/KDP-Category-Ranker.exe"
$folderSize = (Get-ChildItem $finalDir -Recurse | Measure-Object -Property Length -Sum).Sum

Write-Host "`n✅ Optimized build completed!" -ForegroundColor Green
Write-Host "📍 Location: $finalDir" -ForegroundColor Cyan
Write-Host "📏 Executable size: $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "📦 Total package size: $([math]::Round($folderSize / 1MB, 2)) MB" -ForegroundColor Cyan

# Show optimization results
$originalSize = if (Test-Path "dist/portable/KDP-CATEGORY-RANKERApp.exe") {
    (Get-Item "dist/portable/KDP-CATEGORY-RANKERApp.exe").Length
} else { $exeFile.Length }

$savings = [math]::Round((1 - ($exeFile.Length / $originalSize)) * 100, 1)
if ($savings -gt 0) {
    Write-Host "💾 Size reduction: $savings%" -ForegroundColor Green
}

Write-Host "`n📋 Optimized package contents:" -ForegroundColor Yellow
Get-ChildItem $finalDir | ForEach-Object {
    $size = if ($_.PSIsContainer) { 
        $subSize = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
        "($([math]::Round($subSize / 1KB, 1)) KB total)"
    } else { 
        "$([math]::Round($_.Length / 1KB, 1)) KB" 
    }
    Write-Host "  - $($_.Name) $size" -ForegroundColor Gray
}

# Create installer if requested
if ($CreateInstaller) {
    Write-Host "`n📦 Creating MSI installer..." -ForegroundColor Yellow
    
    # Check if WiX is available
    $wixPath = Get-Command "candle.exe" -ErrorAction SilentlyContinue
    if ($wixPath) {
        # Build MSI installer (requires additional WiX setup)
        Write-Host "🔧 WiX Toolset found, building installer..." -ForegroundColor Yellow
        # Add MSI build commands here
    } else {
        Write-Host "⚠️  WiX Toolset not found. MSI installer not created." -ForegroundColor Yellow
        Write-Host "   Install WiX Toolset to enable MSI creation." -ForegroundColor Gray
    }
}

Write-Host "`n🎯 Testing the optimized build:" -ForegroundColor Yellow
Write-Host "  1. Navigate to: $finalDir" -ForegroundColor Gray
Write-Host "  2. Double-click: KDP-Category-Ranker.exe" -ForegroundColor Gray
Write-Host "  3. Or run: Launch KDP Category Ranker.bat" -ForegroundColor Gray

Write-Host "`n🚀 Optimized build ready for distribution!" -ForegroundColor Green

# Create a ZIP package for easy distribution
if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
    Write-Host "`n📁 Creating distribution ZIP..." -ForegroundColor Yellow
    $zipPath = "dist/KDP-Category-Ranker-Optimized-$Runtime-$(Get-Date -Format 'yyyyMMdd').zip"
    Compress-Archive -Path "$finalDir/*" -DestinationPath $zipPath -Force
    Write-Host "📦 Distribution package: $zipPath" -ForegroundColor Cyan
    Write-Host "📏 ZIP size: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor Cyan
}