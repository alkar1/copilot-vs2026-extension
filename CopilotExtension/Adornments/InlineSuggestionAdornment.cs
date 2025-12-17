using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using CopilotExtension.Services;
using System.Threading.Tasks;

namespace CopilotExtension.Adornments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class InlineSuggestionAdornmentFactory : IWpfTextViewCreationListener
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            var adornment = new InlineSuggestionAdornment(textView, TextDocumentFactoryService);
        }
    }

    internal sealed class InlineSuggestionAdornment
    {
        private readonly IWpfTextView textView;
        private readonly IAdornmentLayer adornmentLayer;
        private readonly CopilotCliService copilotService;
        private readonly TextBlock suggestionTextBlock;
        private string currentSuggestion;
        private ITextDocument textDocument;
        private readonly ITextDocumentFactoryService textDocumentFactoryService;

        public InlineSuggestionAdornment(IWpfTextView textView, ITextDocumentFactoryService textDocumentFactoryService)
        {
            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.adornmentLayer = textView.GetAdornmentLayer("InlineSuggestionAdornment");
            this.copilotService = new CopilotCliService();

            suggestionTextBlock = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)),
                FontStyle = FontStyles.Italic,
                Visibility = Visibility.Collapsed
            };

            textView.TextBuffer.Changed += OnTextBufferChanged;
            textView.Caret.PositionChanged += OnCaretPositionChanged;
            textView.LayoutChanged += OnLayoutChanged;
            textView.LostAggregateFocus += OnLostFocus;
            textView.Closed += OnViewClosed;

            textView.VisualElement.PreviewKeyDown += OnPreviewKeyDown;

            if (textDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out textDocument))
            {
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            HideSuggestion();

            if (e.Changes.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await ShowSuggestionAsync();
                });
            }
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateSuggestionPosition();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            UpdateSuggestionPosition();
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            HideSuggestion();
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            textView.TextBuffer.Changed -= OnTextBufferChanged;
            textView.Caret.PositionChanged -= OnCaretPositionChanged;
            textView.LayoutChanged -= OnLayoutChanged;
            textView.LostAggregateFocus -= OnLostFocus;
            textView.Closed -= OnViewClosed;
            textView.VisualElement.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && !string.IsNullOrEmpty(currentSuggestion))
            {
                e.Handled = true;
                AcceptSuggestion();
            }
            else if (e.Key == Key.Escape)
            {
                HideSuggestion();
            }
        }

        private async Task ShowSuggestionAsync()
        {
            try
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var caretPosition = textView.Caret.Position.BufferPosition;
                var currentLine = caretPosition.GetContainingLine();
                var textBeforeCaret = currentLine.GetText().Substring(0, caretPosition.Position - currentLine.Start.Position);

                if (string.IsNullOrWhiteSpace(textBeforeCaret))
                    return;

                var snapshot = textView.TextBuffer.CurrentSnapshot;
                var allText = snapshot.GetText();
                var fileName = GetFileName();

                var suggestion = await copilotService.GetSuggestionAsync(allText, textBeforeCaret, fileName);

                if (!string.IsNullOrEmpty(suggestion))
                {
                    currentSuggestion = suggestion;
                    DisplaySuggestion(suggestion, caretPosition);
                }
            }
            catch (Exception)
            {
            }
        }

        private void DisplaySuggestion(string suggestion, SnapshotPoint position)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            suggestionTextBlock.Text = suggestion;
            suggestionTextBlock.Visibility = Visibility.Visible;

            var lineView = textView.GetTextViewLineContainingBufferPosition(position);
            if (lineView == null)
                return;

            var characterBounds = lineView.GetCharacterBounds(position);
            Canvas.SetLeft(suggestionTextBlock, characterBounds.Left);
            Canvas.SetTop(suggestionTextBlock, characterBounds.Top);

            adornmentLayer.RemoveAllAdornments();
            adornmentLayer.AddAdornment(
                AdornmentPositioningBehavior.TextRelative,
                new SnapshotSpan(position, 0),
                null,
                suggestionTextBlock,
                null);
        }

        private void HideSuggestion()
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                currentSuggestion = null;
                suggestionTextBlock.Visibility = Visibility.Collapsed;
                adornmentLayer.RemoveAllAdornments();
            }
            catch
            {
            }
        }

        private void AcceptSuggestion()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(currentSuggestion))
                return;

            var caretPosition = textView.Caret.Position.BufferPosition;

            using (var edit = textView.TextBuffer.CreateEdit())
            {
                edit.Insert(caretPosition.Position, currentSuggestion);
                edit.Apply();
            }

            HideSuggestion();
        }

        private void UpdateSuggestionPosition()
        {
            if (string.IsNullOrEmpty(currentSuggestion))
                return;

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var caretPosition = textView.Caret.Position.BufferPosition;
            DisplaySuggestion(currentSuggestion, caretPosition);
        }

        private string GetFileName()
        {
            if (textDocument != null)
                return textDocument.FilePath;

            return "unknown.cs";
        }
    }

    [Export(typeof(AdornmentLayerDefinition))]
    [Name("InlineSuggestionAdornment")]
    [Order(After = PredefinedAdornmentLayers.Selection)]
    internal sealed class InlineSuggestionAdornmentLayerDefinition
    {
    }
}
