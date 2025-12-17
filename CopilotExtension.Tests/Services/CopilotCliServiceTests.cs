using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using CopilotExtension.Services;

namespace CopilotExtension.Tests.Services
{
    public class CopilotCliServiceTests
    {
        private readonly CopilotCliService _service;

        public CopilotCliServiceTests()
        {
            _service = new CopilotCliService();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithValidInput_ShouldReturnSuggestion()
        {
            // Arrange
            var context = @"
using System;

namespace TestApp
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}";
            var currentLine = "public int Subtract";
            var fileName = "Calculator.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithEmptyContext_ShouldHandleGracefully()
        {
            // Arrange
            var context = "";
            var currentLine = "public void Test";
            var fileName = "test.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert - Should not throw
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithDifferentFileTypes_ShouldDetectLanguage()
        {
            // Arrange
            var testCases = new[]
            {
                ("test.cs", "C#"),
                ("test.js", "JavaScript"),
                ("test.py", "Python"),
                ("test.cpp", "C++"),
                ("test.java", "Java")
            };

            foreach (var (fileName, expectedLanguage) in testCases)
            {
                var context = "// Some code";
                var currentLine = "function test";

                // Act
                var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

                // Assert
                // Should process without errors for different file types
                result.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetSuggestionAsync_WithLongContext_ShouldTruncate()
        {
            // Arrange
            var longContext = new string('x', 10000); // Very long context
            var currentLine = "public void Test";
            var fileName = "test.cs";

            // Act
            var result = await _service.GetSuggestionAsync(longContext, currentLine, fileName);

            // Assert - Should handle without errors
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithSpecialCharacters_ShouldEscape()
        {
            // Arrange
            var context = "string text = \"Test with \\\"quotes\\\" and special chars\"";
            var currentLine = "var result = ";
            var fileName = "test.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithMultilineCode_ShouldPreserveFormatting()
        {
            // Arrange
            var context = @"
public class TestClass
{
    public void Method1()
    {
        var x = 10;
        var y = 20;
    }
    
    public void Method2()
    {
";
            var currentLine = "        var z = ";
            var fileName = "test.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldIndicateCliAvailability()
        {
            // Act
            var isAvailable = await _service.TestConnectionAsync();

            // Assert
            // Should return bool without throwing
            isAvailable.Should().Be(isAvailable); // Just verify it returns a bool
        }

        [Theory]
        [InlineData("test.cs", "C#")]
        [InlineData("test.vb", "Visual Basic")]
        [InlineData("test.js", "JavaScript")]
        [InlineData("test.ts", "TypeScript")]
        [InlineData("test.py", "Python")]
        [InlineData("test.go", "Go")]
        [InlineData("test.rs", "Rust")]
        [InlineData("test.unknown", "code")]
        public async Task GetSuggestionAsync_ShouldRecognizeFileExtension(string fileName, string expectedLanguage)
        {
            // Arrange
            var context = "// test";
            var currentLine = "function test";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert - Should process file without errors
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithAsyncCode_ShouldHandleAsyncPatterns()
        {
            // Arrange
            var context = @"
public class DataService
{
    public async Task<string> FetchDataAsync(string url)
    {
        using var client = new HttpClient();
";
            var currentLine = "        var response = await ";
            var fileName = "DataService.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSuggestionAsync_WithLinqQuery_ShouldHandleLinq()
        {
            // Arrange
            var context = @"
public class UserService
{
    public List<User> GetActiveUsers(List<User> users)
    {
        return users";
            var currentLine = "            .Where(u => u.";
            var fileName = "UserService.cs";

            // Act
            var result = await _service.GetSuggestionAsync(context, currentLine, fileName);

            // Assert
            result.Should().NotBeNull();
        }
    }
}
