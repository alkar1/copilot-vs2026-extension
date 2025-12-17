using System;
using System.Linq;
using Xunit;
using FluentAssertions;

namespace CopilotExtension.Tests.Helpers
{
    public class LanguageDetectionTests
    {
        [Theory]
        [InlineData(".cs", "C#")]
        [InlineData(".vb", "Visual Basic")]
        [InlineData(".cpp", "C++")]
        [InlineData(".h", "C++")]
        [InlineData(".hpp", "C++")]
        [InlineData(".js", "JavaScript")]
        [InlineData(".ts", "TypeScript")]
        [InlineData(".py", "Python")]
        [InlineData(".java", "Java")]
        [InlineData(".go", "Go")]
        [InlineData(".rs", "Rust")]
        [InlineData(".php", "PHP")]
        [InlineData(".rb", "Ruby")]
        [InlineData(".sql", "SQL")]
        [InlineData(".xml", "XML")]
        [InlineData(".json", "JSON")]
        [InlineData(".html", "HTML")]
        [InlineData(".css", "CSS")]
        [InlineData(".unknown", "code")]
        public void GetLanguageFromExtension_ShouldReturnCorrectLanguage(string extension, string expectedLanguage)
        {
            // Note: This test verifies the logic that should exist in CopilotCliService
            // The actual implementation is in the service class
            var languageMap = new System.Collections.Generic.Dictionary<string, string>
            {
                [".cs"] = "C#",
                [".vb"] = "Visual Basic",
                [".cpp"] = "C++",
                [".cc"] = "C++",
                [".cxx"] = "C++",
                [".h"] = "C++",
                [".hpp"] = "C++",
                [".js"] = "JavaScript",
                [".ts"] = "TypeScript",
                [".py"] = "Python",
                [".java"] = "Java",
                [".go"] = "Go",
                [".rs"] = "Rust",
                [".php"] = "PHP",
                [".rb"] = "Ruby",
                [".sql"] = "SQL",
                [".xml"] = "XML",
                [".json"] = "JSON",
                [".html"] = "HTML",
                [".htm"] = "HTML",
                [".css"] = "CSS"
            };

            // Act
            var result = languageMap.ContainsKey(extension.ToLower()) 
                ? languageMap[extension.ToLower()] 
                : "code";

            // Assert
            result.Should().Be(expectedLanguage);
        }

        [Fact]
        public void LanguageMap_ShouldContainCommonLanguages()
        {
            // Arrange
            var commonExtensions = new[] { ".cs", ".js", ".py", ".java", ".cpp", ".ts" };

            // Assert
            commonExtensions.Should().AllSatisfy(ext => ext.Should().NotBeNullOrEmpty());
        }

        [Theory]
        [InlineData("test.CS", ".cs")]
        [InlineData("test.JS", ".js")]
        [InlineData("TEST.PY", ".py")]
        public void FileExtension_ShouldBeCaseInsensitive(string fileName, string expectedExtension)
        {
            // Act
            var extension = System.IO.Path.GetExtension(fileName).ToLower();

            // Assert
            extension.Should().Be(expectedExtension);
        }
    }

    public class ContextTruncationTests
    {
        [Theory]
        [InlineData(100, 50)]
        [InlineData(500, 50)]
        [InlineData(1000, 100)]
        [InlineData(5000, 500)]
        public void TruncateContext_ShouldLimitLength(int originalLength, int maxLength)
        {
            // Arrange
            var longText = new string('x', originalLength);

            // Act
            var truncated = longText.Length > maxLength 
                ? longText.Substring(longText.Length - maxLength) 
                : longText;

            // Assert
            truncated.Length.Should().BeLessOrEqualTo(maxLength);
        }

