# ?? Przewodnik Diagnostyczny - Copilot Extension

## Jak sprawdziæ czy rozszerzenie siê ³aduje w Experimental Instance

### ? Krok 1: SprawdŸ Output Window w pierwszym VS (debuguj¹cym)

1. **W Visual Studio które debuguje rozszerzenie:**
   - Naciœnij `F5` aby uruchomiæ Experimental Instance
   - PrzejdŸ do **View > Output** (lub `Ctrl+Alt+O`)
   - Z dropdown wybierz **Debug**

2. **Szukaj wpisów:**
   ```
   ========================================
   === COPILOT EXTENSION: STARTING INITIALIZATION ===
   === Time: 2024-XX-XX XX:XX:XX ===
   === Package GUID: f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f ===
   ========================================
   ```

3. **Jeœli widzisz:**
   - ? **"INITIALIZATION COMPLETE!"** - Rozszerzenie za³adowane!
   - ? **"INITIALIZATION FAILED!"** - SprawdŸ b³¹d w logu
   - ? **Brak wpisów** - Rozszerzenie nie startuje

---

### ? Krok 2: SprawdŸ Extensions Manager w Experimental Instance

**W otwartej Experimental Instance:**

1. PrzejdŸ do: **Extensions > Manage Extensions**
2. Kliknij zak³adkê **Installed**
3. Poszukaj: **"Copilot CLI Extension"**

**Jeœli jest na liœcie:**
- ? Rozszerzenie zainstalowane
- SprawdŸ czy jest **enabled** (nie disabled)
- Mo¿e wymagaæ restartu VS

**Jeœli NIE MA na liœcie:**
- ? Rozszerzenie nie zosta³o zainstalowane w Exp Instance
- Zobacz Krok 5: Troubleshooting

---

### ? Krok 3: SprawdŸ Menu w Experimental Instance

1. **Otwórz dowolny plik `.cs`** w Experimental Instance
2. Kliknij menu **Edit**
3. Szukaj: **"Get Copilot Suggestion"**

**Jeœli menu jest widoczne:**
- ? Rozszerzenie za³adowa³o siê poprawnie!
- Mo¿esz klikn¹æ opcjê lub u¿yæ `Ctrl+Alt+.`

**Jeœli menu NIE JEST widoczne:**
- ? Command nie zosta³ zarejestrowany
- SprawdŸ Output > Debug dla b³êdów

---

### ? Krok 4: SprawdŸ Tools > Options

**W Experimental Instance:**

1. PrzejdŸ do: **Tools > Options**
2. W lewym drzewie szukaj: **"Copilot CLI"**
3. Powinna byæ kategoria: **"Copilot CLI > General"**

**Jeœli kategoria istnieje:**
- ? Options Page za³adowana
- SprawdŸ ustawienia (Enable Copilot, CLI Path, etc.)

**Jeœli kategoria NIE ISTNIEJE:**
- ? Package nie za³adowa³ Options Page
- SprawdŸ Output > Debug dla b³êdów

---

### ? Krok 5: Troubleshooting

#### Problem: Brak logów w Output > Debug

**Rozwi¹zanie:**
1. Zamknij Experimental Instance
2. W debuguj¹cym VS, ustaw breakpoint w:
   ```
   CopilotExtensionPackage.cs ? InitializeAsync (linia ~22)
   ```
3. Naciœnij `F5` ponownie
4. Jeœli breakpoint NIE zosta³ trafiony:
   - Package nie ³aduje siê w ogóle
   - SprawdŸ Build Output dla b³êdów

#### Problem: "Package load failed"

**Mo¿liwe przyczyny:**
1. **Brak `Menus.ctmenu`**
   - SprawdŸ: `bin\Debug\net8.0-windows\Menus.ctmenu`
   - Jeœli brak: Problem z kompilacj¹ VSCT

2. **Niepoprawny GUID**
   - SprawdŸ w `source.extension.vsixmanifest`:
     ```xml
     <Identity Id="CopilotExtension.f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f" ...
     ```
   - Musi pasowaæ do `CopilotExtensionPackage.cs`:
     ```csharp
     public const string PackageGuidString = "f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f";
     ```

3. **Dependency missing**
   - SprawdŸ czy wszystkie NuGet packages s¹ zainstalowane
   - Run: `dotnet restore`

#### Problem: Menu nie pokazuje siê

**Rozwi¹zanie:**
1. SprawdŸ GUID w `VSCommandTable.vsct`:
   ```xml
   <GuidSymbol name="guidCopilotExtensionPackage" 
               value="{f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f}" />
   ```

2. SprawdŸ czy command ID w `CopilotCommand.cs` pasuje:
   ```csharp
   public const int CommandId = 0x0100;
   ```

3. SprawdŸ czy `Menus.ctmenu` jest w output folder:
   ```
   bin\Debug\net8.0-windows\Menus.ctmenu
   ```

---

### ? Krok 6: Activity Log (Zaawansowane)

**Experimental Instance tworzy szczegó³owy log:**

```powershell
# Lokalizacja Activity Log
$expLogPath = "$env:APPDATA\Microsoft\VisualStudio\17.0_*Exp\ActivityLog.xml"
notepad (Get-Item $expLogPath | Select-Object -First 1).FullName
```

**Szukaj w pliku:**
- Wpisy zawieraj¹ce: `CopilotExtension`
- Wpisy zawieraj¹ce: `f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f`
- B³êdy: `<type>Error</type>`
- Ostrze¿enia: `<type>Warning</type>`

---

### ? Krok 7: Reset Experimental Instance

**Jeœli nic nie dzia³a, zresetuj Exp Instance:**

1. **Zamknij wszystkie instancje Visual Studio**

