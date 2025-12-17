# Manual VSIX Builder for CopilotExtension
# This script manually creates VSIX package when MSBuild fails

Write-Host "=== MANUAL VSIX BUILDER ===" -ForegroundColor Cyan
Write-Host ""

$projectDir = "C:\PROJ\VS2026\Copilot\CopilotExtension"
$binDir = "$projectDir\bin\Debug\net8.0-windows"
$outputDir = "$projectDir\bin\Debug"
$vsixPath = "$outputDir\CopilotExtension.vsix"

# 1. Check if DLL exists
Write-Host "Step 1: Checking build artifacts..." -ForegroundColor Yellow
if (-not (Test-Path "$binDir\CopilotExtension.dll")) {
    Write-Host "ERROR: CopilotExtension.dll not found. Run Build first!" -ForegroundColor Red
    exit 1
}
Write-Host "  OK - DLL found" -ForegroundColor Green

# 2. Create temp directory for VSIX contents
$tempDir = "$env:TEMP\CopilotExtension_VSIX"
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempDir | Out-Null
Write-Host "  OK - Temp directory created" -ForegroundColor Green

# 3. Copy manifest
Write-Host "`nStep 2: Copying VSIX manifest..." -ForegroundColor Yellow
Copy-Item "$projectDir\source.extension.vsixmanifest" "$tempDir\extension.vsixmanifest"
Write-Host "  OK - Manifest copied" -ForegroundColor Green

# 4. Copy DLL and dependencies
Write-Host "`nStep 3: Copying binaries..." -ForegroundColor Yellow
Copy-Item "$binDir\CopilotExtension.dll" "$tempDir\"
Write-Host "  OK - CopilotExtension.dll" -ForegroundColor Green

# Copy Newtonsoft.Json if exists
if (Test-Path "$binDir\Newtonsoft.Json.dll") {
    Copy-Item "$binDir\Newtonsoft.Json.dll" "$tempDir\"
    Write-Host "  OK - Newtonsoft.Json.dll" -ForegroundColor Green
}

# 5. Copy Resources
Write-Host "`nStep 4: Copying resources..." -ForegroundColor Yellow
if (Test-Path "$projectDir\Resources") {
    Copy-Item "$projectDir\Resources" "$tempDir\" -Recurse
    Write-Host "  OK - Resources folder" -ForegroundColor Green
}

# 6. Create [Content_Types].xml
Write-Host "`nStep 5: Creating [Content_Types].xml..." -ForegroundColor Yellow
$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="dll" ContentType="application/octet-stream"/>
  <Default Extension="pkgdef" ContentType="text/plain"/>
  <Default Extension="vsixmanifest" ContentType="text/xml"/>
  <Default Extension="png" ContentType="application/octet-stream"/>
  <Default Extension="txt" ContentType="text/plain"/>
</Types>
"@
$contentTypes | Out-File "$tempDir\[Content_Types].xml" -Encoding UTF8
Write-Host "  OK - Content types created" -ForegroundColor Green

# 7. Create pkgdef file
Write-Host "`nStep 6: Creating pkgdef..." -ForegroundColor Yellow
$pkgdef = @"
[$RootKey$\Packages\{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}]
@="CopilotExtension"
"InprocServer32"="$WinDir$\SYSTEM32\MSCOREE.DLL"
"Class"="CopilotExtension.CopilotExtensionPackage"
"CodeBase"="`$PackageFolder$\CopilotExtension.dll"
"@
$pkgdef | Out-File "$tempDir\CopilotExtension.pkgdef" -Encoding UTF8
Write-Host "  OK - pkgdef created" -ForegroundColor Green

# 8. Create VSIX (ZIP file)
Write-Host "`nStep 7: Creating VSIX package..." -ForegroundColor Yellow
if (Test-Path $vsixPath) {
    Remove-Item $vsixPath -Force
}

# Use .NET to create ZIP
Add-Type -Assembly System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $vsixPath)

Write-Host "  OK - VSIX created" -ForegroundColor Green

# 9. Verify VSIX
Write-Host "`nStep 8: Verifying VSIX..." -ForegroundColor Yellow
if (Test-Path $vsixPath) {
    $size = (Get-Item $vsixPath).Length
    Write-Host "  OK - VSIX exists: $size bytes" -ForegroundColor Green
} else {
    Write-Host "  ERROR - VSIX not created!" -ForegroundColor Red
    exit 1
}

# 10. Cleanup
Remove-Item $tempDir -Recurse -Force

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "SUCCESS! VSIX created at:" -ForegroundColor Green
Write-Host "  $vsixPath" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Close ALL Visual Studio instances"
Write-Host "2. Double-click: $vsixPath"
Write-Host "3. Install the extension"
Write-Host "4. Restart Visual Studio"
Write-Host ""
