# ?? START EXPERIMENTAL INSTANCE

Write-Host "=== COPILOT EXTENSION - START EXP INSTANCE ===" -ForegroundColor Cyan

# Œcie¿ka do VS (wersja 18 = VS 2025)
$devenv = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe"

# SprawdŸ czy istnieje
if (!(Test-Path $devenv)) {
    Write-Host "? Visual Studio 18 nie znaleziony!" -ForegroundColor Red
    Write-Host "Œcie¿ka: $devenv" -ForegroundColor Yellow
    
    # Spróbuj znaleŸæ inne wersje
    $vsBasePath = "C:\Program Files\Microsoft Visual Studio"
    if (Test-Path $vsBasePath) {
        Write-Host "`nZnalezione wersje VS:" -ForegroundColor Yellow
        Get-ChildItem $vsBasePath -Directory | ForEach-Object {
            Write-Host "  - $($_.Name)"
            Get-ChildItem $_.FullName -Directory | ForEach-Object {
                Write-Host "    - $($_.Name)"
            }
        }
    }
    exit 1
}

Write-Host "? Visual Studio 18 znaleziony" -ForegroundColor Green

# SprawdŸ czy extension jest zdeployowany
$extPath = "$env:LOCALAPPDATA\Microsoft\VisualStudio\18.0_*Exp\Extensions\CopilotExtension"
$extFolder = Get-Item $extPath -ErrorAction SilentlyContinue

if ($extFolder) {
    Write-Host "? Extension jest zdeployowany w Exp Instance" -ForegroundColor Green
    $files = Get-ChildItem $extFolder.FullName -Recurse
    Write-Host "   Plików: $($files.Count)" -ForegroundColor Gray
} else {
    Write-Host "??  Extension NIE jest zdeployowany!" -ForegroundColor Yellow
    Write-Host "   Uruchom najpierw: .\DeployExtension.ps1" -ForegroundColor Yellow
}

Write-Host "`n?? Uruchamiam VS 18 Experimental Instance..." -ForegroundColor Cyan
Write-Host "   (To mo¿e potrwaæ ~10 sekund)" -ForegroundColor Gray

# Uruchom Exp Instance
Start-Process -FilePath $devenv -ArgumentList "/rootsuffix Exp"

Write-Host "`n? Experimental Instance uruchomiona!" -ForegroundColor Green
Write-Host "`n??  WA¯NE: GitHub Authentication" -ForegroundColor Yellow
Write-Host "Jeœli zobaczysz b³¹d 'You are not signed into GitHub':" -ForegroundColor White
Write-Host "1. W VS Exp: File > Account Settings" -ForegroundColor Gray
Write-Host "2. Sign in with GitHub" -ForegroundColor Gray
Write-Host "3. Autoryzuj GitHub Copilot" -ForegroundColor Gray

Write-Host "`nW nowym oknie VS sprawdŸ:" -ForegroundColor Cyan
Write-Host "1. Extensions > Manage Extensions > Installed" -ForegroundColor White
Write-Host "   ? Szukaj: 'Copilot CLI Extension'" -ForegroundColor Gray
Write-Host "`n2. Tools > Options > Copilot CLI" -ForegroundColor White
Write-Host "   ? Powinno byæ 8 opcji" -ForegroundColor Gray
Write-Host "`n3. File > Account Settings" -ForegroundColor White
Write-Host "   ? Zaloguj siê do GitHub jeœli potrzeba" -ForegroundColor Gray
Write-Host "`n4. Otwórz .cs file i pisz kod" -ForegroundColor White
Write-Host "   ? Sugestie powinny siê pojawiæ (po zalogowaniu)" -ForegroundColor Gray

Write-Host "`n?? TIP: Jeœli extension nie widaæ:" -ForegroundColor Cyan
Write-Host "   .\DeployExtension.ps1" -ForegroundColor White
Write-Host "   Potem zamknij i uruchom ponownie Exp Instance" -ForegroundColor White
