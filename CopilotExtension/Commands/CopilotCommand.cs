using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Task = System.Threading.Tasks.Task;
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

        private CopilotCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            copilotService = new CopilotCliService();
        }

        public static CopilotCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CopilotCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var textView = await GetActiveTextViewAsync();
                if (textView == null)
                    return;

                var caretPosition = textView.Caret.Position.BufferPosition;
                var currentLine = caretPosition.GetContainingLine();
                var textBeforeCaret = currentLine.GetText().Substring(0, caretPosition.Position - currentLine.Start.Position);

                var snapshot = textView.TextBuffer.CurrentSnapshot;
                var allText = snapshot.GetText();
                var fileName = GetFileName(textView);

                var suggestion = await copilotService.GetSuggestionAsync(allText, textBeforeCaret, fileName);

                if (!string.IsNullOrEmpty(suggestion))
                {
                    using (var edit = textView.TextBuffer.CreateEdit())
                    {
                        edit.Insert(caretPosition.Position, suggestion);
                        edit.Apply();
                    }
                }
            }
            catch (Exception ex)
            {
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
