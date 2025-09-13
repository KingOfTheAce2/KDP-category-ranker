# KDP Category Ranker - Complete Build Script
# Builds all distribution versions (Portable, Optimized, and MSI)

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests = $false,
    [switch]$PortableOnly = $false,
    [switch]$OptimizedOnly = $false,
    [switch]$InstallerOnly = $false
)

Write-Host "🚀 KDP Category Ranker - Complete Build Process" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Track build times
$buildStartTime = Get-Date

# Clean everything
Write-Host "`n🧹 Cleaning all previous builds..." -ForegroundColor Yellow
if (Test-Path "dist") {
    Remove-Item "dist" -Recurse -Force
}

# Run tests once for all builds
if (-not $SkipTests) {
    Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed! Build process aborted." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ All tests passed!" -ForegroundColor Green
}

# Build summary
$buildSummary = @()
$totalSize = 0

# 1. Build Portable Version
if (-not $OptimizedOnly -and -not $InstallerOnly) {
    Write-Host "`n" + "="*50 -ForegroundColor Cyan
    Write-Host "📦 Building Portable Version" -ForegroundColor Cyan
    Write-Host "="*50 -ForegroundColor Cyan
    
    $portableStart = Get-Date
    & "$scriptPath/build-portable.ps1" -Configuration $Configuration -Runtime $Runtime -SkipTests
    $portableTime = (Get-Date) - $portableStart
    
    if (Test-Path "dist/portable/KDP-CATEGORY-RANKERApp.exe") {
        $portableSize = (Get-ChildItem "dist/portable" -Recurse | Measure-Object -Property Length -Sum).Sum
        $buildSummary += @{
            Name = "Portable"
            Path = "dist/portable/"
            Size = $portableSize
            Time = $portableTime
            Status = "✅ Success"
        }
        $totalSize += $portableSize
        Write-Host "✅ Portable build completed in $($portableTime.ToString('mm\:ss'))" -ForegroundColor Green
    } else {
        $buildSummary += @{
            Name = "Portable"
            Status = "❌ Failed"
        }
        Write-Host "❌ Portable build failed!" -ForegroundColor Red
    }
}

# 2. Build Optimized Version
if (-not $PortableOnly -and -not $InstallerOnly) {
    Write-Host "`n" + "="*50 -ForegroundColor Cyan
    Write-Host "⚡ Building Optimized Version" -ForegroundColor Cyan
    Write-Host "="*50 -ForegroundColor Cyan
    
    $optimizedStart = Get-Date
    & "$scriptPath/build-optimized.ps1" -Configuration $Configuration -Runtime $Runtime -SkipTests -EnableTrimming -EnableR2R
    $optimizedTime = (Get-Date) - $optimizedStart
    
    if (Test-Path "dist/KDP-Category-Ranker-Optimized/KDP-Category-Ranker.exe") {
        $optimizedSize = (Get-ChildItem "dist/KDP-Category-Ranker-Optimized" -Recurse | Measure-Object -Property Length -Sum).Sum
        $buildSummary += @{
            Name = "Optimized"
            Path = "dist/KDP-Category-Ranker-Optimized/"
            Size = $optimizedSize
            Time = $optimizedTime
            Status = "✅ Success"
        }
        $totalSize += $optimizedSize
        Write-Host "✅ Optimized build completed in $($optimizedTime.ToString('mm\:ss'))" -ForegroundColor Green
    } else {
        $buildSummary += @{
            Name = "Optimized"
            Status = "❌ Failed"
        }
        Write-Host "❌ Optimized build failed!" -ForegroundColor Red
    }
}

