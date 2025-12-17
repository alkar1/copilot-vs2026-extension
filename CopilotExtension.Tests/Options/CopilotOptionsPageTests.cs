using System;
using FluentAssertions;
using Xunit;
using CopilotExtension.Tests.TestHelpers;

namespace CopilotExtension.Tests.Options
{
    public class CopilotOptionsPageTests
    {
        private readonly CopilotOptionsPageSimple _optionsPage;

        public CopilotOptionsPageTests()
        {
            _optionsPage = new CopilotOptionsPageSimple();
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Assert
            _optionsPage.EnableCopilot.Should().BeTrue();
            _optionsPage.CopilotCliPath.Should().BeEmpty();
            _optionsPage.AutoSuggestDelay.Should().Be(500);
            _optionsPage.MaxContextLines.Should().Be(50);
            _optionsPage.EnableInlineSuggestions.Should().BeTrue();
            _optionsPage.SuggestionOpacity.Should().Be(50);
            _optionsPage.TimeoutSeconds.Should().Be(30);
            _optionsPage.DebugMode.Should().BeFalse();
        }

        [Fact]
        public void EnableCopilot_CanBeToggled()
        {
            // Act
            _optionsPage.EnableCopilot = false;

            // Assert
            _optionsPage.EnableCopilot.Should().BeFalse();

            // Act
            _optionsPage.EnableCopilot = true;

            // Assert
            _optionsPage.EnableCopilot.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        public void AutoSuggestDelay_ShouldAcceptValidValues(int delay)
        {
            // Act
            _optionsPage.AutoSuggestDelay = delay;

            // Assert
            _optionsPage.AutoSuggestDelay.Should().Be(delay);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(200)]
        public void MaxContextLines_ShouldAcceptValidValues(int lines)
        {
            // Act
            _optionsPage.MaxContextLines = lines;

            // Assert
            _optionsPage.MaxContextLines.Should().Be(lines);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        public void SuggestionOpacity_ShouldAcceptValidRange(int opacity)
        {
            // Act
            _optionsPage.SuggestionOpacity = opacity;

            // Assert
            _optionsPage.SuggestionOpacity.Should().Be(opacity);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(120)]
        public void TimeoutSeconds_ShouldAcceptValidRange(int timeout)
        {
            // Act
            _optionsPage.TimeoutSeconds = timeout;

            // Assert
            _optionsPage.TimeoutSeconds.Should().Be(timeout);
        }

        [Fact]
        public void CopilotCliPath_CanBeSet()
        {
            // Arrange
            var testPath = @"C:\Program Files\GitHub Copilot\copilot.exe";

            // Act
            _optionsPage.CopilotCliPath = testPath;

            // Assert
            _optionsPage.CopilotCliPath.Should().Be(testPath);
        }

        [Fact]
        public void EnableInlineSuggestions_CanBeToggled()
        {
            // Act
            _optionsPage.EnableInlineSuggestions = false;

            // Assert
            _optionsPage.EnableInlineSuggestions.Should().BeFalse();

            // Act
            _optionsPage.EnableInlineSuggestions = true;

            // Assert
            _optionsPage.EnableInlineSuggestions.Should().BeTrue();
        }

        [Fact]
        public void DebugMode_CanBeToggled()
        {
            // Act
            _optionsPage.DebugMode = true;

            // Assert
            _optionsPage.DebugMode.Should().BeTrue();

            // Act
            _optionsPage.DebugMode = false;

            // Assert
            _optionsPage.DebugMode.Should().BeFalse();
        }

        [Fact]
        public void AllProperties_ShouldBeIndependent()
        {
            // Act - Set all properties to non-default values
            _optionsPage.EnableCopilot = false;
            _optionsPage.CopilotCliPath = @"C:\test\copilot.exe";
            _optionsPage.AutoSuggestDelay = 1000;
            _optionsPage.MaxContextLines = 100;
            _optionsPage.EnableInlineSuggestions = false;
            _optionsPage.SuggestionOpacity = 75;
            _optionsPage.TimeoutSeconds = 60;
            _optionsPage.DebugMode = true;

            // Assert - All should maintain their values
            _optionsPage.EnableCopilot.Should().BeFalse();
            _optionsPage.CopilotCliPath.Should().Be(@"C:\test\copilot.exe");
            _optionsPage.AutoSuggestDelay.Should().Be(1000);
            _optionsPage.MaxContextLines.Should().Be(100);
            _optionsPage.EnableInlineSuggestions.Should().BeFalse();
            _optionsPage.SuggestionOpacity.Should().Be(75);
            _optionsPage.TimeoutSeconds.Should().Be(60);
            _optionsPage.DebugMode.Should().BeTrue();
        }

        [Fact]
        public void ApplySettings_ShouldEnforceValidationRules()
        {
            // Arrange - Set invalid values
            _optionsPage.AutoSuggestDelay = -100;
            _optionsPage.MaxContextLines = 5;
            _optionsPage.SuggestionOpacity = 150;
            _optionsPage.TimeoutSeconds = 2;

            // Act
            _optionsPage.ApplySettings();

            // Assert - Values should be corrected
            _optionsPage.AutoSuggestDelay.Should().BeGreaterOrEqualTo(0);
            _optionsPage.MaxContextLines.Should().BeGreaterOrEqualTo(10);
            _optionsPage.SuggestionOpacity.Should().BeLessOrEqualTo(100);
            _optionsPage.TimeoutSeconds.Should().BeGreaterOrEqualTo(5);
        }
    }
}
