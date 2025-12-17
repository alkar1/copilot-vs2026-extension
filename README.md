# Copilot CLI Extension for Visual Studio 2026

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-71%2F71%20passed-brightgreen)](https://github.com/alkar1/copilot-vs2026-extension)

**GitHub Repository:** https://github.com/alkar1/copilot-vs2026-extension

Dodatek do Visual Studio 2026, który integruje GitHub Copilot CLI z edytorem, zapewniaj¹c inteligentne sugestie kodu w czasie rzeczywistym.

## Funkcje

- ? **Inteligentne sugestie kodu** - Automatyczne sugestie podczas pisania kodu
- ?? **Inline suggestions** - Sugestie wyœwietlane bezpoœrednio w edytorze (jak GitHub Copilot)
- ?? **Skróty klawiszowe** - `Ctrl+Alt+.` aby uzyskaæ sugestiê, `Tab` aby zaakceptowaæ
- ?? **Konfigurowalne ustawienia** - Pe³na kontrola nad zachowaniem dodatku
- ?? **Wsparcie dla wielu jêzyków** - C#, C++, JavaScript, TypeScript, Python i wiele innych

## Wymagania

1. **Visual Studio 2026** (wersja 17.0 lub nowsza)
2. **GitHub Copilot CLI** - Musi byæ zainstalowany i skonfigurowany
   - Instalacja: `gh extension install github/gh-copilot`
   - Lub zainstaluj standalone Copilot CLI

## Instalacja

### Z pliku VSIX

1. Pobierz najnowsz¹ wersjê `CopilotExtension.vsix`
2. Zamknij wszystkie instancje Visual Studio
3. Uruchom plik `.vsix` podwójnym klikniêciem
4. Postêpuj zgodnie z instrukcjami instalatora
5. Uruchom ponownie Visual Studio

### Z kodu Ÿród³owego

1. Sklonuj repozytorium:
   ```bash
   git clone https://github.com/your-repo/copilot-vs-extension.git
   cd copilot-vs-extension
   ```

2. Otwórz `CopilotExtension.sln` w Visual Studio 2026

3. Naciœnij `F5` aby zbudowaæ i uruchomiæ w trybie debugowania

## Konfiguracja

### Ustawienia GitHub Copilot CLI

Przed u¿yciem dodatku, upewnij siê ¿e Copilot CLI jest zainstalowany:

```bash
# SprawdŸ instalacjê
gh copilot --version

# Lub jeœli u¿ywasz standalone CLI
copilot --version
```

### Ustawienia dodatku

PrzejdŸ do: **Tools > Options > Copilot CLI > General**

Dostêpne opcje:
- **Enable Copilot** - W³¹cz/wy³¹cz dodatek
- **Copilot CLI Path** - Œcie¿ka do CLI (automatyczna detekcja jeœli puste)
- **Auto-suggest Delay** - OpóŸnienie przed pokazaniem sugestii (domyœlnie 500ms)
- **Max Context Lines** - Maksymalna liczba linii kontekstu (domyœlnie 50)
- **Enable Inline Suggestions** - W³¹cz sugestie inline
- **Suggestion Opacity** - Przezroczystoœæ sugestii (0-100%)
- **Timeout** - Timeout dla ¿¹dañ (domyœlnie 30s)
- **Debug Mode** - Tryb debugowania

## U¿ycie

### Automatyczne sugestie

Po prostu zacznij pisaæ kod. Dodatek automatycznie:
1. Wykryje kontekst kodu
2. Wyœle zapytanie do Copilot CLI
3. Wyœwietli sugestiê jako szary, pochylony tekst
4. Naciœnij `Tab` aby zaakceptowaæ sugestiê
5. Naciœnij `Esc` aby odrzuciæ sugestiê

### Manualne sugestie

1. Ustaw kursor w miejscu gdzie potrzebujesz sugestii
2. Naciœnij `Ctrl+Alt+.` lub wybierz **Edit > Get Copilot Suggestion**
3. Sugestia zostanie automatycznie wstawiona

### Przyk³ad

```csharp
// Napisz pocz¹tek funkcji:
public async Task<string> FetchData

// Copilot zasugeruje:
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    var response = await client.GetAsync(url);
    return await response.Content.ReadAsStringAsync();
}
```

## Skróty klawiszowe

| Skrót | Akcja |
|-------|-------|
| `Ctrl+Alt+.` | Poka¿ sugestiê Copilot |
| `Tab` | Zaakceptuj sugestiê inline |
| `Esc` | Odrzuæ sugestiê inline |

## Obs³ugiwane jêzyki

- C# (*.cs)
- Visual Basic (*.vb)
- C++ (*.cpp, *.h, *.hpp)
- JavaScript (*.js)
- TypeScript (*.ts)
- Python (*.py)
- Java (*.java)
- Go (*.go)
- Rust (*.rs)
- PHP (*.php)
- Ruby (*.rb)
- SQL (*.sql)
- HTML (*.html)
- CSS (*.css)
- XML (*.xml)
- JSON (*.json)

## Troubleshooting

### Sugestie nie dzia³aj¹

1. SprawdŸ czy Copilot CLI jest zainstalowany:
   ```bash
   gh copilot --version
   ```

2. SprawdŸ czy jesteœ zalogowany:
   ```bash
   gh auth status
   ```

3. W³¹cz Debug Mode w ustawieniach i sprawdŸ logi w Output Window

### Wolne sugestie

- Zwiêksz **Auto-suggest Delay** w ustawieniach
- Zmniejsz **Max Context Lines**
- Zwiêksz **Timeout**

### B³êdy CLI

Jeœli automatyczna detekcja nie dzia³a, ustaw **Copilot CLI Path** manualnie:
- Windows: `C:\Users\YourName\.copilot\copilot.exe`
- Lub: `gh copilot suggest`

## Architektura

```
CopilotExtension/
??? Commands/
?   ??? CopilotCommand.cs          # Obs³uga poleceñ
??? Services/
?   ??? CopilotCliService.cs       # Integracja z CLI
??? Adornments/
?   ??? InlineSuggestionAdornment.cs # UI sugestii inline
??? Options/
?   ??? CopilotOptionsPage.cs      # Strona ustawieñ
??? Resources/
?   ??? Icon.png                    # Ikona dodatku
??? CopilotExtensionPackage.cs     # G³ówny pakiet
??? VSCommandTable.vsct             # Definicje poleceñ
??? source.extension.vsixmanifest   # Manifest dodatku
```

## Rozwój

### Budowanie

```bash
# Buduj w trybie Release
msbuild CopilotExtension.csproj /p:Configuration=Release

# Plik VSIX bêdzie w:
bin/Release/CopilotExtension.vsix
```

### Debugowanie

1. Otwórz `CopilotExtension.sln`
2. Naciœnij `F5`
3. Visual Studio uruchomi siê w trybie eksperymentalnym
4. Testuj dodatek w nowej instancji

### Testowanie

Dodatek zosta³ przetestowany z:
- Visual Studio 2026 Community/Pro/Enterprise
- .NET 8.0
- GitHub Copilot CLI v1.0+

## Licencja

MIT License - Zobacz [LICENSE](LICENSE) dla szczegó³ów

## Wsparcie

- ?? **Issues**: [GitHub Issues](https://github.com/your-repo/copilot-vs-extension/issues)
- ?? **Dyskusje**: [GitHub Discussions](https://github.com/your-repo/copilot-vs-extension/discussions)
- ?? **Email**: support@example.com

## Roadmap

- [ ] Wsparcie dla multi-line suggestions
- [ ] Cache sugestii
- [ ] Statystyki u¿ycia
- [ ] Integracja z GitHub Copilot API
- [ ] Code review suggestions
- [ ] Unit test generation
- [ ] Documentation generation

## Podziêkowania

- GitHub Copilot CLI team
- Visual Studio Extensibility team
- Spo³ecznoœæ open source

## Changelog

### v1.0.0 (2024)
- ? Pierwsza wersja
- ? Inline suggestions
- ? Keyboard shortcuts
- ? Multi-language support
- ? Configurable options

---

**Uwaga**: Ten dodatek wymaga aktywnej subskrypcji GitHub Copilot i zainstalowanego Copilot CLI.
