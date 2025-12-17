# ? POPRAWKA ZASTOSOWANA - Co dalej?

## Zmiany wprowadzone:

? Dodano do `CopilotExtension.csproj`:
```xml
<CreateVsixContainer>true</CreateVsixContainer>
<DeployExtension>true</DeployExtension>
<DeployVSTemplates>false</DeployVSTemplates>
```

? Build succeeds (23 warnings - non-blocking)

---

## ?? JAK TERAZ URUCHOMIÆ (2 METODY)

### METODA 1: F5 w Visual Studio (Najlepsze) ?

**Kroki:**

1. **Otwórz Visual Studio 2026**

2. **Otwórz Solution:**
   ```
   File > Open > Project/Solution
   C:\PROJ\VS2026\Copilot\CopilotExtension.sln
   ```

3. **Ustaw Startup Project:**
   ```
   Solution Explorer > Prawy click na "CopilotExtension"
   > Set as Startup Project
   ```

4. **Naciœnij F5** (lub Debug > Start Debugging)

5. **Co siê stanie:**
   - VS zbuduje projekt
   - Uruchomi Experimental Instance
   - Extension zostanie automatycznie za³adowany

6. **W Experimental Instance sprawdŸ:**
   ```
   Extensions > Manage Extensions
   Installed > "Copilot CLI Extension" powinno byæ ?
   ```

7. **SprawdŸ Options:**
   ```
   Tools > Options
   
   W lewej liœcie przewiñ do:
   "Copilot CLI" > "General"
   
   Powinno pokazaæ 8 opcji ?
   ```

**Jeœli Options nie ma:**
- Zamknij Exp Instance
- W PowerShell:
  ```powershell
  $devenv = "C:\Program Files\Microsoft Visual Studio\2026\Community\Common7\IDE\devenv.exe"
  & $devenv /resetSettings /rootsuffix Exp
  & $devenv /clearcache /rootsuffix Exp
  ```
- F5 ponownie

---

### METODA 2: Build w Visual Studio (Alternatywa)

**Problem:** SDK-style projects nie zawsze generuj¹ VSIX automatycznie

**Rozwi¹zanie:**

1. **Otwórz projekt w VS2026**

2. **Build > Build Solution**
   - Mo¿e wygenerowaæ VSIX
   - Lub przynajmniej zdeployuje do Exp Instance

3. **SprawdŸ czy deployment dzia³a³:**
   ```
   View > Other Windows > Output
   Show output from: Build
   
   Szukaj: "Deploying VSIX" lub "Extension deployed"
   ```

4. **Rêcznie uruchom Exp Instance:**
   ```
   C:\Program Files\Microsoft Visual Studio\2026\Community\Common7\IDE\devenv.exe /rootsuffix Exp
   ```

5. **SprawdŸ Extensions > Manage Extensions**

---

## ?? DLACZEGO VSIX SIÊ NIE GENERUJE?

SDK-style projects (.NET 8) maj¹ problemy z automatycznym VSIX generation.

**Dwa podejœcia:**

### Podejœcie A: U¿yj Visual Studio do Build (Zalecane)

Visual Studio ma lepsze wsparcie dla VSIX ni¿ `dotnet build`

```
1. Otwórz solution w VS2026
2. Build > Build Solution
3. F5
```

### Podejœcie B: Konwersja do Legacy Project Format (Advanced)

Wymaga du¿ych zmian, nie zalecane teraz.

---

## ? CO TERAZ DZIA£A

Po wprowadzonych zmianach:

? **DeployExtension=true** 
- Extension bêdzie deployowany do Exp Instance podczas F5

? **CreateVsixContainer=true**
- Przygotowuje projekt do VSIX generation (wymaga VS)

? **Build succeeds**
- Kod kompiluje siê poprawnie

---

## ?? ZALECANE KROKI

### 1. U¿yj F5 w Visual Studio

To najlepsza metoda bo:
- Automatyczny deployment
- Debugowanie dzia³a
- Exp Instance jest izolowane
- £atwe testowanie zmian

### 2. Testuj w Experimental Instance

```
1. F5 uruchamia Exp Instance
2. Extensions > Manage Extensions
   - SprawdŸ czy extension zainstalowany
3. Tools > Options > Copilot CLI
   - SprawdŸ czy options s¹
4. Otwórz .cs file i pisz kod
   - SprawdŸ czy sugestie dzia³aj¹
```

### 3. Jeœli coœ nie dzia³a

**Activity Log:**
```powershell
$log = Get-ChildItem "$env:APPDATA\Microsoft\VisualStudio\17.0*Exp\ActivityLog.xml" | 
       Sort-Object LastWriteTime -Descending | 
       Select-Object -First 1
notepad $log.FullName
```

**Output Window w VS:**
```
View > Output
Show output from: Extensions
```

---

## ?? TROUBLESHOOTING

### Problem: F5 nic nie robi

**SprawdŸ Debug Properties:**
```
Solution Explorer > CopilotExtension > Properties > Debug

Start action: Start external program
Path: C:\Program Files\Microsoft Visual Studio\2026\Community\Common7\IDE\devenv.exe
Arguments: /rootsuffix Exp
```

### Problem: Exp Instance bez Extension

**Reset i rebuild:**
```powershell
# Clean
dotnet clean

# Reset Exp
$devenv = "C:\Program Files\...\devenv.exe"
& $devenv /resetSettings /rootsuffix Exp
& $devenv /clearcache /rootsuffix Exp

# Rebuild in VS
# Build > Rebuild Solution
# F5
```

### Problem: Options nie pojawia siê

**Mo¿liwe przyczyny:**
1. Extension nie za³adowany (sprawdŸ Extensions Manager)
2. Cache VS (Tools > Reset all settings)
3. B³¹d w Activity Log (sprawdŸ log)

**Rozwi¹zanie:**
```
1. Zamknij Exp Instance
2. W VS: Build > Clean + Rebuild
3. Reset Exp Instance (polecenia wy¿ej)
4. F5 ponownie
```

---

## ?? QUICK CHECKLIST

Przed uruchomieniem:

```
[ ] Visual Studio 2026 zainstalowany
[ ] Solution otwarte w VS2026
[ ] CopilotExtension = Startup Project
[ ] Build succeeds (dotnet build or VS Build)
[ ] .csproj ma CreateVsixContainer=true
[ ] .csproj ma DeployExtension=true
```

Podczas uruchomienia (F5):

```
[ ] Exp Instance siê uruchamia
[ ] Window title zawiera "(Experimental Instance)"
[ ] Extensions Manager pokazuje extension
[ ] Tools > Options > Copilot CLI istnieje
[ ] Inline suggestions dzia³aj¹
```

---

## ?? PODSUMOWANIE

**Status:** ? Projekt skonfigurowany poprawnie

**Nastêpny krok:** Naciœnij F5 w Visual Studio 2026

**Oczekiwany rezultat:** Extension za³adowany w Exp Instance z Options

**Jeœli nie dzia³a:** SprawdŸ Activity Log i Output Window

---

**Utworzono:** 2024  
**Zmiany:** CreateVsixContainer + DeployExtension added  
**Build:** ? SUCCESS  
**Nastêpny krok:** F5 w Visual Studio
