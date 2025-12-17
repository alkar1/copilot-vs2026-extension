using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using CopilotExtension.Services;
using Microsoft.VisualStudio.ComponentModelHost;

namespace CopilotExtension.Commands
{
    internal sealed class CopilotCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-e5f6-4a5b-8c7d-9e0f1a2b3c4d");

        private readonly AsyncPackage package;
        private IWpfTextView currentTextView;
        private CopilotCliService copilotService;

        private CopilotCommand(AsyncPackage package, IMenuCommandService commandService)
        {
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand: Constructor started ===");
            
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            System.Diagnostics.Debug.WriteLine($"=== CopilotCommand: CommandSet GUID = {CommandSet} ===");
            System.Diagnostics.Debug.WriteLine($"=== CopilotCommand: CommandId = 0x{CommandId:X4} ===");

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            System.Diagnostics.Debug.WriteLine("=== CopilotCommand: Menu command added to service ===");

            copilotService = new CopilotCliService();
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand: CopilotCliService created ===");
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand: Constructor completed successfully! ===");
        }

        public static CopilotCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: Starting... ===");
            
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: On main thread ===");

            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: Getting IMenuCommandService... ===");
            IMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            
            if (commandService == null)
            {
                System.Diagnostics.Debug.WriteLine("=== ERROR: IMenuCommandService is NULL! ===");
                throw new InvalidOperationException("IMenuCommandService could not be obtained");
            }
            
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: IMenuCommandService obtained ===");
            
            Instance = new CopilotCommand(package, commandService);
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: Instance created ===");
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.InitializeAsync: COMPLETED! ===");
        }

        private async void Execute(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: Command triggered! ===");
            
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: Getting active text view... ===");
                var textView = await GetActiveTextViewAsync();
                if (textView == null)
                {
                    System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: No active text view ===");
                    await ShowMessageAsync("Copilot", "No active code editor found. Please open a code file.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: Active text view obtained ===");

                var caretPosition = textView.Caret.Position.BufferPosition;
                var currentLine = caretPosition.GetContainingLine();
                var textBeforeCaret = currentLine.GetText().Substring(0, caretPosition.Position - currentLine.Start.Position);

                var snapshot = textView.TextBuffer.CurrentSnapshot;
                var allText = snapshot.GetText();
                var fileName = GetFileName(textView);

                System.Diagnostics.Debug.WriteLine($"=== CopilotCommand.Execute: File = {fileName} ===");
                System.Diagnostics.Debug.WriteLine($"=== CopilotCommand.Execute: Line = {textBeforeCaret} ===");
                System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: Calling Copilot CLI... ===");

                var suggestion = await copilotService.GetSuggestionAsync(allText, textBeforeCaret, fileName);

                if (!string.IsNullOrEmpty(suggestion))
                {
                    System.Diagnostics.Debug.WriteLine($"=== CopilotCommand.Execute: Got suggestion: {suggestion.Substring(0, Math.Min(50, suggestion.Length))}... ===");
                    
                    using (var edit = textView.TextBuffer.CreateEdit())
                    {
                        edit.Insert(caretPosition.Position, suggestion);
                        edit.Apply();
                    }
                    
                    System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: Suggestion inserted! ===");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== CopilotCommand.Execute: No suggestion received ===");
                    await ShowMessageAsync("Copilot", "No suggestion available. Make sure GitHub Copilot CLI is installed and configured.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== CopilotCommand.Execute: ERROR - {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== Stack: {ex.StackTrace} ===");
                await ShowMessageAsync("Copilot Error", $"Error getting suggestion: {ex.Message}");
            }
        }

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
    }
}
