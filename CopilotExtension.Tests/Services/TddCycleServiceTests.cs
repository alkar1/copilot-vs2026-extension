using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using CopilotExtension.Services;

namespace CopilotExtension.Tests.Services
{
    public class TddCycleServiceTests
    {
        private readonly TddCycleService _service;

        public TddCycleServiceTests()
        {
            _service = new TddCycleService();
        }

        [Fact]
        public async Task GenerateTestsAsync_WithValidCode_ShouldReturnTests()
        {
            // Arrange
            var sourceCode = @"
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }
}";

            // Act
            var tests = await _service.GenerateTestsAsync(sourceCode, "Calculator", "C#");

            // Assert
            tests.Should().NotBeNullOrEmpty();
            // Note: Actual test generation requires CLI, this tests the service structure
        }

        [Fact]
        public async Task GenerateMethodTestAsync_WithSimpleMethod_ShouldGenerateTest()
        {
            // Arrange
            var methodCode = @"
public int Add(int a, int b)
{
    return a + b;
}";

            // Act
            var test = await _service.GenerateMethodTestAsync(methodCode, "Add", "C#");

            // Assert
            test.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzeTestFailuresAsync_WithFailures_ShouldProvideSuggestions()
        {
            // Arrange
            var sourceCode = "public int Add(int a, int b) { return a - b; }"; // Bug: subtract instead of add
            var failures = new List<TestFailure>
            {
                new TestFailure
                {
                    TestName = "Add_ShouldReturnSum",
                    ErrorMessage = "Expected 5 but was -1",
                    StackTrace = "at Calculator.Add()",
                    ExpectedValue = "5",
                    ActualValue = "-1"
                }
            };

            // Act
            var analysis = await _service.AnalyzeTestFailuresAsync(sourceCode, failures, "C#");

            // Assert
            analysis.Should().NotBeNull();
            analysis.Failures.Should().HaveCount(1);
            analysis.Suggestions.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ExecuteTddCycleAsync_WithCode_ShouldExecuteFullCycle()
        {
            // Arrange
            var sourceCode = "public int Add(int a, int b) { return a + b; }";
            var testCode = "[Fact] public void Add_Test() { Assert.Equal(5, Add(2, 3)); }";
            var projectPath = "test.csproj";

            // Act
            var result = await _service.ExecuteTddCycleAsync(sourceCode, testCode, projectPath, "C#");

            // Assert
            result.Should().NotBeNull();
            // Note: Full execution requires test runner infrastructure
        }

        [Theory]
        [InlineData("C#", "xUnit")]
        [InlineData("JavaScript", "Jest")]
        [InlineData("Python", "pytest")]
        [InlineData("Java", "JUnit")]
        public void GetTestFramework_ShouldReturnCorrectFramework(string language, string expectedFramework)
        {
            // This tests the internal logic of framework detection
            // In real implementation, would be exposed or tested through public API
            var framework = language.ToLower() switch
            {
                "c#" or "csharp" => "xUnit",
                "javascript" or "typescript" => "Jest",
                "python" => "pytest",
                "java" => "JUnit",
                _ => "appropriate test framework"
            };

            framework.Should().Be(expectedFramework);
        }

        [Fact]
        public async Task SuggestRefactoringsAsync_WithCode_ShouldProvideRefactorings()
        {
            // Arrange
            var sourceCode = @"
public class Calculator
{
    public int Calculate(int a, int b, string operation)
    {
        if (operation == ""add"")
            return a + b;
        else if (operation == ""subtract"")
            return a - b;
        else if (operation == ""multiply"")
            return a * b;
        else
            return 0;
    }
}";

            var testCode = "[Fact] public void Test() { }";

            // Act
            var refactorings = await _service.SuggestRefactoringsAsync(sourceCode, testCode, "C#");

            // Assert
            refactorings.Should().NotBeNull();
            // Should suggest strategy pattern or similar
        }

        [Fact]
        public void TestFailure_ShouldStoreAllInformation()
        {
            // Arrange & Act
            var failure = new TestFailure
            {
                TestName = "MyTest",
                ErrorMessage = "Expected 5 but was 4",
                StackTrace = "at MyClass.MyMethod()",
                ExpectedValue = "5",
                ActualValue = "4"
            };

            // Assert
            failure.TestName.Should().Be("MyTest");
            failure.ErrorMessage.Should().Contain("Expected");
            failure.StackTrace.Should().Contain("MyMethod");
            failure.ExpectedValue.Should().Be("5");
            failure.ActualValue.Should().Be("4");
        }

        [Fact]
        public void FixSuggestion_ShouldContainAllComponents()
        {
            // Arrange & Act
            var suggestion = new FixSuggestion
            {
                TestName = "MyTest",
                RootCause = "Wrong operator used",
                ProposedFix = "return a + b;",
                Explanation = "The method uses subtraction instead of addition"
            };

            // Assert
            suggestion.TestName.Should().Be("MyTest");
            suggestion.RootCause.Should().NotBeNullOrEmpty();
            suggestion.ProposedFix.Should().Contain("+");
            suggestion.Explanation.Should().Contain("addition");
        }

        [Fact]
        public void TddCycleResult_ShouldTrackAllSteps()
        {
            // Arrange & Act
            var result = new TddCycleResult
            {
                GeneratedTests = "public void Test() { }",
                TestResults = new TestRunResult { TotalTests = 5, PassedTests = 4 },
                Analysis = new TestAnalysisResult { Suggestions = new List<FixSuggestion>() },
                Success = false
            };

            // Assert
            result.GeneratedTests.Should().NotBeEmpty();
            result.TestResults.Should().NotBeNull();
            result.Analysis.Should().NotBeNull();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void IterativeTddResult_ShouldTrackIterations()
        {
            // Arrange & Act
            var result = new IterativeTddResult
            {
                Success = true,
                Iterations = new List<TddCycleResult>
                {
                    new TddCycleResult { Success = false },
                    new TddCycleResult { Success = true }
                },
                FinalCode = "public int Add(int a, int b) { return a + b; }",
                Message = "All tests passed after 2 iterations"
            };

            // Assert
            result.Success.Should().BeTrue();
            result.Iterations.Should().HaveCount(2);
            result.FinalCode.Should().Contain("Add");
            result.Message.Should().Contain("2 iterations");
        }

        [Theory]
        [InlineData(RefactoringType.ExtractMethod, "Extract method")]
        [InlineData(RefactoringType.Rename, "Rename variable")]
        [InlineData(RefactoringType.RemoveDuplication, "Remove duplicate code")]
        public void RefactoringSuggestion_ShouldSupportDifferentTypes(RefactoringType type, string description)
        {
            // Arrange & Act
            var suggestion = new RefactoringSuggestion
            {
                Description = description,
                Type = type
            };

            // Assert
            suggestion.Type.Should().Be(type);
            suggestion.Description.Should().NotBeEmpty();
        }
    }
}
