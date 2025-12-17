# Manual Extension Deployment to Experimental Instance
# This deploys extension WITHOUT VSIX

Write-Host "=== MANUAL EXTENSION DEPLOYMENT ===" -ForegroundColor Cyan
Write-Host ""

# 1. Find Experimental Instance directory
$expInstanceDirs = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\VisualStudio" -Directory | Where-Object { $_.Name -match "17\.0.*Exp" }

if ($expInstanceDirs.Count -eq 0) {
    Write-Host "ERROR: No Experimental Instance found!" -ForegroundColor Red
    Write-Host "Run Visual Studio with /rootsuffix Exp at least once to create it." -ForegroundColor Yellow
    exit 1
}

$expInstance = $expInstanceDirs | Select-Object -First 1
Write-Host "Found Experimental Instance:" -ForegroundColor Green
Write-Host "  $($expInstance.FullName)" -ForegroundColor Gray
Write-Host ""

# 2. Create Extensions folder
$extDir = "$($expInstance.FullName)\Extensions"
$ourExtDir = "$extDir\CopilotExtension"

if (-not (Test-Path $extDir)) {
    New-Item -ItemType Directory -Path $extDir -Force | Out-Null
}

if (Test-Path $ourExtDir) {
    Write-Host "Removing old deployment..." -ForegroundColor Yellow
    Remove-Item $ourExtDir -Recurse -Force
}

New-Item -ItemType Directory -Path $ourExtDir -Force | Out-Null
Write-Host "Extension directory created:" -ForegroundColor Green
Write-Host "  $ourExtDir" -ForegroundColor Gray
Write-Host ""

# 3. Copy DLL
$sourceDll = "C:\PROJ\VS2026\Copilot\CopilotExtension\bin\Debug\net8.0-windows\CopilotExtension.dll"
if (-not (Test-Path $sourceDll)) {
    Write-Host "ERROR: CopilotExtension.dll not found!" -ForegroundColor Red
    Write-Host "Build the project first!" -ForegroundColor Yellow
    exit 1
}

Copy-Item $sourceDll $ourExtDir
Write-Host "Copied: CopilotExtension.dll" -ForegroundColor Green

# 4. Copy dependencies
$binDir = "C:\PROJ\VS2026\Copilot\CopilotExtension\bin\Debug\net8.0-windows"
if (Test-Path "$binDir\Newtonsoft.Json.dll") {
    Copy-Item "$binDir\Newtonsoft.Json.dll" $ourExtDir
    Write-Host "Copied: Newtonsoft.Json.dll" -ForegroundColor Green
}

# 5. Copy manifest
$manifest = "C:\PROJ\VS2026\Copilot\CopilotExtension\source.extension.vsixmanifest"
Copy-Item $manifest "$ourExtDir\extension.vsixmanifest"
Write-Host "Copied: extension.vsixmanifest" -ForegroundColor Green

# 6. Create extension.pkgdef
$pkgdef = @"
// CopilotExtension Package Definition
[$RootKey$\Packages\{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}]
@="CopilotExtension"
"InprocServer32"="`$WinDir`$\SYSTEM32\MSCOREE.DLL"
"Class"="CopilotExtension.CopilotExtensionPackage"
"Assembly"="CopilotExtension"
"CodeBase"="`$PackageFolder`$\CopilotExtension.dll"

[$RootKey$\Menus]
"{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}"=", Menus.ctmenu, 1"
"@

$pkgdef | Out-File "$ourExtDir\CopilotExtension.pkgdef" -Encoding ASCII
Write-Host "Created: CopilotExtension.pkgdef" -ForegroundColor Green
Write-Host ""

# 7. Create extensions.configurationchanged to force reload
$configChanged = "$($expInstance.FullName)\extensions.configurationchanged"
"" | Out-File $configChanged -Encoding ASCII -Force
Write-Host "Marked for reload: extensions.configurationchanged" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Green  
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Close Experimental Instance if running"
Write-Host "2. Start debugging (F5) from main Visual Studio"
Write-Host "3. Check Output > Debug for initialization logs"
Write-Host ""
Write-Host "Extension deployed to:" -ForegroundColor Cyan
Write-Host "  $ourExtDir" -ForegroundColor Gray
Write-Host ""
