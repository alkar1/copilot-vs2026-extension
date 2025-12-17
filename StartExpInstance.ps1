# ?? START EXPERIMENTAL INSTANCE

Write-Host "=== COPILOT EXTENSION - START EXP INSTANCE ===" -ForegroundColor Cyan

# Œcie¿ka do VS 2026
$devenv = "C:\Program Files\Microsoft Visual Studio\2026\Community\Common7\IDE\devenv.exe"

# SprawdŸ czy istnieje
if (!(Test-Path $devenv)) {
    Write-Host "? Visual Studio 2026 nie znaleziony!" -ForegroundColor Red
    Write-Host "Œcie¿ka: $devenv" -ForegroundColor Yellow
    
    # Spróbuj znaleŸæ inne wersje
    $vsPath = "C:\Program Files\Microsoft Visual Studio\2026"
    if (Test-Path $vsPath) {
        Write-Host "`nZnalezione edycje:" -ForegroundColor Yellow
        Get-ChildItem $vsPath -Directory | ForEach-Object {
            Write-Host "  - $($_.Name)"
        }
    }
    exit 1
}

Write-Host "? Visual Studio 2026 znaleziony" -ForegroundColor Green

# SprawdŸ czy extension jest zdeployowany
$extPath = "$env:LOCALAPPDATA\Microsoft\VisualStudio\18.0_*Exp\Extensions\CopilotExtension"
$extExists = Test-Path $extPath

if ($extExists) {
    Write-Host "? Extension jest zdeployowany w Exp Instance" -ForegroundColor Green
    $files = Get-ChildItem $extPath -Recurse
    Write-Host "   Plików: $($files.Count)" -ForegroundColor Gray
} else {
    Write-Host "??  Extension NIE jest zdeployowany!" -ForegroundColor Yellow
    Write-Host "   Uruchom najpierw: .\DeployExtension.ps1" -ForegroundColor Yellow
}

Write-Host "`n?? Uruchamiam VS 2026 Experimental Instance..." -ForegroundColor Cyan
Write-Host "   (To mo¿e potrwaæ ~10 sekund)" -ForegroundColor Gray

# Uruchom Exp Instance
Start-Process -FilePath $devenv -ArgumentList "/rootsuffix Exp"

Write-Host "`n? Experimental Instance uruchomiona!" -ForegroundColor Green
Write-Host "`nW nowym oknie VS sprawdŸ:" -ForegroundColor Yellow
Write-Host "1. Extensions > Manage Extensions > Installed" -ForegroundColor White
Write-Host "   ? Szukaj: 'Copilot CLI Extension'" -ForegroundColor Gray
Write-Host "`n2. Tools > Options > Copilot CLI" -ForegroundColor White
Write-Host "   ? Powinno byæ 8 opcji" -ForegroundColor Gray
Write-Host "`n3. Otwórz .cs file i pisz kod" -ForegroundColor White
Write-Host "   ? Sugestie powinny siê pojawiæ" -ForegroundColor Gray

Write-Host "`n?? TIP: Jeœli extension nie widaæ:" -ForegroundColor Cyan
Write-Host "   .\DeployExtension.ps1" -ForegroundColor White
Write-Host "   Potem zamknij i uruchom ponownie Exp Instance" -ForegroundColor White
