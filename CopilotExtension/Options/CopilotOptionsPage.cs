using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CopilotExtension.Options
{
    [Guid("2e3f4a5b-6c7d-8e9f-0a1b-2c3d4e5f6a7b")]
    public class CopilotOptionsPage : DialogPage
    {
        [Category("General")]
        [DisplayName("Enable Copilot")]
        [Description("Enable or disable Copilot suggestions")]
        public bool EnableCopilot { get; set; } = true;

        [Category("General")]
        [DisplayName("Copilot CLI Path")]
        [Description("Path to GitHub Copilot CLI executable. Leave empty for auto-detection.")]
        public string CopilotCliPath { get; set; } = "";

        [Category("Behavior")]
        [DisplayName("Auto-suggest Delay (ms)")]
        [Description("Delay in milliseconds before showing suggestions after typing")]
        public int AutoSuggestDelay { get; set; } = 500;

        [Category("Behavior")]
        [DisplayName("Max Context Lines")]
        [Description("Maximum number of lines to send as context to Copilot")]
        public int MaxContextLines { get; set; } = 50;

        [Category("Behavior")]
        [DisplayName("Enable Inline Suggestions")]
        [Description("Show suggestions inline as you type (like GitHub Copilot)")]
        public bool EnableInlineSuggestions { get; set; } = true;

        [Category("Appearance")]
        [DisplayName("Suggestion Opacity")]
        [Description("Opacity of inline suggestions (0-100)")]
        public int SuggestionOpacity { get; set; } = 50;

        [Category("Advanced")]
        [DisplayName("Timeout (seconds)")]
        [Description("Timeout for Copilot CLI requests in seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        [Category("Advanced")]
        [DisplayName("Debug Mode")]
        [Description("Enable debug logging for troubleshooting")]
        public bool DebugMode { get; set; } = false;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                if (AutoSuggestDelay < 0)
                    AutoSuggestDelay = 0;
                if (AutoSuggestDelay > 5000)
                    AutoSuggestDelay = 5000;

                if (MaxContextLines < 10)
                    MaxContextLines = 10;
                if (MaxContextLines > 200)
                    MaxContextLines = 200;

                if (SuggestionOpacity < 0)
                    SuggestionOpacity = 0;
                if (SuggestionOpacity > 100)
                    SuggestionOpacity = 100;

                if (TimeoutSeconds < 5)
                    TimeoutSeconds = 5;
                if (TimeoutSeconds > 120)
                    TimeoutSeconds = 120;
            }
        }
    }
}