2. **Uruchom w PowerShell jako Admin:**
   ```powershell
   cd "C:\Program Files\Microsoft Visual Studio\2022\Preview\VSSDK\VisualStudioIntegration\Tools\Bin"
   .\CreateExpInstance.exe /Reset /VSInstance=17.0 /RootSuffix=Exp
   ```

3. **Przebuduj rozszerzenie:**
   ```powershell
   cd C:\PROJ\VS2026\Copilot
   dotnet clean
   dotnet build -c Debug
   ```

4. **Uruchom ponownie F5**

---

## ?? Checklist Diagnostyczna

PrzejdŸ przez tê listê krok po kroku:

### Build Phase
- [ ] `dotnet build` - Build succeeds bez b³êdów
- [ ] `bin\Debug\net8.0-windows\CopilotExtension.dll` - DLL istnieje
- [ ] `bin\Debug\net8.0-windows\Menus.ctmenu` - VSCT compiled
- [ ] `bin\Debug\CopilotExtension.vsix` - VSIX package created

### Debug Phase
- [ ] F5 uruchamia Experimental Instance
- [ ] Output > Debug pokazuje logi "COPILOT EXTENSION"
- [ ] Breakpoint w InitializeAsync siê wywo³uje
- [ ] InitializeAsync koñczy siê bez wyj¹tków

### UI Phase  
- [ ] Extensions > Installed pokazuje "Copilot CLI Extension"
- [ ] Edit menu zawiera "Get Copilot Suggestion"
- [ ] Ctrl+Alt+. trigger command
- [ ] Tools > Options zawiera "Copilot CLI"

### Runtime Phase
- [ ] Klikniêcie menu pokazuje message box lub insert suggestion
- [ ] Debug Output pokazuje "Command triggered!"
- [ ] Brak exception w Output > Debug

---

## ??? Dodatkowe Narzêdzia Diagnostyczne

### Test Command Manually

**W Experimental Instance:**

1. Otwórz **Tools > Command Window** (`Ctrl+Alt+A`)
2. Wpisz:
   ```
   Copilot.GetSuggestion
   ```
3. Naciœnij Enter

**Jeœli dzia³a:**
- ? Command jest zarejestrowany
- Problem mo¿e byæ tylko z menu/keybinding

**Jeœli nie dzia³a:**
- ? Command nie zosta³ zarejestrowany
- SprawdŸ InitializeAsync errors

---

### Check Package GUID Registration

**W Experimental Instance, otwórz Command Window:**

```
Tools.Extension.List
```

Szukaj wpisu zawieraj¹cego `f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f`

---

## ? FAQ

### Q: Widzê logi "INITIALIZATION COMPLETE" ale menu nie ma
**A:** Problem z VSCT compilation. SprawdŸ czy `Menus.ctmenu` jest w output folder.

### Q: Breakpoint w InitializeAsync nie jest trafiony
**A:** Package nie ³aduje siê. SprawdŸ:
1. Activity Log dla b³êdów
2. Build Output dla b³êdów kompilacji
3. GUID matching w manifests

### Q: "Get Copilot Suggestion" nie robi nic
**A:** SprawdŸ Output > Debug po klikniêciu. Powinny byæ logi "Execute: Command triggered!"

### Q: Experimental Instance nie uruchamia siê
**A:** 
1. SprawdŸ czy masz zainstalowany VS SDK
2. SprawdŸ czy project ma `<StartArguments>/rootsuffix Exp</StartArguments>`
3. SprawdŸ DevEnvDir environment variable

---

## ?? Quick Test Script

**Uruchom ten skrypt w pierwszym VS (debuguj¹cym):**

```powershell
# Quick Diagnostic Script
Write-Host "=== COPILOT EXTENSION DIAGNOSTIC ===" -ForegroundColor Cyan
Write-Host ""

# Check build outputs
$dllPath = ".\CopilotExtension\bin\Debug\net8.0-windows\CopilotExtension.dll"
$ctmenuPath = ".\CopilotExtension\bin\Debug\net8.0-windows\Menus.ctmenu"
$vsixPath = ".\CopilotExtension\bin\Debug\CopilotExtension.vsix"

Write-Host "Build Artifacts:" -ForegroundColor Yellow
Write-Host "  DLL:     " -NoNewline
if (Test-Path $dllPath) { Write-Host "? EXISTS" -ForegroundColor Green } else { Write-Host "? MISSING" -ForegroundColor Red }

Write-Host "  CTMENU:  " -NoNewline
if (Test-Path $ctmenuPath) { Write-Host "? EXISTS" -ForegroundColor Green } else { Write-Host "? MISSING" -ForegroundColor Red }

Write-Host "  VSIX:    " -NoNewline
if (Test-Path $vsixPath) { Write-Host "? EXISTS" -ForegroundColor Green } else { Write-Host "? MISSING" -ForegroundColor Red }

Write-Host ""
Write-Host "Activity Log Location:" -ForegroundColor Yellow
$expLog = Get-Item "$env:APPDATA\Microsoft\VisualStudio\17.0_*Exp\ActivityLog.xml" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($expLog) {
    Write-Host "  $($expLog.FullName)" -ForegroundColor Gray
} else {
    Write-Host "  Not found (Exp instance not yet run)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Press F5 to launch Experimental Instance"
Write-Host "2. Check Output > Debug for initialization logs"
Write-Host "3. Check Extensions > Manage Extensions > Installed"
Write-Host "4. Open a .cs file and check Edit menu"
Write-Host ""
```

---

**Stworzono:** 2024  
**Cel:** Diagnostyka ³adowania rozszerzenia w Experimental Instance  
**Status:** ? Ready to use
