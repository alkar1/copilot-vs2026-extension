using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotExtension.Services
{
    /// <summary>
    /// Monitors GitHub Copilot health and automatically restarts it if it fails
    /// </summary>
    public class CopilotHealthMonitor : IDisposable
    {
        private readonly CopilotCliService copilotService;
        private Timer healthCheckTimer;
        private bool isMonitoring;
        private int consecutiveFailures;
        private DateTime lastSuccessfulCheck;
        private const int MaxConsecutiveFailures = 3;
        private const int HealthCheckIntervalMs = 60000; // 1 minute
        private const int RestartCooldownMs = 300000; // 5 minutes

        public event EventHandler<CopilotHealthEventArgs> HealthStatusChanged;
        public event EventHandler<string> RestartAttempted;

        public CopilotHealthStatus CurrentStatus { get; private set; }
        public DateTime? LastRestartAttempt { get; private set; }

        public CopilotHealthMonitor(CopilotCliService copilotService)
        {
            this.copilotService = copilotService ?? throw new ArgumentNullException(nameof(copilotService));
            CurrentStatus = CopilotHealthStatus.Unknown;
            lastSuccessfulCheck = DateTime.Now;
        }

        public void StartMonitoring()
        {
            if (isMonitoring)
                return;

            isMonitoring = true;
            consecutiveFailures = 0;

            healthCheckTimer = new Timer(
                HealthCheckCallback,
                null,
                TimeSpan.Zero, // Start immediately
                TimeSpan.FromMilliseconds(HealthCheckIntervalMs)
            );

            Debug.WriteLine("[CopilotHealthMonitor] Monitoring started");
        }

        public void StopMonitoring()
        {
            isMonitoring = false;
            healthCheckTimer?.Dispose();
            healthCheckTimer = null;

            Debug.WriteLine("[CopilotHealthMonitor] Monitoring stopped");
        }

        private async void HealthCheckCallback(object state)
        {
            if (!isMonitoring)
                return;

            try
            {
                var isHealthy = await CheckCopilotHealthAsync();

                if (isHealthy)
                {
                    HandleHealthyStatus();
                }
                else
                {
                    HandleUnhealthyStatus();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CopilotHealthMonitor] Health check error: {ex.Message}");
                HandleUnhealthyStatus();
            }
        }

        private async Task<bool> CheckCopilotHealthAsync()
        {
            try
            {
                // Try simple connection test
                var isConnected = await copilotService.TestConnectionAsync();

                if (!isConnected)
                {
                    Debug.WriteLine("[CopilotHealthMonitor] Copilot connection test failed");
                    return false;
                }

                // Try actual suggestion request (lightweight)
                var testPrompt = "test";
                var result = await Task.Run(async () =>
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        try
                        {
                            return await copilotService.GetSuggestionAsync("", testPrompt, "test.cs");
                        }
                        catch (OperationCanceledException)
                        {
                            return null;
                        }
                    }
                });

                var isHealthy = result != null || isConnected;
                
                Debug.WriteLine($"[CopilotHealthMonitor] Health check result: {(isHealthy ? "HEALTHY" : "UNHEALTHY")}");
                
                return isHealthy;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CopilotHealthMonitor] Health check exception: {ex.Message}");
                return false;
            }
        }

        private void HandleHealthyStatus()
        {
            if (consecutiveFailures > 0)
            {
                Debug.WriteLine($"[CopilotHealthMonitor] Recovered after {consecutiveFailures} failures");
            }

            consecutiveFailures = 0;
            lastSuccessfulCheck = DateTime.Now;
            UpdateStatus(CopilotHealthStatus.Healthy);
        }

        private void HandleUnhealthyStatus()
        {
            consecutiveFailures++;
            
            Debug.WriteLine($"[CopilotHealthMonitor] Unhealthy status detected. Consecutive failures: {consecutiveFailures}/{MaxConsecutiveFailures}");

            if (consecutiveFailures >= MaxConsecutiveFailures)
            {
                UpdateStatus(CopilotHealthStatus.Failed);
                
                // Check if we should attempt restart
                if (ShouldAttemptRestart())
                {
                    _ = Task.Run(async () => await AttemptRestartAsync());
                }
            }
            else
            {
                UpdateStatus(CopilotHealthStatus.Degraded);
            }
        }

        private bool ShouldAttemptRestart()
        {
            // Don't restart too frequently
            if (LastRestartAttempt.HasValue)
            {
                var timeSinceLastRestart = DateTime.Now - LastRestartAttempt.Value;
                if (timeSinceLastRestart.TotalMilliseconds < RestartCooldownMs)
                {
                    Debug.WriteLine($"[CopilotHealthMonitor] Skipping restart - cooldown period ({timeSinceLastRestart.TotalMinutes:F1} min remaining)");
                    return false;
                }
            }

            return true;
        }

        private async Task AttemptRestartAsync()
        {
            LastRestartAttempt = DateTime.Now;
            UpdateStatus(CopilotHealthStatus.Restarting);

            Debug.WriteLine("[CopilotHealthMonitor] Attempting to restart Copilot...");
            RestartAttempted?.Invoke(this, "Attempting automatic restart");

            try
            {
                // Strategy 1: Try to re-authenticate CLI
                bool success = await RestartCopilotCliAsync();

                if (success)
                {
                    Debug.WriteLine("[CopilotHealthMonitor] Restart successful!");
                    consecutiveFailures = 0;
                    UpdateStatus(CopilotHealthStatus.Healthy);
                    RestartAttempted?.Invoke(this, "Restart successful");
                }
                else
                {
                    Debug.WriteLine("[CopilotHealthMonitor] Restart failed");
                    UpdateStatus(CopilotHealthStatus.Failed);
                    RestartAttempted?.Invoke(this, "Restart failed - manual intervention required");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CopilotHealthMonitor] Restart error: {ex.Message}");
                UpdateStatus(CopilotHealthStatus.Failed);
                RestartAttempted?.Invoke(this, $"Restart error: {ex.Message}");
            }
        }

        private async Task<bool> RestartCopilotCliAsync()
        {
            try
            {
                // Strategy 1: Kill any existing copilot CLI processes
                await KillCopilotProcessesAsync();

                // Wait a bit
                await Task.Delay(2000);

                // Strategy 2: Test connection again
                var isHealthy = await CheckCopilotHealthAsync();

                return isHealthy;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CopilotHealthMonitor] Restart CLI error: {ex.Message}");
                return false;
            }
        }

        private async Task KillCopilotProcessesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var processNames = new[] { "github-copilot-cli", "copilot", "gh" };

                    foreach (var processName in processNames)
                    {
                        var processes = Process.GetProcessesByName(processName);
                        foreach (var process in processes)
                        {
                            try
                            {
                                Debug.WriteLine($"[CopilotHealthMonitor] Killing process: {process.ProcessName} (PID: {process.Id})");
                                process.Kill();
                                process.WaitForExit(5000);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[CopilotHealthMonitor] Failed to kill process {process.Id}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CopilotHealthMonitor] Error killing processes: {ex.Message}");
                }
            });
        }

        private void UpdateStatus(CopilotHealthStatus newStatus)
        {
            if (CurrentStatus != newStatus)
            {
                var oldStatus = CurrentStatus;
                CurrentStatus = newStatus;

                Debug.WriteLine($"[CopilotHealthMonitor] Status changed: {oldStatus} -> {newStatus}");

                HealthStatusChanged?.Invoke(this, new CopilotHealthEventArgs
                {
                    Status = newStatus,
                    ConsecutiveFailures = consecutiveFailures,
                    LastSuccessfulCheck = lastSuccessfulCheck,
                    Message = GetStatusMessage(newStatus)
                });
            }
        }

        private string GetStatusMessage(CopilotHealthStatus status)
        {
            return status switch
            {
                CopilotHealthStatus.Healthy => "GitHub Copilot is running normally",
                CopilotHealthStatus.Degraded => $"GitHub Copilot experiencing issues ({consecutiveFailures} failures)",
                CopilotHealthStatus.Failed => $"GitHub Copilot has failed ({consecutiveFailures} consecutive failures)",
                CopilotHealthStatus.Restarting => "Attempting to restart GitHub Copilot...",
                _ => "GitHub Copilot status unknown"
            };
        }

        public void Dispose()
        {
            StopMonitoring();
            healthCheckTimer?.Dispose();
        }
    }

    public enum CopilotHealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Failed,
        Restarting
    }

    public class CopilotHealthEventArgs : EventArgs
    {
        public CopilotHealthStatus Status { get; set; }
        public int ConsecutiveFailures { get; set; }
        public DateTime LastSuccessfulCheck { get; set; }
        public string Message { get; set; }
    }
}
