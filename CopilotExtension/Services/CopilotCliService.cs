using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CopilotExtension.Services
{
    public class CopilotCliService
    {
        private readonly string copilotCliPath;
        private readonly int timeoutSeconds = 30;

        public CopilotCliService()
        {
            copilotCliPath = FindCopilotCli();
        }

        private string FindCopilotCli()
        {
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "copilot.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub Copilot CLI", "copilot.exe"),
                "copilot"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path) || IsCommandAvailable(path))
                {
                    return path;
                }
            }

            return "gh copilot suggest";
        }

        private bool IsCommandAvailable(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(1000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetSuggestionAsync(string context, string currentLine, string fileName)
        {
            try
            {
                var prompt = BuildPrompt(context, currentLine, fileName);
                var result = await ExecuteCopilotCliAsync(prompt);
                return ParseSuggestion(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Copilot CLI error: {ex.Message}");
                return null;
            }
        }

        private string BuildPrompt(string context, string currentLine, string fileName)
        {
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var language = GetLanguageFromExtension(extension);

            var prompt = $@"Complete the following {language} code:

File: {fileName}

Context:
{TruncateContext(context, 1000)}

Current line to complete:
{currentLine}

Provide only the code completion, without explanations.";

            return prompt;
        }

        private string GetLanguageFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                "cs" => "C#",
                "vb" => "Visual Basic",
                "cpp" or "cc" or "cxx" or "h" or "hpp" => "C++",
                "js" => "JavaScript",
                "ts" => "TypeScript",
                "py" => "Python",
                "java" => "Java",
                "go" => "Go",
                "rs" => "Rust",
                "php" => "PHP",
                "rb" => "Ruby",
                "sql" => "SQL",
                "xml" => "XML",
                "json" => "JSON",
                "html" or "htm" => "HTML",
                "css" => "CSS",
                _ => "code"
            };
        }

        private string TruncateContext(string context, int maxLength)
        {
            if (string.IsNullOrEmpty(context) || context.Length <= maxLength)
                return context;

            var lines = context.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            var currentLength = 0;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (currentLength + lines[i].Length > maxLength)
                    break;

                result.Insert(0, lines[i] + Environment.NewLine);
                currentLength += lines[i].Length;
            }

            return result.ToString();
        }

        private async Task<string> ExecuteCopilotCliAsync(string prompt)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {copilotCliPath} \"{prompt.Replace("\"", "\"\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var completedTask = await Task.WhenAny(
                Task.WhenAll(outputTask, errorTask),
                Task.Delay(timeoutSeconds * 1000)
            );

            if (completedTask != Task.WhenAll(outputTask, errorTask))
            {
                process.Kill();
                throw new TimeoutException("Copilot CLI request timed out");
            }

            process.WaitForExit();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                throw new Exception($"Copilot CLI error: {error}");
            }

            return output;
        }

        private string ParseSuggestion(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return null;

            output = output.Trim();

            var codeBlockStart = output.IndexOf("```");
            if (codeBlockStart >= 0)
            {
                var codeStart = output.IndexOf('\n', codeBlockStart) + 1;
                var codeEnd = output.IndexOf("```", codeStart);
                if (codeEnd > codeStart)
                {
                    return output.Substring(codeStart, codeEnd - codeStart).Trim();
                }
            }

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!line.StartsWith("#") && 
                    !line.StartsWith("//") && 
                    !line.ToLower().Contains("here") &&
                    !line.ToLower().Contains("completion"))
                {
                    return line.Trim();
                }
            }

            return output;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var result = await ExecuteCopilotCliAsync("test");
                return !string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }
    }
}
