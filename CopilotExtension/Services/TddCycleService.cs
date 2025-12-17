using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopilotExtension.Services
{
    /// <summary>
    /// Service for managing Test-Driven Development cycle:
    /// Write Test -> Run Test -> Fix Code -> Refactor
    /// </summary>
    public class TddCycleService
    {
        private readonly CopilotCliService copilotService;
        private readonly TestRunnerService testRunnerService;

        public TddCycleService()
        {
            copilotService = new CopilotCliService();
            testRunnerService = new TestRunnerService();
        }

        #region Test Generation

        /// <summary>
        /// Generate unit tests for the provided code
        /// </summary>
        public async Task<string> GenerateTestsAsync(string sourceCode, string className, string language)
        {
            var prompt = BuildTestGenerationPrompt(sourceCode, className, language);
            var suggestion = await copilotService.GetSuggestionAsync("", prompt, $"test.{GetFileExtension(language)}");
            return ParseTestCode(suggestion);
        }

        /// <summary>
        /// Generate test for specific method
        /// </summary>
        public async Task<string> GenerateMethodTestAsync(string methodCode, string methodName, string language)
        {
            var prompt = $@"Generate a comprehensive unit test for this {language} method:

{methodCode}

Test should cover:
- Happy path scenarios
- Edge cases
- Error conditions
- Null/empty inputs

Use appropriate test framework (xUnit for C#, Jest for JS, pytest for Python)";

            var suggestion = await copilotService.GetSuggestionAsync("", prompt, $"test.{GetFileExtension(language)}");
            return ParseTestCode(suggestion);
        }

        #endregion

        #region Test Analysis

        /// <summary>
        /// Analyze test failures and suggest fixes
        /// </summary>
        public async Task<TestAnalysisResult> AnalyzeTestFailuresAsync(
            string sourceCode,
            List<TestFailure> failures,
            string language)
        {
            var result = new TestAnalysisResult
            {
                Failures = failures,
                Suggestions = new List<FixSuggestion>()
            };

            foreach (var failure in failures)
            {
                var suggestion = await GenerateFixSuggestionAsync(sourceCode, failure, language);
                result.Suggestions.Add(suggestion);
            }

            return result;
        }

        /// <summary>
        /// Generate fix suggestion for a failing test
        /// </summary>
        private async Task<FixSuggestion> GenerateFixSuggestionAsync(
            string sourceCode, 
            TestFailure failure, 
            string language)
        {
            var prompt = $@"Analyze this {language} test failure and suggest a fix:

SOURCE CODE:
{sourceCode}

TEST NAME: {failure.TestName}
ERROR MESSAGE: {failure.ErrorMessage}
STACK TRACE:
{failure.StackTrace}

Provide:
1. Root cause analysis
2. Specific code fix
3. Explanation of the fix";

            var analysis = await copilotService.GetSuggestionAsync("", prompt, $"fix.{GetFileExtension(language)}");
            
            return new FixSuggestion
            {
                TestName = failure.TestName,
                RootCause = ExtractRootCause(analysis),
                ProposedFix = ExtractCodeFix(analysis),
                Explanation = ExtractExplanation(analysis)
            };
        }

        #endregion

        #region Full TDD Cycle

        /// <summary>
        /// Execute complete TDD cycle:
        /// 1. Generate tests
        /// 2. Run tests
        /// 3. Analyze failures
        /// 4. Suggest fixes
        /// </summary>
        public async Task<TddCycleResult> ExecuteTddCycleAsync(
            string sourceCode,
            string testCode,
            string projectPath,
            string language)
        {
            var result = new TddCycleResult();

            // Step 1: Generate tests if needed
            if (string.IsNullOrEmpty(testCode))
            {
                result.GeneratedTests = await GenerateTestsAsync(sourceCode, "TestClass", language);
                testCode = result.GeneratedTests;
            }

            // Step 2: Run tests
            result.TestResults = await testRunnerService.RunTestsAsync(projectPath);

            // Step 3: Analyze failures
            if (result.TestResults.FailedTests.Any())
            {
                result.Analysis = await AnalyzeTestFailuresAsync(
                    sourceCode,
                    result.TestResults.FailedTests,
                    language);

                // Step 4: Auto-fix if configured
                result.SuggestedFixes = result.Analysis.Suggestions;
            }

            result.Success = !result.TestResults.FailedTests.Any();
            return result;
        }

        /// <summary>
        /// Iterative TDD cycle until all tests pass or max iterations
        /// </summary>
        public async Task<IterativeTddResult> RunIterativeTddCycleAsync(
            string sourceCode,
            string testCode,
            string projectPath,
            string language,
            int maxIterations = 5)
        {
            var iterations = new List<TddCycleResult>();
            var currentCode = sourceCode;

            for (int i = 0; i < maxIterations; i++)
            {
                var cycleResult = await ExecuteTddCycleAsync(currentCode, testCode, projectPath, language);
                iterations.Add(cycleResult);

                if (cycleResult.Success)
                {
                    return new IterativeTddResult
                    {
                        Success = true,
                        Iterations = iterations,
                        FinalCode = currentCode,
                        Message = $"All tests passed after {i + 1} iteration(s)"
                    };
                }

                // Apply suggested fixes
                if (cycleResult.SuggestedFixes.Any())
                {
                    currentCode = ApplyFixes(currentCode, cycleResult.SuggestedFixes);
                }
                else
                {
                    break; // No more suggestions, stop iterating
                }
            }

            return new IterativeTddResult
            {
                Success = false,
                Iterations = iterations,
                FinalCode = currentCode,
                Message = $"Tests still failing after {maxIterations} iterations"
            };
        }

        #endregion

        #region Code Refactoring

        /// <summary>
        /// Suggest refactorings after tests pass
        /// </summary>
        public async Task<List<RefactoringSuggestion>> SuggestRefactoringsAsync(
            string sourceCode,
            string testCode,
            string language)
        {
            var prompt = $@"Analyze this {language} code and suggest refactorings while keeping tests passing:

SOURCE CODE:
{sourceCode}

TESTS:
{testCode}

Suggest refactorings for:
- Code duplication
- Complex methods
- Poor naming
- Design patterns
- Performance improvements

Ensure all tests will still pass after refactoring.";

            var suggestions = await copilotService.GetSuggestionAsync("", prompt, $"refactor.{GetFileExtension(language)}");
            return ParseRefactoringSuggestions(suggestions);
        }

        #endregion

        #region Helper Methods

        private string BuildTestGenerationPrompt(string sourceCode, string className, string language)
        {
            return $@"Generate comprehensive unit tests for this {language} class:

{sourceCode}

Requirements:
- Test all public methods
- Cover happy paths and edge cases
- Include error handling tests
- Use appropriate assertions
- Follow {GetTestFramework(language)} conventions
- Add descriptive test names

Class under test: {className}";
        }

        private string GetTestFramework(string language)
        {
            return language.ToLower() switch
            {
                "c#" or "csharp" => "xUnit",
                "javascript" or "typescript" => "Jest",
                "python" => "pytest",
                "java" => "JUnit",
                _ => "appropriate test framework"
            };
        }

        private string GetFileExtension(string language)
        {
            return language.ToLower() switch
            {
                "c#" or "csharp" => "cs",
                "javascript" => "js",
                "typescript" => "ts",
                "python" => "py",
                "java" => "java",
                _ => "txt"
            };
        }

        private string ParseTestCode(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return string.Empty;

            // Remove markdown code blocks
            if (suggestion.Contains("```"))
            {
                var start = suggestion.IndexOf("```");
                var end = suggestion.LastIndexOf("```");
                if (end > start)
                {
                    var code = suggestion.Substring(start, end - start);
                    code = code.Substring(code.IndexOf('\n') + 1); // Remove language identifier
                    return code.Trim();
                }
            }

            return suggestion.Trim();
        }

        private string ExtractRootCause(string analysis)
        {
            // Extract root cause from Copilot analysis
            var lines = analysis.Split('\n');
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("root cause") || line.ToLower().Contains("problem"))
                {
                    return line.Trim();
                }
            }
            return analysis.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown cause";
        }

        private string ExtractCodeFix(string analysis)
        {
            // Extract code fix from analysis
            if (analysis.Contains("```"))
            {
                return ParseTestCode(analysis);
            }

            // Try to find code-like content
            var lines = analysis.Split('\n');
            var codeLines = lines.Where(l => 
                l.Contains("(") || 
                l.Contains("{") || 
                l.Contains("=") ||
                l.Trim().StartsWith("public") ||
                l.Trim().StartsWith("private")).ToList();

            return string.Join("\n", codeLines);
        }

        private string ExtractExplanation(string analysis)
        {
            // Extract explanation text
            var lines = analysis.Split('\n').Where(l => 
                !l.Contains("```") && 
                !string.IsNullOrWhiteSpace(l)).ToList();

            return string.Join("\n", lines);
        }

        private string ApplyFixes(string sourceCode, List<FixSuggestion> fixes)
        {
            // Simple fix application - replace or insert suggested code
            // In real implementation, this would use Roslyn for C# or similar for other languages
            var result = sourceCode;

            foreach (var fix in fixes)
            {
                if (!string.IsNullOrEmpty(fix.ProposedFix))
                {
                    // This is a simplified version
                    // Real implementation would need proper code analysis and modification
                    result += "\n\n// Suggested fix:\n" + fix.ProposedFix;
                }
            }

            return result;
        }

        private List<RefactoringSuggestion> ParseRefactoringSuggestions(string suggestions)
        {
            var result = new List<RefactoringSuggestion>();
            var lines = suggestions.Split('\n');

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                result.Add(new RefactoringSuggestion
                {
                    Description = line.Trim(),
                    Type = DetermineRefactoringType(line)
                });
            }

            return result;
        }

        private RefactoringType DetermineRefactoringType(string description)
        {
            var lower = description.ToLower();
            if (lower.Contains("extract") && lower.Contains("method"))
                return RefactoringType.ExtractMethod;
            if (lower.Contains("rename"))
                return RefactoringType.Rename;
            if (lower.Contains("duplicate"))
                return RefactoringType.RemoveDuplication;
            if (lower.Contains("pattern"))
                return RefactoringType.ApplyPattern;

            return RefactoringType.Other;
        }

        #endregion
    }

    #region Data Models

    public class TestFailure
    {
        public string TestName { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string ExpectedValue { get; set; }
        public string ActualValue { get; set; }
    }

    public class FixSuggestion
    {
        public string TestName { get; set; }
        public string RootCause { get; set; }
        public string ProposedFix { get; set; }
        public string Explanation { get; set; }
    }

    public class TestAnalysisResult
    {
        public List<TestFailure> Failures { get; set; }
        public List<FixSuggestion> Suggestions { get; set; }
    }

    public class TddCycleResult
    {
        public string GeneratedTests { get; set; }
        public TestRunResult TestResults { get; set; }
        public TestAnalysisResult Analysis { get; set; }
        public List<FixSuggestion> SuggestedFixes { get; set; }
        public bool Success { get; set; }
    }

    public class IterativeTddResult
    {
        public bool Success { get; set; }
        public List<TddCycleResult> Iterations { get; set; }
        public string FinalCode { get; set; }
        public string Message { get; set; }
    }

    public class RefactoringSuggestion
    {
        public string Description { get; set; }
        public RefactoringType Type { get; set; }
        public string ProposedCode { get; set; }
    }

    public enum RefactoringType
    {
        ExtractMethod,
        Rename,
        RemoveDuplication,
        ApplyPattern,
        SimplifyLogic,
        Other
    }

    #endregion
}
