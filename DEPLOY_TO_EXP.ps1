# Manual Extension Deployment to Experimental Instance
# This deploys extension WITHOUT VSIX - VS 18 (2025)

Write-Host "=== MANUAL EXTENSION DEPLOYMENT ===" -ForegroundColor Cyan
Write-Host ""

# 1. Find Experimental Instance directory (VS 18)
$expInstanceDirs = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\VisualStudio" -Directory | Where-Object { $_.Name -match "18\.0.*Exp" }

if ($expInstanceDirs.Count -eq 0) {
    Write-Host "ERROR: No Experimental Instance found!" -ForegroundColor Red
    Write-Host "Run Visual Studio 18 with /rootsuffix Exp at least once to create it." -ForegroundColor Yellow
    Write-Host "" 
    Write-Host "Try running:" -ForegroundColor Cyan
    Write-Host '  & "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe" /rootsuffix Exp' -ForegroundColor White
    exit 1
}

$expInstance = $expInstanceDirs | Select-Object -First 1
Write-Host "Found Experimental Instance (VS 18):" -ForegroundColor Green
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

# 3. Copy all files from bin
$binDir = "C:\PROJ\VS2026\Copilot\CopilotExtension\bin\Debug\net8.0-windows"

if (-not (Test-Path $binDir)) {
    Write-Host "ERROR: Build directory not found!" -ForegroundColor Red
    Write-Host "Path: $binDir" -ForegroundColor Yellow
    Write-Host "Build the project first: dotnet build" -ForegroundColor Yellow
    exit 1
}

Write-Host "Copying files from build directory..." -ForegroundColor Cyan
Copy-Item -Path "$binDir\*" -Destination $ourExtDir -Recurse -Force

$copiedFiles = Get-ChildItem $ourExtDir -Recurse
Write-Host "Copied $($copiedFiles.Count) files" -ForegroundColor Green

# 4. Verify critical files
Write-Host "`nVerifying critical files..." -ForegroundColor Cyan
$criticalFiles = @(
    "CopilotExtension.dll",
    "CopilotExtension.pkgdef",
    "extension.vsixmanifest"
)

$allPresent = $true
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $ourExtDir $file
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length
        Write-Host "  ? $file ($size bytes)" -ForegroundColor Green
    } else {
        Write-Host "  ? MISSING: $file" -ForegroundColor Red
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Host "`nWARNING: Some critical files are missing!" -ForegroundColor Yellow
    Write-Host "Extension may not load correctly." -ForegroundColor Yellow
}

# 5. Create extensions.configurationchanged to force reload
$configChanged = "$($expInstance.FullName)\extensions.configurationchanged"
"" | Out-File $configChanged -Encoding ASCII -Force
Write-Host "`nMarked for reload: extensions.configurationchanged" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Green  
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Close Experimental Instance if running"
Write-Host "2. Run: .\StartExpInstance.ps1"
Write-Host "   OR"
Write-Host '   & "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe" /rootsuffix Exp'
Write-Host ""
Write-Host "3. In Experimental Instance:" -ForegroundColor Cyan
Write-Host "   - Extensions > Manage Extensions > Installed"
Write-Host "   - Tools > Options > Copilot CLI"
Write-Host "   - File > Account Settings (Sign in to GitHub)"
Write-Host ""
Write-Host "Extension deployed to:" -ForegroundColor Cyan
Write-Host "  $ourExtDir" -ForegroundColor Gray
Write-Host ""