# 3. Build MSI Installer
if (-not $PortableOnly -and -not $OptimizedOnly) {
    Write-Host "`n" + "="*50 -ForegroundColor Cyan
    Write-Host "📦 Building MSI Installer" -ForegroundColor Cyan
    Write-Host "="*50 -ForegroundColor Cyan
    
    # Check if WiX is available
    $wixCandle = Get-Command "candle.exe" -ErrorAction SilentlyContinue
    $wixLight = Get-Command "light.exe" -ErrorAction SilentlyContinue
    
    if ($wixCandle -and $wixLight) {
        $installerStart = Get-Date
        
        try {
            # First ensure we have the publishdir build
            Write-Host "🔨 Building application for installer..." -ForegroundColor Yellow
            $publishDir = "dist/installer-staging"
            
            dotnet publish src/KDP-CATEGORY-RANKERApp `
                --configuration $Configuration `
                --runtime $Runtime `
                --self-contained true `
                --output $publishDir `
                /p:PublishSingleFile=false `
                /p:PublishReadyToRun=true
            
            if ($LASTEXITCODE -eq 0) {
                # Set up WiX variables
                $env:PublishDir = (Resolve-Path $publishDir).Path
                $env:ResourceDir = (Resolve-Path "installer").Path
                $env:SampleDataDir = (Resolve-Path "dist/KDP-Category-Ranker-Optimized/SampleData").Path
                
                # Build WiX installer
                Write-Host "🔧 Compiling WiX installer..." -ForegroundColor Yellow
                & candle.exe installer/KDP-Category-Ranker.wxs -out dist/KDP-Category-Ranker.wixobj
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "🔗 Linking MSI package..." -ForegroundColor Yellow
                    & light.exe dist/KDP-Category-Ranker.wixobj -out dist/KDP-Category-Ranker-Setup.msi
                    
                    if ($LASTEXITCODE -eq 0) {
                        $installerTime = (Get-Date) - $installerStart
                        $installerSize = (Get-Item "dist/KDP-Category-Ranker-Setup.msi").Length
                        
                        $buildSummary += @{
                            Name = "MSI Installer"
                            Path = "dist/KDP-Category-Ranker-Setup.msi"
                            Size = $installerSize
                            Time = $installerTime
                            Status = "✅ Success"
                        }
                        $totalSize += $installerSize
                        Write-Host "✅ MSI installer completed in $($installerTime.ToString('mm\:ss'))" -ForegroundColor Green
                    } else {
                        Write-Host "❌ WiX Light failed!" -ForegroundColor Red
                        $buildSummary += @{ Name = "MSI Installer"; Status = "❌ Light Failed" }
                    }
                } else {
                    Write-Host "❌ WiX Candle failed!" -ForegroundColor Red
                    $buildSummary += @{ Name = "MSI Installer"; Status = "❌ Candle Failed" }
                }
            } else {
                Write-Host "❌ Installer staging build failed!" -ForegroundColor Red
                $buildSummary += @{ Name = "MSI Installer"; Status = "❌ Staging Failed" }
            }
        }
        catch {
            Write-Host "❌ MSI build error: $($_.Exception.Message)" -ForegroundColor Red
            $buildSummary += @{ Name = "MSI Installer"; Status = "❌ Exception" }
        }
    } else {
        Write-Host "⚠️  WiX Toolset not found. MSI installer skipped." -ForegroundColor Yellow
        Write-Host "   Install WiX Toolset v4.0+ to enable MSI creation." -ForegroundColor Gray
        Write-Host "   Download: https://wixtoolset.org/releases/" -ForegroundColor Gray
        
        $buildSummary += @{
            Name = "MSI Installer"
            Status = "⚠️  WiX Not Found"
        }
    }
}

# 4. Create Release Package
Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "📁 Creating Release Package" -ForegroundColor Cyan
Write-Host "="*50 -ForegroundColor Cyan

$releaseDir = "dist/KDP-Category-Ranker-Release-$(Get-Date -Format 'yyyyMMdd')"
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

# Copy successful builds to release folder
foreach ($build in $buildSummary) {
    if ($build.Status -eq "✅ Success" -and $build.Path) {
        $buildName = $build.Name -replace " ", "-"
        $targetPath = "$releaseDir/$buildName"
        
        if (Test-Path $build.Path) {
            if ((Get-Item $build.Path).PSIsContainer) {
                # Directory - copy contents
                Copy-Item $build.Path $targetPath -Recurse -Force
            } else {
                # File - copy directly
                Copy-Item $build.Path $releaseDir -Force
            }
            Write-Host "📦 Added $($build.Name) to release package" -ForegroundColor Green
        }
    }
}

# Create release notes
$releaseNotes = @"
# KDP Category Ranker - Release $(Get-Date -Format 'yyyy.MM.dd')

🚀 **Complete Distribution Package**

## 📦 What's Included

This release package contains multiple distribution formats:

### 📱 Portable Version (`/Portable/`)
- **Single-file executable** - no installation required
- **Full feature set** with sample data included
- **Runs from USB drive** or any folder
- **Perfect for**: Testing, demonstrations, or users without admin rights

### ⚡ Optimized Version (`/Optimized/`)
- **High-performance build** with ReadyToRun compilation
- **Reduced file size** through intelligent trimming
- **Faster startup time** and optimized memory usage
- **Perfect for**: Daily use, production environments

### 📦 MSI Installer (`KDP-Category-Ranker-Setup.msi`)
- **Professional Windows installer** with auto-launch
- **Start Menu shortcuts** and file associations
- **Automatic updates support** and uninstall functionality
- **Perfect for**: End users, enterprise deployment

## 🎯 Key Features

