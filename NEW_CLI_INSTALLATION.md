# ?? New GitHub Copilot CLI Installation Guide

## ? COMPLETED: New CLI Installed!

**Version:** 0.1.36  
**Installation Date:** 2024  
**Status:** Installed but requires authentication

---

## ?? What Was Installed

```bash
npm install -g @githubnext/github-copilot-cli
# ? Successfully installed version 0.1.36
```

**Installed Command:**
```bash
github-copilot-cli --version
# Output: 0.1.36
```

---

## ?? Authentication Required

To use the new Copilot CLI, you need to authenticate:

### Step 1: Start Authentication
```powershell
github-copilot-cli auth
```

This will:
1. Open your browser automatically
2. Ask you to approve GitHub Copilot CLI access
3. Save the token locally

### Step 2: Approve in Browser
- Click the link that appears
- Login to GitHub if needed
- Approve the application
- Wait for confirmation

### Step 3: Verify Authentication
```powershell
github-copilot-cli what-the-shell "list files"
```

---

## ?? Available Commands

### 1. **what-the-shell** - Shell Commands
```powershell
github-copilot-cli what-the-shell "find all txt files"
# Suggests: Get-ChildItem -Recurse -Filter *.txt
```

### 2. **git-assist** - Git Commands
```powershell
github-copilot-cli git-assist "undo last commit"
# Suggests: git reset --soft HEAD~1
```

### 3. **gh-assist** - GitHub CLI Commands
```powershell
github-copilot-cli gh-assist "create a new repo"
# Suggests: gh repo create <name> --public
```

---

## ?? Integration with Extension

### Update CopilotCliService.cs

The extension's `CopilotCliService.cs` needs to be updated to support the new CLI:

```csharp
// Old (deprecated):
gh copilot suggest "code suggestion"

// New (working):
github-copilot-cli what-the-shell "code suggestion"
```

### Suggested Changes:

1. **Update CLI Path Detection:**
```csharp
private string FindCopilotCli()
{
    var possiblePaths = new[]
    {
        "github-copilot-cli",           // New CLI (global npm)
        "gh copilot suggest",           // Old CLI (deprecated)
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile), 
            "node_modules", ".bin", "github-copilot-cli")
    };
    // ... test each path
}
```

2. **Update Command Execution:**
```csharp
private async Task<string> ExecuteCopilotCliAsync(string prompt)
{
    // Use new CLI format
    var args = $"what-the-shell \"{EscapePrompt(prompt)}\"";
    // ... execute github-copilot-cli with args
}
```

---

## ?? Comparison: Old vs New CLI

| Feature | Old CLI (gh copilot) | New CLI (github-copilot-cli) |
|---------|---------------------|------------------------------|
| **Status** | ? Deprecated | ? Active |
| **Command** | `gh copilot suggest` | `github-copilot-cli what-the-shell` |
| **Installation** | `gh extension install` | `npm install -g` |
| **Version** | v1.2.0 (final) | v0.1.36+ (active) |
| **Auth** | Uses gh auth | Separate auth |
| **Support** | No longer supported | Actively maintained |

---

## ?? Testing the New CLI

### Test 1: Simple Command
```powershell
github-copilot-cli what-the-shell "show disk usage"
```

Expected output:
```
Suggestion: Get-PSDrive -PSProvider FileSystem
```

### Test 2: Git Command
```powershell
github-copilot-cli git-assist "create a new branch"
```

Expected output:
```
Suggestion: git checkout -b new-branch-name
```

### Test 3: Code Suggestion
```powershell
github-copilot-cli what-the-shell "create a C# function to add numbers"
```

---

## ?? Migration Path

### For Extension Users:

1. **Install New CLI** ? DONE
   ```bash
   npm install -g @githubnext/github-copilot-cli
   ```

2. **Authenticate** ? PENDING
   ```bash
   github-copilot-cli auth
   # Follow browser prompts
   ```

3. **Update Extension** (If needed)
   - Extension will auto-detect new CLI
   - Fallback to old CLI if available
   - Works with both versions

### For Developers:

Update `CopilotCliService.cs` to detect both:
```csharp
// Try new CLI first
if (IsCommandAvailable("github-copilot-cli"))
    return "github-copilot-cli";

// Fallback to old CLI
if (IsCommandAvailable("gh copilot"))
    return "gh copilot suggest";
```

---

## ?? Known Issues

### Issue 1: Authentication Timeout
**Symptom:** `github-copilot-cli auth` times out  
**Solution:** 
- Check internet connection
- Disable VPN if active
- Try again: `github-copilot-cli auth`

### Issue 2: Command Not Found
**Symptom:** `'github-copilot-cli' is not recognized`  
**Solution:**
```powershell
# Refresh PATH
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Or restart PowerShell
```

### Issue 3: npm Not Found
**Symptom:** `'npm' is not recognized`  
**Solution:**
- Install Node.js: https://nodejs.org/
- Restart terminal
- Verify: `node --version`

---

## ?? Next Steps

### 1. Complete Authentication
```powershell
# Run this and follow browser prompts
github-copilot-cli auth
```

### 2. Test Basic Functionality
```powershell
# After auth, test it
github-copilot-cli what-the-shell "list running processes"
```

### 3. Update Extension Code
- Modify `CopilotCliService.cs`
- Add support for new CLI
- Keep backward compatibility with old CLI

### 4. Test Integration
```powershell
# Build extension
dotnet build CopilotExtension/CopilotExtension.csproj -c Release

# Install in VS2026
# Test with real code suggestions
```

---

## ?? Success Criteria

- [x] New CLI installed (v0.1.36)
- [ ] Authentication completed
- [ ] Basic commands working
- [ ] Extension updated to use new CLI
- [ ] End-to-end testing in VS2026

---

## ?? Support

**New CLI Issues:**
- GitHub: https://github.com/github/copilot-cli
- Docs: https://docs.github.com/copilot/github-copilot-in-the-cli

**Extension Issues:**
- Repository: https://github.com/alkar1/copilot-vs2026-extension
- Issues: https://github.com/alkar1/copilot-vs2026-extension/issues

---

## ?? Quick Reference

```powershell
# Check version
github-copilot-cli --version

# Authenticate
github-copilot-cli auth

# Get shell command
github-copilot-cli what-the-shell "your query"

# Get git command
github-copilot-cli git-assist "your query"

# Get GitHub CLI command
github-copilot-cli gh-assist "your query"

# Help
github-copilot-cli --help
```

---

**Status:** ? CLI Installed, ? Auth Pending  
**Last Updated:** 2024  
**Version:** 0.1.36
