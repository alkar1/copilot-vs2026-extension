# ?? GITHUB AUTHENTICATION FIX

## Problem
```
Error: You are not signed into GitHub.
```

Extension jest zainstalowany, ale **nie jesteœ zalogowany do GitHub** w Experimental Instance.

---

## ? ROZWI¥ZANIE (2 minuty)

### W Experimental Instance:

#### KROK 1: Otwórz Account Settings
```
File > Account Settings
```

#### KROK 2: Sign in
```
1. Click "Sign in"
2. Wybierz "GitHub"
3. Autoryzuj w przegl¹darce
4. Wróæ do VS
```

#### KROK 3: SprawdŸ GitHub Copilot
```
Extensions > GitHub Copilot
? Powinno byæ "Active" lub "Enabled"
```

#### KROK 4: Restart Experimental Instance
```
Zamknij i uruchom ponownie:
.\StartExpInstance.ps1
```

---

## ?? TEST

Po zalogowaniu:

```
1. Nowy Console App
2. W Program.cs napisz:
   public class Test
   {
       public int Add
3. Poczekaj ~1s
4. Powinna pojawiæ siê sugestia ?
```

---

## ?? WERYFIKACJA

### SprawdŸ status:

**W Experimental Instance > Output Window:**
```
View > Output
Show output from: Extensions

Szukaj: "Copilot auth status: OK"
```

**Jeœli widzisz:**
- ? `auth status: OK` ? Zalogowany poprawnie
- ? `Error: You are not signed into GitHub` ? Zaloguj siê ponownie

---

## ?? DLACZEGO TO SIÊ DZIEJE?

Experimental Instance jest **oddzieln¹ instancj¹ VS** z **w³asnym logowaniem**.

Nawet jeœli jesteœ zalogowany w g³ównym VS, **musisz siê zalogowaæ osobno** w Exp Instance.

---

## ?? ALTERNATYWA: Copilot CLI

Jeœli GitHub Copilot w VS nie dzia³a, mo¿esz u¿yæ **Copilot CLI**:

### Zainstaluj:
```powershell
npm install -g @githubnext/github-copilot-cli
```

### Autoryzuj:
```powershell
github-copilot-cli auth
```

### SprawdŸ:
```powershell
github-copilot-cli --version
```

Extension bêdzie u¿ywa³ CLI jeœli jest dostêpny.

---

## ?? STATUS CHECKLIST

```
[ ] Experimental Instance uruchomiona
[ ] Extension widoczny (Extensions > Manage Extensions)
[ ] Zalogowany do GitHub (File > Account Settings)
[ ] GitHub Copilot: Active
[ ] Sugestie pojawiaj¹ siê podczas pisania
```

---

**Po zalogowaniu extension bêdzie dzia³a³ w pe³ni!** ??
