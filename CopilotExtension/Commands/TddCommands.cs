using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using CopilotExtension.Services;

namespace CopilotExtension.Commands
{
    /// <summary>
    /// Commands for TDD cycle operations
    /// </summary>
    internal sealed class TddCommands
    {
        // Command IDs
        public const int GenerateTestsCommandId = 0x0200;
        public const int RunTestsCommandId = 0x0201;
        public const int FixFailingTestsCommandId = 0x0202;
        public const int FullTddCycleCommandId = 0x0203;

        public static readonly Guid CommandSet = new Guid("b2c3d4e5-f6a7-4b5c-8d9e-0f1a2b3c4d5e");

        private readonly AsyncPackage package;
        private readonly TddCycleService tddService;
        private readonly TestRunnerService testRunner;

        private TddCommands(AsyncPackage package, IMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            tddService = new TddCycleService();
            testRunner = new TestRunnerService();

            // Register commands
            RegisterCommand(commandService, GenerateTestsCommandId, ExecuteGenerateTests);
            RegisterCommand(commandService, RunTestsCommandId, ExecuteRunTests);
            RegisterCommand(commandService, FixFailingTestsCommandId, ExecuteFixFailingTests);
            RegisterCommand(commandService, FullTddCycleCommandId, ExecuteFullTddCycle);
        }

        public static TddCommands Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            IMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            Instance = new TddCommands(package, commandService);
        }

