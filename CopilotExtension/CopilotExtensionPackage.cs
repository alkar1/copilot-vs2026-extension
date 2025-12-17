using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CopilotExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CopilotExtensionPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options.CopilotOptionsPage), "Copilot CLI", "General", 0, 0, true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CopilotExtensionPackage : AsyncPackage
    {
        public const string PackageGuidString = "f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f";

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Log to Debug Output
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine("=== COPILOT EXTENSION: STARTING INITIALIZATION ===");
            System.Diagnostics.Debug.WriteLine($"=== Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            System.Diagnostics.Debug.WriteLine($"=== Package GUID: {PackageGuidString} ===");
            System.Diagnostics.Debug.WriteLine("========================================");

            try
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                System.Diagnostics.Debug.WriteLine("=== Switched to main thread successfully ===");

                System.Diagnostics.Debug.WriteLine("=== Initializing CopilotCommand... ===");
                await Commands.CopilotCommand.InitializeAsync(this);
                System.Diagnostics.Debug.WriteLine("=== CopilotCommand initialized successfully! ===");

                await base.InitializeAsync(cancellationToken, progress);
                System.Diagnostics.Debug.WriteLine("=== Base package initialization completed ===");

                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("=== COPILOT EXTENSION: INITIALIZATION COMPLETE! ===");
                System.Diagnostics.Debug.WriteLine("=== Extension is now ACTIVE and READY ===");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("=== COPILOT EXTENSION: INITIALIZATION FAILED! ===");
                System.Diagnostics.Debug.WriteLine($"=== ERROR: {ex.GetType().Name} ===");
                System.Diagnostics.Debug.WriteLine($"=== Message: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== Stack Trace: ===");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                System.Diagnostics.Debug.WriteLine("========================================");
                throw;
            }
        }

        #endregion
    }
}
