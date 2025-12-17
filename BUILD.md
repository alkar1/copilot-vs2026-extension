# ?? Build & Deploy Guide

## Quick Build

```powershell
# Build extension
dotnet build CopilotExtension/CopilotExtension.csproj -c Release

# VSIX bêdzie tutaj:
# CopilotExtension\bin\Release\CopilotExtension.vsix
```

## Build Script (All-in-One)

```powershell
# Zapisz jako Build.ps1
Write-Host "Building Copilot Extension..." -ForegroundColor Cyan

# Clean
Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean CopilotExtension.sln

# Restore
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore CopilotExtension.sln

# Build Release
Write-Host "Building Release..." -ForegroundColor Yellow
dotnet build CopilotExtension/CopilotExtension.csproj -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "VSIX location:" -ForegroundColor Cyan
    Write-Host "  CopilotExtension\bin\Release\CopilotExtension.vsix" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Install by double-clicking the VSIX file" -ForegroundColor Yellow
} else {
    Write-Host "? Build failed" -ForegroundColor Red
}
```

## Manual Installation

1. **Close all Visual Studio instances**
2. **Double-click** `CopilotExtension.vsix`
3. Follow installer wizard
4. **Restart** Visual Studio

## Verify Installation

```csharp
// Open VS2026, create new C# file
public class Test
{
    public void Method  // Wait for suggestion

// Should see gray suggestion after ~500ms
```

## Uninstallation

1. **Extensions > Manage Extensions**
2. Find "Copilot CLI Extension"
3. Click **Uninstall**
4. Restart VS

## Development Build (Debug)

```powershell
# For debugging
dotnet build CopilotExtension/CopilotExtension.csproj -c Debug

# Press F5 in VS to launch Experimental Instance
```

## Troubleshooting Build

### Error: "SDK not found"
```powershell
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8
```

### Error: "NuGet restore failed"
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore CopilotExtension.sln --force
```

### Error: "VSIX not created"
```powershell
# Check build output folder
dir CopilotExtension\bin\Release\

# If missing, rebuild
dotnet clean
dotnet build -c Release
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build Extension

on: [push]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release
      
      - name: Test
        run: dotnet test --filter "FullyQualifiedName!~CopilotCliServiceTests"
      
      - name: Upload VSIX
        uses: actions/upload-artifact@v3
        with:
          name: vsix
          path: CopilotExtension/bin/Release/*.vsix
```

## Success!

After build, you should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

CopilotExtension\bin\Release\CopilotExtension.vsix
```

**Ready to install! ??**