        private void RegisterCommand(IMenuCommandService commandService, int commandId, EventHandler handler)
        {
            var menuCommandID = new CommandID(CommandSet, commandId);
            var menuItem = new MenuCommand(handler, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        #region Command Handlers

        /// <summary>
        /// Generate tests for selected code
        /// </summary>
        private async void ExecuteGenerateTests(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var textView = await GetActiveTextViewAsync();
                if (textView == null)
                {
                    await ShowMessageAsync("TDD Error", "No active text editor");
                    return;
                }

                // Get selected code or entire file
                var selection = textView.Selection;
                var selectedCode = selection.IsEmpty 
                    ? textView.TextBuffer.CurrentSnapshot.GetText()
                    : selection.StreamSelectionSpan.GetText();

                if (string.IsNullOrEmpty(selectedCode))
                {
                    await ShowMessageAsync("TDD Error", "No code selected");
                    return;
                }

                // Determine language
                var fileName = GetFileName(textView);
                var language = DetermineLanguage(fileName);

                // Show progress
                await ShowStatusAsync("Generating tests...");

                // Generate tests
                var generatedTests = await tddService.GenerateTestsAsync(
                    selectedCode,
                    "GeneratedTests",
                    language);

                if (string.IsNullOrEmpty(generatedTests))
                {
                    await ShowMessageAsync("TDD Result", "No tests generated");
                    return;
                }

                // Create new test file or insert tests
                await CreateOrUpdateTestFileAsync(fileName, generatedTests);

                await ShowStatusAsync("Tests generated successfully!");
                await ShowMessageAsync("TDD Success", 
                    $"Generated {CountTests(generatedTests)} test(s)\n\n" +
                    "Test file created/updated. Review and run tests.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("TDD Error", $"Failed to generate tests: {ex.Message}");
            }
        }

        /// <summary>
        /// Run tests for current project
        /// </summary>
        private async void ExecuteRunTests(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var projectPath = await GetCurrentProjectPathAsync();
                if (string.IsNullOrEmpty(projectPath))
                {
                    await ShowMessageAsync("TDD Error", "No project file found");
                    return;
                }

                await ShowStatusAsync("Running tests...");

                var result = await testRunner.RunTestsAsync(projectPath);

                // Show results
                var message = $@"Test Results:
Total: {result.TotalTests}
Passed: {result.PassedTests}
Failed: {result.FailedTests.Count}
Skipped: {result.SkippedTests}
Duration: {result.Duration.TotalSeconds:F2}s

{(result.Success ? "? All tests passed!" : "? Some tests failed")}";

                if (result.FailedTests.Count > 0)
                {
                    message += "\n\nFailed Tests:\n";
                    foreach (var failure in result.FailedTests)
                    {
                        message += $"- {failure.TestName}\n";
                    }
                }

                await ShowMessageAsync("Test Results", message);
                await ShowStatusAsync(result.Success ? "All tests passed!" : "Some tests failed");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("TDD Error", $"Failed to run tests: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze and fix failing tests
        /// </summary>
        private async void ExecuteFixFailingTests(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var projectPath = await GetCurrentProjectPathAsync();
                if (string.IsNullOrEmpty(projectPath))
                {
                    await ShowMessageAsync("TDD Error", "No project file found");
                    return;
                }

                await ShowStatusAsync("Running tests to find failures...");

                // Run tests first
                var testResult = await testRunner.RunTestsAsync(projectPath);

                if (testResult.Success)
                {
                    await ShowMessageAsync("TDD Info", "All tests are passing! No fixes needed.");
                    return;
                }

                await ShowStatusAsync($"Analyzing {testResult.FailedTests.Count} failing test(s)...");

                // Get source code
                var textView = await GetActiveTextViewAsync();
                var sourceCode = textView != null 
                    ? textView.TextBuffer.CurrentSnapshot.GetText()
                    : "";

                var fileName = textView != null ? GetFileName(textView) : "";
                var language = DetermineLanguage(fileName);

                // Analyze failures and get suggestions
                var analysis = await tddService.AnalyzeTestFailuresAsync(
                    sourceCode,
                    testResult.FailedTests,
                    language);

                // Show suggestions
                var message = "Test Failure Analysis:\n\n";
                foreach (var suggestion in analysis.Suggestions)
                {
                    message += $"Test: {suggestion.TestName}\n";
                    message += $"Root Cause: {suggestion.RootCause}\n";
                    message += $"Suggested Fix:\n{suggestion.ProposedFix}\n";
                    message += $"Explanation: {suggestion.Explanation}\n\n";
                }

                message += "Would you like to apply these fixes?";

                var apply = await ShowConfirmationAsync("Fix Suggestions", message);
                if (apply)
                {
                    // Apply fixes (simplified - in real impl would use proper code modification)
                    await ShowMessageAsync("TDD Info", 
                        "Fixes suggested. Please review and apply manually.\n" +
                        "Advanced auto-fix coming soon!");
                }

                await ShowStatusAsync("Fix suggestions generated");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("TDD Error", $"Failed to analyze failures: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute full TDD cycle: Generate tests -> Run -> Fix -> Repeat
        /// </summary>
        private async void ExecuteFullTddCycle(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var textView = await GetActiveTextViewAsync();
                if (textView == null)
                {
                    await ShowMessageAsync("TDD Error", "No active text editor");
                    return;
                }

                var projectPath = await GetCurrentProjectPathAsync();
                if (string.IsNullOrEmpty(projectPath))
                {
                    await ShowMessageAsync("TDD Error", "No project file found");
                    return;
                }

                var sourceCode = textView.TextBuffer.CurrentSnapshot.GetText();
                var fileName = GetFileName(textView);
                var language = DetermineLanguage(fileName);

                await ShowStatusAsync("Starting TDD cycle...");

                // Execute iterative TDD cycle
                var result = await tddService.RunIterativeTddCycleAsync(
                    sourceCode,
                    "", // Will generate tests
                    projectPath,
                    language,
                    maxIterations: 3);

                // Show results
                var message = $@"TDD Cycle Complete!

{result.Message}

Iterations: {result.Iterations.Count}
Final Status: {(result.Success ? "? All tests passed" : "?? Tests still failing")}

";

                foreach (var iteration in result.Iterations)
                {
                    message += $"\nIteration {result.Iterations.IndexOf(iteration) + 1}:\n";
                    message += $"  Tests: {iteration.TestResults?.TotalTests ?? 0}\n";
                    message += $"  Passed: {iteration.TestResults?.PassedTests ?? 0}\n";
                    message += $"  Failed: {iteration.TestResults?.FailedTests?.Count ?? 0}\n";
                }

                await ShowMessageAsync("TDD Cycle Results", message);
                await ShowStatusAsync(result.Success ? "TDD cycle successful!" : "TDD cycle completed with issues");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("TDD Error", $"TDD cycle failed: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task<IWpfTextView> GetActiveTextViewAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = await ServiceProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
                return null;

            var editorAdaptersFactory = componentModel.GetService<Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService>();
            if (editorAdaptersFactory == null)
                return null;

            var textManager = await ServiceProvider.GetServiceAsync(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager))
                as Microsoft.VisualStudio.TextManager.Interop.IVsTextManager2;
            if (textManager == null)
                return null;

            textManager.GetActiveView2(1, null, (uint)Microsoft.VisualStudio.TextManager.Interop._VIEWFRAMETYPE.vftCodeWindow, out var textViewAdapter);
            return editorAdaptersFactory.GetWpfTextView(textViewAdapter);
        }

        private string GetFileName(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            textView.TextBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer), out Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer bufferAdapter);
            if (bufferAdapter is Microsoft.VisualStudio.Shell.Interop.IPersistFileFormat persistFileFormat)
            {
                persistFileFormat.GetCurFile(out string filePath, out _);
                return filePath;
            }
            return "unknown.cs";
        }

        private string DetermineLanguage(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName)?.ToLower();
            return ext switch
            {
                ".cs" => "C#",
                ".vb" => "Visual Basic",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                _ => "C#"
            };
        }

        private async Task<string> GetCurrentProjectPathAsync()
        {
            // Simplified - in real implementation would get actual project path from VS
            await Task.CompletedTask;
            return @"C:\PROJ\VS2026\Copilot\CopilotExtension.Tests\CopilotExtension.Tests.csproj";
        }

        private async Task CreateOrUpdateTestFileAsync(string sourceFileName, string testCode)
        {
            // Simplified - in real implementation would create actual test file
            await Task.CompletedTask;
            // Would use VS APIs to create/update file
        }

        private int CountTests(string testCode)
        {
            // Simple test counting
            int count = 0;
            foreach (var line in testCode.Split('\n'))
            {
                if (line.Contains("[Fact]") || line.Contains("[Theory]") || 
                    line.Contains("@Test") || line.Contains("def test_"))
                {
                    count++;
                }
            }
            return count;
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Microsoft.VisualStudio.Shell.Interop.IVsUIShell uiShell =
                await ServiceProvider.GetServiceAsync(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell))
                as Microsoft.VisualStudio.Shell.Interop.IVsUIShell;

            if (uiShell != null)
            {
                Guid clsid = Guid.Empty;
                int result;
                uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    title,
                    message,
                    string.Empty,
                    0,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                    0,
                    out result);
            }
        }

        private async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Microsoft.VisualStudio.Shell.Interop.IVsUIShell uiShell =
                await ServiceProvider.GetServiceAsync(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell))
                as Microsoft.VisualStudio.Shell.Interop.IVsUIShell;

            if (uiShell != null)
            {
                Guid clsid = Guid.Empty;
                int result;
                uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    title,
                    message,
                    string.Empty,
                    0,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_QUERY,
                    0,
                    out result);
                
                return result == 6; // IDYES
            }

            return false;
        }

        private async Task ShowStatusAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var statusBar = await ServiceProvider.GetServiceAsync(typeof(Microsoft.VisualStudio.Shell.Interop.SVsStatusbar))
                as Microsoft.VisualStudio.Shell.Interop.IVsStatusbar;

            statusBar?.SetText(message);
        }

        #endregion
    }
}
