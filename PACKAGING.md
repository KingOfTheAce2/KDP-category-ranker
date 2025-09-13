# Packaging Instructions

## Single-File EXE Deployment

### Windows x64 Self-Contained Executable
```bash
dotnet publish src/KDP-CATEGORY-RANKERApp -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishTrimmed=false
```

Output location: `src/KDP-CATEGORY-RANKERApp/bin/Release/net8.0-windows/win-x64/publish/KDP-CATEGORY-RANKERApp.exe`

### Size Optimization (Optional)
For smaller file size (may break some features):
```bash
dotnet publish src/KDP-CATEGORY-RANKERApp -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishTrimmed=true \
  /p:TrimMode=partial
```

## MSI Installer (Windows)

### Prerequisites
- Install WiX Toolset v4: https://wixtoolset.org/
- Visual Studio extension (optional but recommended)

### Basic MSI Creation

1. **Create WiX Project File** (`KDP-Category-Ranker.wixproj`):
```xml
<Project Sdk="WixToolset.Sdk/4.0.0">
  <PropertyGroup>
    <OutputType>Package</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="WixToolset.UI.wixext" Version="4.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="Package.wxs" />
  </ItemGroup>
</Project>
```

2. **Create Package.wxs**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Id="*" Name="KDP Category Ranker" Language="1033" Version="1.0.0" 
           Manufacturer="KDP Tools" UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <SummaryInformation Keywords="Installer" Description="KDP Category Ranker Installer" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="KDP Category Ranker" Level="1">
      <ComponentRef Id="MainExecutable" />
      <ComponentRef Id="StartMenuShortcut" />
      <ComponentRef Id="DesktopShortcut" />
    </Feature>

    <StandardDirectory Id="ProgramFilesFolder">
      <Directory Id="INSTALLFOLDER" Name="KDP Category Ranker">
        <Component Id="MainExecutable">
          <File Id="MainExe" Source="src/KDP-CATEGORY-RANKERApp/bin/Release/net8.0-windows/win-x64/publish/KDP-CATEGORY-RANKERApp.exe" />
        </Component>
      </Directory>
    </StandardDirectory>

    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="KDP Category Ranker">
        <Component Id="StartMenuShortcut">
          <Shortcut Id="ApplicationStartMenuShortcut"
                    Name="KDP Category Ranker"
                    Description="Amazon KDP keyword research tool"
                    Target="[INSTALLFOLDER]KDP-CATEGORY-RANKERApp.exe"
                    WorkingDirectory="INSTALLFOLDER" />
          <RemoveFolder Id="CleanUpStartMenu" Directory="ApplicationProgramsFolder" On="uninstall" />
          <RegistryValue Root="HKCU" Key="Software\KDPCategoryRanker" Name="installed" Type="integer" Value="1" KeyPath="yes" />
        </Component>
      </Directory>
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder">
      <Component Id="DesktopShortcut">
        <Shortcut Id="ApplicationDesktopShortcut"
                  Name="KDP Category Ranker"
                  Description="Amazon KDP keyword research tool"
                  Target="[INSTALLFOLDER]KDP-CATEGORY-RANKERApp.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RegistryValue Root="HKCU" Key="Software\KDPCategoryRanker" Name="desktop" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </StandardDirectory>
  </Package>
</Wix>
```

3. **Build MSI**:
```bash
# First build the application
dotnet build -c Release

# Then build the MSI
dotnet build KDP-Category-Ranker.wixproj -c Release
```

### Advanced MSI Features

For a production MSI, consider adding:
- **File associations** (.rocketproj files)
- **Registry entries** for application settings
- **Custom actions** for first-run setup
- **Digital signing** for trusted installation
- **Uninstall cleanup** routines

## Deployment Notes

### System Requirements
- Windows 10 version 1607 or later (x64)
- .NET 8 Runtime (included in self-contained build)
- ~150MB disk space for self-contained build
- 4GB RAM minimum, 8GB recommended

### First Run Setup
The application includes a first-run wizard that:
1. Creates the local database
2. Seeds sample data for offline mode
3. Configures default settings
4. Shows getting started guide

### Distribution Options
1. **GitHub Releases**: Upload the single-file EXE
2. **Microsoft Store**: Package as MSIX (requires additional setup)
3. **Company Website**: Host MSI installer
4. **USB/Offline**: Self-contained EXE works without internet

### Digital Signing (Recommended for Production)
```bash
# Sign the executable before packaging
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com KDP-CATEGORY-RANKERApp.exe

# Sign the MSI installer
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com Setup.msi
```

## Testing Deployment

### Automated Testing
```bash
# Test build process
dotnet publish src/KDP-CATEGORY-RANKERApp -c Release -r win-x64 --self-contained

# Verify executable runs
./src/KDP-CATEGORY-RANKERApp/bin/Release/net8.0-windows/win-x64/publish/KDP-CATEGORY-RANKERApp.exe

# Run unit tests
dotnet test
```

### Manual Testing Checklist
- [ ] Application launches successfully
- [ ] First-run wizard completes
- [ ] Sample data loads correctly
- [ ] All major features work in offline mode
- [ ] Database is created in correct location
- [ ] Application closes cleanly
- [ ] Uninstall removes all components (for MSI)

## File Associations (.rocketproj)

To enable .rocketproj file associations, add to Package.wxs:

```xml
<Component Id="FileAssociation">
  <RegistryValue Root="HKCR" Key=".rocketproj" Value="KDPCategoryRanker.Project" Type="string" />
  <RegistryValue Root="HKCR" Key="KDPCategoryRanker.Project" Value="KDP Category Ranker Project" Type="string" />
  <RegistryValue Root="HKCR" Key="KDPCategoryRanker.Project\shell\open\command" 
                 Value="[INSTALLFOLDER]KDP-CATEGORY-RANKERApp.exe &quot;%1&quot;" Type="string" />
  <RegistryValue Root="HKCR" Key="KDPCategoryRanker.Project\DefaultIcon" 
                 Value="[INSTALLFOLDER]KDP-CATEGORY-RANKERApp.exe,0" Type="string" />
</Component>
```

This allows users to double-click .rocketproj files to open them in the application.