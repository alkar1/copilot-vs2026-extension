# Manual Test Guide for Copilot Extension

## Przygotowanie do testów

1. **Zainstaluj GitHub Copilot CLI**
   ```powershell
   gh extension install github/gh-copilot
   gh auth login
   ```

2. **Zbuduj extension**
   ```powershell
   dotnet build CopilotExtension/CopilotExtension.csproj -c Release
   ```

3. **Zainstaluj VSIX**
   - ZnajdŸ `CopilotExtension\bin\Release\CopilotExtension.vsix`
   - Zamknij wszystkie instancje VS
   - Uruchom plik VSIX

## Test 1: Automatyczne testy jednostkowe

### Uruchom wszystkie testy
```powershell
.\RunTests.ps1
```

### Uruchom konkretn¹ kategoriê testów
```powershell
# Testy serwisu CLI
dotnet test --filter "CopilotCliServiceTests"

# Testy opcji
dotnet test --filter "CopilotOptionsPageTests"

# Testy integracyjne
dotnet test --filter "ExtensionIntegrationTests"

# Testy helperów
dotnet test --filter "LanguageDetectionTests"
```

### Uruchom konkretny test
```powershell
dotnet test --filter "GetSuggestionAsync_WithValidInput_ShouldReturnSuggestion"
```

## Test 2: Manualne testowanie w Visual Studio

### Test 2.1: Inline Suggestions (C#)

1. Otwórz Visual Studio 2026
2. Utwórz nowy projekt C# Console App
3. W pliku `Program.cs` napisz:
   ```csharp
   public class Calculator
   {
       public int Add
   ```
4. **Oczekiwany rezultat:** Po ~500ms pojawi siê szara sugestia, np. `(int a, int b)`
5. Naciœnij **Tab** aby zaakceptowaæ
6. **Status:** ? Pass / ? Fail

### Test 2.2: Manual Trigger (JavaScript)

1. Utwórz plik `test.js`
2. Napisz:
   ```javascript
   function fetchData
   ```
3. Naciœnij **Ctrl+Alt+.**
4. **Oczekiwany rezultat:** Sugestia zostanie wstawiona natychmiast
5. **Status:** ? Pass / ? Fail

### Test 2.3: Async Code Suggestions (C#)

1. Utwórz nowy plik C#
2. Napisz:
   ```csharp
   public async Task<string> FetchDataAsync
   ```
3. Poczekaj na sugestiê lub naciœnij Ctrl+Alt+.
4. **Oczekiwany rezultat:** Sugestia zawiera `async/await` pattern
5. **Status:** ? Pass / ? Fail

### Test 2.4: LINQ Suggestions (C#)

1. Napisz:
   ```csharp
   var users = GetUsers();
   var activeUsers = users.Where
   ```
2. Poczekaj na sugestiê
3. **Oczekiwany rezultat:** Sugestia zawiera lambda expression
4. **Status:** ? Pass / ? Fail

### Test 2.5: Multi-language Support

Test dla ka¿dego jêzyka:

**C#**
```csharp
public class Test
{
    public void Method
```

**Python**
```python
def calculate_sum
```

**TypeScript**
```typescript
interface User
```

**Java**
```java
public class Calculator
{
    public int add
```

Dla ka¿dego: SprawdŸ czy sugestie s¹ odpowiednie dla jêzyka.

### Test 2.6: Configuration Options

1. IdŸ do **Tools > Options > Copilot CLI > General**
2. Zmieñ **Auto-suggest Delay** na `1000`
3. Testuj czy sugestie pojawiaj¹ siê póŸniej
4. Zmieñ **Suggestion Opacity** na `25`
5. SprawdŸ czy sugestie s¹ bardziej przezroczyste
6. **Status:** ? Pass / ? Fail

### Test 2.7: Keyboard Shortcuts

1. Napisz kod
2. Naciœnij **Ctrl+Alt+.** - powinno pokazaæ sugestiê
3. Gdy sugestia jest widoczna, naciœnij **Tab** - powinno zaakceptowaæ
4. Naciœnij **Esc** - powinno anulowaæ
5. **Status:** ? Pass / ? Fail

### Test 2.8: Error Handling

