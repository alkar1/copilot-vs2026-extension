# Test Runner Script for Copilot Extension
# This script builds the project and runs all tests

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Copilot Extension - Test Runner" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean CopilotExtension.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Clean completed" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore CopilotExtension.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Restore completed" -ForegroundColor Green
Write-Host ""

Write-Host "Step 3: Building solution..." -ForegroundColor Yellow
dotnet build CopilotExtension.sln -c Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Build completed" -ForegroundColor Green
Write-Host ""

Write-Host "Step 4: Running tests..." -ForegroundColor Yellow
Write-Host ""

# Run tests with detailed output
dotnet test CopilotExtension.Tests/CopilotExtension.Tests.csproj `
    --no-build `
    --verbosity normal `
    --logger "console;verbosity=detailed" `
    --collect:"XPlat Code Coverage"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Some tests failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "To run specific test:" -ForegroundColor Yellow
    Write-Host "  dotnet test --filter <TestName>" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To run tests for specific class:" -ForegroundColor Yellow
    Write-Host "  dotnet test --filter FullyQualifiedName~CopilotCliServiceTests" -ForegroundColor Gray
    exit 1
} else {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "? All tests passed successfully!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
}

Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "  - Service Tests: CopilotCliServiceTests" -ForegroundColor Gray
Write-Host "  - Options Tests: CopilotOptionsPageTests" -ForegroundColor Gray
Write-Host "  - Integration Tests: ExtensionIntegrationTests" -ForegroundColor Gray
Write-Host "  - Helper Tests: LanguageDetectionTests, ContextTruncationTests, etc." -ForegroundColor Gray
Write-Host ""

# Additional test commands
Write-Host "Useful test commands:" -ForegroundColor Cyan
Write-Host "  Run specific test:" -ForegroundColor Yellow
Write-Host "    dotnet test --filter GetSuggestionAsync_WithValidInput_ShouldReturnSuggestion" -ForegroundColor Gray
Write-Host ""
Write-Host "  Run tests with coverage:" -ForegroundColor Yellow
Write-Host "    dotnet test --collect:`"XPlat Code Coverage`"" -ForegroundColor Gray
Write-Host ""
Write-Host "  Run tests in watch mode:" -ForegroundColor Yellow
Write-Host "    dotnet watch test" -ForegroundColor Gray
Write-Host ""

exit 0