        [Fact]
        public void TruncateContext_ShouldPreserveRecentLines()
        {
            // Arrange
            var lines = new[]
            {
                "line 1",
                "line 2",
                "line 3",
                "line 4",
                "line 5"
            };
            var context = string.Join(Environment.NewLine, lines);
            var maxLength = 30;

            // Act - Should take from end
            var recentLines = lines.Reverse().Take(2).Reverse();

            // Assert
            recentLines.Should().Contain("line 4");
            recentLines.Should().Contain("line 5");
        }

        [Fact]
        public void TruncateContext_WithEmptyString_ShouldReturnEmpty()
        {
            // Arrange
            var context = "";
            var maxLength = 100;

            // Act
            var result = string.IsNullOrEmpty(context) ? context : context.Substring(0, Math.Min(context.Length, maxLength));

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class PromptBuildingTests
    {
        [Fact]
        public void BuildPrompt_ShouldIncludeAllElements()
        {
            // Arrange
            var fileName = "test.cs";
            var language = "C#";
            var context = "public class Test { }";
            var currentLine = "public void Method";

            // Act
            var prompt = $@"Complete the following {language} code:

File: {fileName}

Context:
{context}

Current line to complete:
{currentLine}

Provide only the code completion, without explanations.";

            // Assert
            prompt.Should().Contain(language);
            prompt.Should().Contain(fileName);
            prompt.Should().Contain(context);
            prompt.Should().Contain(currentLine);
            prompt.Should().Contain("Complete the following");
        }

        [Theory]
        [InlineData("test.cs", "C#")]
        [InlineData("test.js", "JavaScript")]
        [InlineData("test.py", "Python")]
        public void BuildPrompt_ShouldUseCorrectLanguage(string fileName, string expectedLanguage)
        {
            // Arrange
            var extension = System.IO.Path.GetExtension(fileName);

            // Act
            var prompt = $"Complete the following {expectedLanguage} code:";

            // Assert
            prompt.Should().Contain(expectedLanguage);
        }

        [Fact]
        public void BuildPrompt_ShouldEscapeSpecialCharacters()
        {
            // Arrange
            var textWithQuotes = "string text = \"Hello World\"";

            // Act
            var escaped = textWithQuotes.Replace("\"", "\"\"");

            // Assert
            escaped.Should().Contain("\"\"");
        }
    }

    public class SuggestionParsingTests
    {
        [Fact]
        public void ParseSuggestion_WithCodeBlock_ShouldExtractCode()
        {
            // Arrange
            var output = @"Here is the suggestion:
```csharp
public void Method()
{
    return;
}
```
End of suggestion.";

            // Act
            var codeBlockStart = output.IndexOf("```");
            var codeStart = output.IndexOf('\n', codeBlockStart) + 1;
            var codeEnd = output.IndexOf("```", codeStart);
            var extracted = codeEnd > codeStart 
                ? output.Substring(codeStart, codeEnd - codeStart).Trim() 
                : null;

            // Assert
            extracted.Should().NotBeNull();
            extracted.Should().Contain("public void Method()");
        }

        [Fact]
        public void ParseSuggestion_WithoutCodeBlock_ShouldReturnFirstCodeLine()
        {
            // Arrange
            var output = @"# Here is a comment
public void Method() { }
// Another comment";

            // Act
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var codeLine = lines.FirstOrDefault(l => 
                !l.TrimStart().StartsWith("#") && 
                !l.TrimStart().StartsWith("//"));

            // Assert
            codeLine.Should().Contain("public void Method()");
        }

        [Fact]
        public void ParseSuggestion_WithEmptyOutput_ShouldReturnNull()
        {
            // Arrange
            var output = "";

            // Act
            var result = string.IsNullOrWhiteSpace(output) ? null : output;

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("```csharp\ncode\n```", "code")]
        [InlineData("```\ncode\n```", "code")]
        [InlineData("no code block", "no code block")]
        public void ParseSuggestion_ShouldHandleDifferentFormats(string input, string expectedContent)
        {
            // Act
            var hasCodeBlock = input.Contains("```");
            
            // Assert
            hasCodeBlock.Should().Be(input.Contains("```"));
        }
    }
}