- **🚀 Category Recommender**: AI-powered category analysis with difficulty scoring
- **📊 Advanced Analytics**: 19,000+ KDP categories with bestseller requirements
- **🔍 Research Tools**: Keyword research, competition analysis, AMS generator
- **💰 Revenue Planning**: Daily sales targets and earnings projections
- **🌍 Global Markets**: Support for 8 major Amazon marketplaces
- **📈 Trend Analysis**: Historical data and seasonal patterns
- **🔐 License System**: Flexible licensing with feature tiers
- **🔄 Auto-Updates**: Automatic update notifications and installation

## 💻 System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 1GB free space for application and data
- **Network**: Broadband internet for live data (optional - works offline)

## 🚀 Quick Start

1. **Choose your preferred version**:
   - **First-time users**: Try the Portable version
   - **Regular users**: Use the MSI installer
   - **Power users**: Use the Optimized version

2. **Run the application** and complete the first-run setup wizard

3. **Start with Category Recommender** to find the best categories for your books

## 📞 Support

- **Documentation**: Built-in help system (press F1)
- **GitHub**: https://github.com/your-repo/kdp-category-ranker
- **Issues**: Report problems via GitHub Issues

---

**Build Date**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  
**Target Platform**: $Runtime  
**Configuration**: $Configuration  

© 2024 KDP Tools LLC. All rights reserved.
"@

$releaseNotes | Out-File "$releaseDir/README-Release.txt" -Encoding UTF8

# Create ZIP packages
if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
    Write-Host "`n📁 Creating distribution archives..." -ForegroundColor Yellow
    
    # Create individual ZIP files for each build
    foreach ($build in $buildSummary) {
        if ($build.Status -eq "✅ Success" -and $build.Path) {
            $zipName = "KDP-Category-Ranker-$($build.Name -replace ' ', '-')-$Runtime-$(Get-Date -Format 'yyyyMMdd').zip"
            $zipPath = "dist/$zipName"
            
            if (Test-Path $build.Path) {
                if ((Get-Item $build.Path).PSIsContainer) {
                    Compress-Archive -Path "$($build.Path)/*" -DestinationPath $zipPath -Force
                } else {
                    Compress-Archive -Path $build.Path -DestinationPath $zipPath -Force
                }
                Write-Host "📦 Created: $zipName" -ForegroundColor Green
            }
        }
    }
    
    # Create complete release ZIP
    $completeZip = "dist/KDP-Category-Ranker-Complete-$Runtime-$(Get-Date -Format 'yyyyMMdd').zip"
    Compress-Archive -Path "$releaseDir/*" -DestinationPath $completeZip -Force
    Write-Host "📦 Created complete release: $(Split-Path $completeZip -Leaf)" -ForegroundColor Green
}

# Final Summary
$totalBuildTime = (Get-Date) - $buildStartTime
Write-Host "`n" + "="*60 -ForegroundColor Green
Write-Host "🎉 BUILD PROCESS COMPLETED!" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Green

Write-Host "`n📊 Build Summary:" -ForegroundColor Yellow
Write-Host "─" * 60 -ForegroundColor Gray

foreach ($build in $buildSummary) {
    $statusColor = switch ($build.Status.Substring(0,1)) {
        "✅" { "Green" }
        "❌" { "Red" }
        "⚠" { "Yellow" }
        default { "White" }
    }
    
    Write-Host ("  {0,-20} {1}" -f $build.Name, $build.Status) -ForegroundColor $statusColor
    
    if ($build.Size) {
        Write-Host ("  {0,-20} {1:N2} MB in {2}" -f "", ($build.Size / 1MB), $build.Time.ToString('mm\:ss')) -ForegroundColor Gray
    }
}

Write-Host "─" * 60 -ForegroundColor Gray
Write-Host ("  {0,-20} {1:N2} MB total" -f "Total Size:", ($totalSize / 1MB)) -ForegroundColor Cyan
Write-Host ("  {0,-20} {1}" -f "Total Time:", $totalBuildTime.ToString('mm\:ss')) -ForegroundColor Cyan

Write-Host "`n📁 Output Directory: dist/" -ForegroundColor Yellow
Write-Host "📦 Release Package: $releaseDir" -ForegroundColor Yellow

Write-Host "`n🎯 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test each build version thoroughly" -ForegroundColor Gray
Write-Host "  2. Update version numbers if needed" -ForegroundColor Gray
Write-Host "  3. Create GitHub release with ZIP files" -ForegroundColor Gray
Write-Host "  4. Update documentation and changelog" -ForegroundColor Gray

Write-Host "`n🚀 All builds ready for distribution!" -ForegroundColor Green