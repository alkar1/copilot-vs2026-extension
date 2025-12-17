# Szybki Start - Copilot CLI Extension

## Krok 1: Zainstaluj GitHub Copilot CLI

```powershell
# Opcja 1: Przez GitHub CLI
gh extension install github/gh-copilot

# Opcja 2: SprawdŸ instalacjê
gh copilot --version
```

## Krok 2: Zbuduj extension

```powershell
# Z katalogu g³ównego projektu
cd CopilotExtension
dotnet build -c Release
```

## Krok 3: Zainstaluj w Visual Studio

1. Zamknij wszystkie instancje Visual Studio 2026
2. ZnajdŸ plik: `CopilotExtension\bin\Release\CopilotExtension.vsix`
3. Uruchom go podwójnym klikniêciem
4. Postêpuj zgodnie z instrukcjami instalatora

## Krok 4: Konfiguracja (opcjonalnie)

Po uruchomieniu VS:
1. IdŸ do **Tools > Options**
2. ZnajdŸ **Copilot CLI > General**
3. Dostosuj ustawienia wed³ug potrzeb

## Krok 5: Testowanie

1. Otwórz dowolny plik C# (lub inny)
2. Zacznij pisaæ kod
3. Po ~500ms zobaczysz szar¹ sugestiê
4. Naciœnij `Tab` aby zaakceptowaæ
5. Lub naciœnij `Ctrl+Alt+.` aby rêcznie poprosiæ o sugestiê

## Przyk³ady u¿ycia

### Przyk³ad 1: Prosta funkcja
```csharp
// Wpisz:
public string GetUserName

// Copilot zasugeruje:
public string GetUserName(int userId)
{
    // implementation
}
```

### Przyk³ad 2: Async metoda
```csharp
// Wpisz:
public async Task<List<User>> Fetch

// Copilot zasugeruje ca³¹ implementacjê
```

### Przyk³ad 3: LINQ query
```csharp
// Wpisz:
var activeUsers = users.Where

// Copilot zasugeruje lambda expression
```

## Debugowanie Extension (dla developerów)

```powershell
# Otwórz solution
cd C:\PROJ\VS2026\Copilot
code CopilotExtension.sln

# W Visual Studio naciœnij F5
# Otworzy siê druga instancja VS w trybie Experimental
```

## Troubleshooting

### Problem: "Copilot CLI not found"
**Rozwi¹zanie:**
- SprawdŸ: `gh copilot --version`
- Jeœli nie dzia³a, ustaw œcie¿kê w Options > Copilot CLI > Copilot CLI Path

### Problem: "No suggestions appear"
**Rozwi¹zanie:**
- W³¹cz Debug Mode w ustawieniach
- SprawdŸ Output Window > Copilot CLI
- Upewnij siê ¿e masz aktywn¹ subskrypcjê GitHub Copilot

### Problem: "Timeout errors"
**Rozwi¹zanie:**
- Zwiêksz Timeout w ustawieniach (default: 30s)
- Zmniejsz Max Context Lines

## Najczêœciej u¿ywane skróty

| Akcja | Skrót |
|-------|-------|
| Poka¿ sugestiê | `Ctrl+Alt+.` |
| Zaakceptuj | `Tab` |
| Odrzuæ | `Esc` |
| Otwórz ustawienia | `Tools > Options > Copilot CLI` |

## Wsparcie techniczne

- GitHub: https://github.com/your-repo/copilot-vs-extension
- Issues: Zg³oœ problem na GitHub
- Dokumentacja: Zobacz README.md

## Nastêpne kroki

1. ? Zg³oœ feedback na GitHub
2. ?? Przeczytaj pe³n¹ dokumentacjê w README.md
3. ?? Dostosuj ustawienia do swoich potrzeb
4. ?? Zwiêksz swoj¹ produktywnoœæ!

---

**Uwaga:** Wymagana jest aktywna subskrypcja GitHub Copilot!