1. **Scenario A: Brak Copilot CLI**
   - Odinstaluj/wy³¹cz Copilot CLI
   - Spróbuj u¿yæ extension
   - **Oczekiwany rezultat:** Graceful error message
   
2. **Scenario B: Timeout**
   - Ustaw **Timeout** na `1` sekundê w opcjach
   - Spróbuj u¿yæ z du¿ym kontekstem
   - **Oczekiwany rezultat:** Timeout error bez crash

3. **Scenario C: Invalid CLI Path**
   - Ustaw **Copilot CLI Path** na nieprawid³ow¹ œcie¿kê
   - Spróbuj u¿yæ extension
   - **Oczekiwany rezultat:** Error message

### Test 2.9: Performance

1. Otwórz du¿y plik (>1000 linii)
2. PrzejdŸ na koniec pliku
3. Zacznij pisaæ kod
4. **Mierz:** Czas do pokazania sugestii
5. **Oczekiwany rezultat:** < 2 sekundy
6. **Status:** ? Pass / ? Fail

### Test 2.10: Context Awareness

1. Napisz klasê z kilkoma metodami:
   ```csharp
   public class UserService
   {
       private IRepository _repo;
       
       public UserService(IRepository repo)
       {
           _repo = repo;
       }
       
       public async Task<User> GetUser
   ```
2. **Oczekiwany rezultat:** Sugestia u¿ywa `_repo` z kontekstu
3. **Status:** ? Pass / ? Fail

## Test 3: Stress Testing

### Test 3.1: Rapid Typing
1. Szybko pisz kod bez przerw
2. **Oczekiwany rezultat:** Extension nie spowalnia VS
3. **Status:** ? Pass / ? Fail

### Test 3.2: Multiple Files
1. Otwórz 10+ plików
2. Prze³¹czaj miêdzy nimi u¿ywaj¹c extension
3. **Oczekiwany rezultat:** Brak memory leaks, sprawne dzia³anie
4. **Status:** ? Pass / ? Fail

### Test 3.3: Long Session
1. U¿ywaj extension przez 30+ minut
2. SprawdŸ Task Manager dla zu¿ycia pamiêci
3. **Oczekiwany rezultat:** Stabilne zu¿ycie pamiêci
4. **Status:** ? Pass / ? Fail

## Test 4: Edge Cases

### Test 4.1: Empty File
1. Utwórz pusty plik
2. Zacznij pisaæ pierwszy znak
3. **Status:** ? Pass / ? Fail

### Test 4.2: Special Characters
```csharp
string text = "Test with \"quotes\" and \\ backslashes";
var next = 
```
**Status:** ? Pass / ? Fail

### Test 4.3: Very Long Line
1. Napisz liniê >500 znaków
2. Poproœ o sugestiê
3. **Status:** ? Pass / ? Fail

### Test 4.4: Unicode Characters
```csharp
// Polski: ¿ó³æ
// Chinese: ??
// Emoji: ??
var test = 
```
**Status:** ? Pass / ? Fail

## Test Results Summary

| Test Category | Total | Passed | Failed | Notes |
|--------------|-------|--------|--------|-------|
| Unit Tests | ? | ? | ? | |
| Inline Suggestions | 1 | | | |
| Manual Trigger | 1 | | | |
| Async Code | 1 | | | |
| LINQ | 1 | | | |
| Multi-language | 5 | | | |
| Configuration | 1 | | | |
| Keyboard Shortcuts | 1 | | | |
| Error Handling | 3 | | | |
| Performance | 1 | | | |
| Context Awareness | 1 | | | |
| Stress Testing | 3 | | | |
| Edge Cases | 4 | | | |
| **TOTAL** | **23+** | | | |

## Raportowanie problemów

Jeœli test nie przeszed³:

1. Zapisz dok³adne kroki reprodukcji
2. Za³¹cz screenshot jeœli mo¿liwe
3. SprawdŸ Output Window > Copilot CLI dla logów
4. SprawdŸ Event Viewer dla b³êdów VS
5. Zg³oœ issue na GitHub z wszystkimi informacjami

## Debug Mode

Aby w³¹czyæ szczegó³owe logi:
1. **Tools > Options > Copilot CLI > General**
2. Zaznacz **Debug Mode**
3. SprawdŸ Output Window podczas testów
