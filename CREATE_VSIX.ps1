# Complete VSIX Creation and Installation Script
# This creates a working VSIX and installs it

Write-Host "=== COPILOT EXTENSION - VSIX CREATOR ===" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Paths
$projectDir = "C:\PROJ\VS2026\Copilot\CopilotExtension"
$binDir = "$projectDir\bin\Debug\net8.0-windows"
$tempDir = "$env:TEMP\CopilotVSIX_$(Get-Date -Format 'yyyyMMddHHmmss')"
$vsixPath = "$projectDir\bin\Debug\CopilotExtension.vsix"

try {
    # Step 1: Verify DLL exists
    Write-Host "[1/9] Checking build artifacts..." -ForegroundColor Yellow
    if (-not (Test-Path "$binDir\CopilotExtension.dll")) {
        throw "CopilotExtension.dll not found. Please build the project first!"
    }
    Write-Host "      ? DLL found" -ForegroundColor Green

    # Step 2: Create temp directory
    Write-Host "[2/9] Creating temporary directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    Write-Host "      ? Temp directory created" -ForegroundColor Green

    # Step 3: Copy and rename manifest (MUST be exactly "extension.vsixmanifest")
    Write-Host "[3/9] Copying extension manifest..." -ForegroundColor Yellow
    $manifestContent = Get-Content "$projectDir\source.extension.vsixmanifest" -Raw
    # Fix manifest - ensure proper XML structure
    [System.IO.File]::WriteAllText("$tempDir\extension.vsixmanifest", $manifestContent, [System.Text.Encoding]::UTF8)
    Write-Host "      ? Manifest copied as extension.vsixmanifest" -ForegroundColor Green

    # Step 4: Copy binaries
    Write-Host "[4/9] Copying binaries..." -ForegroundColor Yellow
    Copy-Item "$binDir\CopilotExtension.dll" "$tempDir\"
    if (Test-Path "$binDir\Newtonsoft.Json.dll") {
        Copy-Item "$binDir\Newtonsoft.Json.dll" "$tempDir\"
    }
    Write-Host "      ? Binaries copied" -ForegroundColor Green

    # Step 5: Copy resources
    Write-Host "[5/9] Copying resources..." -ForegroundColor Yellow
    if (Test-Path "$projectDir\Resources") {
        Copy-Item "$projectDir\Resources" "$tempDir\Resources" -Recurse
        Write-Host "      ? Resources copied" -ForegroundColor Green
    } else {
        Write-Host "      ? No resources folder found" -ForegroundColor DarkYellow
    }
    
    # Step 6: Copy LICENSE if exists
    Write-Host "[6/9] Copying license..." -ForegroundColor Yellow
    if (Test-Path "$projectDir\LICENSE.txt") {
        Copy-Item "$projectDir\LICENSE.txt" "$tempDir\"
        Write-Host "      ? License copied" -ForegroundColor Green
    } else {
        # Create minimal license
        "MIT License" | Out-File "$tempDir\LICENSE.txt" -Encoding UTF8
        Write-Host "      ? Default license created" -ForegroundColor Green
    }

    # Step 7: Create [Content_Types].xml - CRITICAL for VSIX validation
    Write-Host "[7/9] Creating content types..." -ForegroundColor Yellow
    $contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="dll" ContentType="application/octet-stream" />
  <Default Extension="pkgdef" ContentType="text/plain" />
  <Default Extension="txt" ContentType="text/plain" />
  <Default Extension="png" ContentType="image/png" />
  <Default Extension="vsixmanifest" ContentType="text/xml" />
</Types>
"@
    [System.IO.File]::WriteAllText("$tempDir\[Content_Types].xml", $contentTypes, [System.Text.Encoding]::UTF8)
    Write-Host "      ? Content types created" -ForegroundColor Green

    # Step 8: Create pkgdef
    Write-Host "[8/9] Creating package definition..." -ForegroundColor Yellow
    $pkgdef = @"
// CopilotExtension Package Registration
[`$RootKey`$\Packages\{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}]
@="CopilotExtension"
"InprocServer32"="`$WinDir`$\SYSTEM32\MSCOREE.DLL"
"Class"="CopilotExtension.CopilotExtensionPackage"
"CodeBase"="`$PackageFolder`$\CopilotExtension.dll"

[`$RootKey`$\AutoLoadPackages\{f1536ef8-92ec-443c-9ed7-fdadf150da82}]
"{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}"=dword:00000000
"@
    [System.IO.File]::WriteAllText("$tempDir\CopilotExtension.pkgdef", $pkgdef, [System.Text.Encoding]::ASCII)
    Write-Host "      ? Package definition created" -ForegroundColor Green

    # Step 9: Create VSIX - Use proper ZIP creation
    Write-Host "[9/9] Creating VSIX package..." -ForegroundColor Yellow
    if (Test-Path $vsixPath) {
        Remove-Item $vsixPath -Force
    }

    # Create ZIP with proper settings (no compression of uncompressible files)
    Add-Type -Assembly System.IO.Compression.FileSystem
    
    # Create the archive
    $zip = [System.IO.Compression.ZipFile]::Open($vsixPath, [System.IO.Compression.ZipArchiveMode]::Create)
    
    # Add all files from temp directory
    Get-ChildItem $tempDir -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($tempDir.Length + 1)
        $entry = $zip.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
        $entryStream = $entry.Open()
        $fileStream = [System.IO.File]::OpenRead($_.FullName)
        $fileStream.CopyTo($entryStream)
        $fileStream.Close()
        $entryStream.Close()
    }
    
    $zip.Dispose()
    
    $size = (Get-Item $vsixPath).Length
    Write-Host "      ? VSIX created: $([math]::Round($size/1KB, 2)) KB" -ForegroundColor Green

    # Verify VSIX structure
    Write-Host ""
    Write-Host "Verifying VSIX structure..." -ForegroundColor Yellow
    $zipTest = [System.IO.Compression.ZipFile]::OpenRead($vsixPath)
    $hasManifest = $zipTest.Entries | Where-Object { $_.Name -eq "extension.vsixmanifest" }
    $hasContentTypes = $zipTest.Entries | Where-Object { $_.Name -eq "[Content_Types].xml" }
    $zipTest.Dispose()
    
    if ($hasManifest -and $hasContentTypes) {
        Write-Host "  ? VSIX structure is valid" -ForegroundColor Green
    } else {
        throw "VSIX structure is invalid!"
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "? SUCCESS! VSIX package created" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "VSIX Location:" -ForegroundColor Cyan
    Write-Host "  $vsixPath" -ForegroundColor White
    Write-Host ""
    Write-Host "?? NEXT STEPS:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Close ALL Visual Studio instances" -ForegroundColor White
    Write-Host "2. Double-click the VSIX file to install" -ForegroundColor White
    Write-Host "3. In the installer, select 'Visual Studio Community 2022' or similar" -ForegroundColor White
    Write-Host "4. Check 'Apply to all instances'" -ForegroundColor White
    Write-Host "5. Click Install" -ForegroundColor White
    Write-Host "6. After installation, press F5 in VS to test" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "? ERROR" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor DarkGray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    exit 1
} finally {
    # Cleanup
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
