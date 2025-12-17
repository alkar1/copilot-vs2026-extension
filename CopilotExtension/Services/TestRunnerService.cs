using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CopilotExtension.Services
{
    /// <summary>
    /// Service for running tests and capturing results
    /// </summary>
    public class TestRunnerService
    {
        private readonly int timeoutSeconds = 120; // 2 minutes default

        /// <summary>
        /// Run tests for a project
        /// </summary>
        public async Task<TestRunResult> RunTestsAsync(string projectPath)
        {
            var result = new TestRunResult
            {
                ProjectPath = projectPath,
                StartTime = DateTime.Now
            };

            try
            {
                // Determine test framework
                var framework = DetectTestFramework(projectPath);
                result.TestFramework = framework;

                // Run tests based on framework
                var output = await ExecuteTestRunnerAsync(projectPath, framework);
                
                // Parse results
                ParseTestResults(output, result, framework);

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.FailedTests.Count == 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Run specific test class
        /// </summary>
        public async Task<TestRunResult> RunTestClassAsync(string projectPath, string testClassName)
        {
            var framework = DetectTestFramework(projectPath);
            var filter = BuildTestFilter(framework, testClassName);
            var output = await ExecuteTestRunnerAsync(projectPath, framework, filter);

            var result = new TestRunResult
            {
                ProjectPath = projectPath,
                TestFramework = framework,
                StartTime = DateTime.Now
            };

            ParseTestResults(output, result, framework);
            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            return result;
        }

        /// <summary>
        /// Run specific test method
        /// </summary>
        public async Task<TestRunResult> RunTestMethodAsync(string projectPath, string testClassName, string testMethodName)
        {
            var framework = DetectTestFramework(projectPath);
            var filter = BuildTestFilter(framework, testClassName, testMethodName);
            var output = await ExecuteTestRunnerAsync(projectPath, framework, filter);

            var result = new TestRunResult
            {
                ProjectPath = projectPath,
                TestFramework = framework,
                StartTime = DateTime.Now
            };

            ParseTestResults(output, result, framework);
            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            return result;
        }

        #region Private Methods

        private TestFramework DetectTestFramework(string projectPath)
        {
            if (!File.Exists(projectPath))
                return TestFramework.Unknown;

            var content = File.ReadAllText(projectPath);

            if (content.Contains("xunit", StringComparison.OrdinalIgnoreCase))
                return TestFramework.XUnit;
            if (content.Contains("nunit", StringComparison.OrdinalIgnoreCase))
                return TestFramework.NUnit;
            if (content.Contains("mstest", StringComparison.OrdinalIgnoreCase))
                return TestFramework.MSTest;
            if (content.Contains("jest", StringComparison.OrdinalIgnoreCase))
                return TestFramework.Jest;
            if (content.Contains("pytest", StringComparison.OrdinalIgnoreCase))
                return TestFramework.Pytest;

            return TestFramework.Unknown;
        }

        private async Task<string> ExecuteTestRunnerAsync(
            string projectPath, 
            TestFramework framework, 
            string filter = null)
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            string command, arguments;

            switch (framework)
            {
                case TestFramework.XUnit:
                case TestFramework.NUnit:
                case TestFramework.MSTest:
                    command = "dotnet";
                    arguments = $"test \"{projectPath}\" --verbosity normal";
                    if (!string.IsNullOrEmpty(filter))
                        arguments += $" --filter \"{filter}\"";
                    break;

                case TestFramework.Jest:
                    command = "npm";
                    arguments = "test";
                    if (!string.IsNullOrEmpty(filter))
                        arguments += $" -- --testNamePattern=\"{filter}\"";
                    break;

                case TestFramework.Pytest:
                    command = "pytest";
                    arguments = projectDir;
                    if (!string.IsNullOrEmpty(filter))
                        arguments += $" -k \"{filter}\"";
                    break;

                default:
                    throw new NotSupportedException($"Test framework {framework} not supported");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var completedTask = await Task.WhenAny(
                Task.WhenAll(outputTask, errorTask),
                Task.Delay(timeoutSeconds * 1000)
            );

            if (completedTask != Task.WhenAll(outputTask, errorTask))
            {
                process.Kill();
                throw new TimeoutException($"Test execution timed out after {timeoutSeconds} seconds");
            }

            process.WaitForExit();

            var output = await outputTask;
            var error = await errorTask;

            return output + "\n" + error;
        }

        private string BuildTestFilter(TestFramework framework, string className, string methodName = null)
        {
            switch (framework)
            {
                case TestFramework.XUnit:
                case TestFramework.NUnit:
                case TestFramework.MSTest:
                    if (!string.IsNullOrEmpty(methodName))
                        return $"FullyQualifiedName~{className}.{methodName}";
                    return $"FullyQualifiedName~{className}";

                case TestFramework.Jest:
                    if (!string.IsNullOrEmpty(methodName))
                        return $"{className}.*{methodName}";
                    return className;

                case TestFramework.Pytest:
                    if (!string.IsNullOrEmpty(methodName))
                        return $"{className} and {methodName}";
                    return className;

                default:
                    return string.Empty;
            }
        }

        private void ParseTestResults(string output, TestRunResult result, TestFramework framework)
        {
            switch (framework)
            {
                case TestFramework.XUnit:
                    ParseXUnitResults(output, result);
                    break;
                case TestFramework.NUnit:
                    ParseNUnitResults(output, result);
                    break;
                case TestFramework.MSTest:
                    ParseMSTestResults(output, result);
                    break;
                case TestFramework.Jest:
                    ParseJestResults(output, result);
                    break;
                case TestFramework.Pytest:
                    ParsePytestResults(output, result);
                    break;
            }
        }

        private void ParseXUnitResults(string output, TestRunResult result)
        {
            // Parse xUnit output format
            // Example: "Test summary: total: 88; failed: 16; succeeded: 72; skipped: 0"
            
            var summaryMatch = Regex.Match(output, @"total:\s*(\d+);\s*failed:\s*(\d+);\s*succeeded:\s*(\d+);\s*skipped:\s*(\d+)");
            if (summaryMatch.Success)
            {
                result.TotalTests = int.Parse(summaryMatch.Groups[1].Value);
                result.FailedTests = ParseFailedTests(output, result.TotalTests - int.Parse(summaryMatch.Groups[3].Value));
                result.PassedTests = int.Parse(summaryMatch.Groups[3].Value);
                result.SkippedTests = int.Parse(summaryMatch.Groups[4].Value);
            }

            // Parse individual test failures
            var failureMatches = Regex.Matches(output, @"error TESTERROR:[\s\S]*?CopilotExtension\.Tests\.(.*?)\s*\(.*?\):\s*Error Message:\s*(.*?)(?:\r?\n|$)");
            foreach (Match match in failureMatches)
            {
                var testName = match.Groups[1].Value.Trim();
                var errorMsg = match.Groups[2].Value.Trim();
                
                if (!result.FailedTests.Any(f => f.TestName == testName))
                {
                    result.FailedTests.Add(new TestFailure
                    {
                        TestName = testName,
                        ErrorMessage = errorMsg,
                        StackTrace = ExtractStackTrace(output, testName)
                    });
                }
            }
        }

        private void ParseNUnitResults(string output, TestRunResult result)
        {
            // Parse NUnit output
            var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)");
            var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)");
            var skippedMatch = Regex.Match(output, @"Skipped:\s*(\d+)");

            if (passedMatch.Success) result.PassedTests = int.Parse(passedMatch.Groups[1].Value);
            if (failedMatch.Success)
            {
                var failedCount = int.Parse(failedMatch.Groups[1].Value);
                result.FailedTests = ParseFailedTests(output, failedCount);
            }
            if (skippedMatch.Success) result.SkippedTests = int.Parse(skippedMatch.Groups[1].Value);

            result.TotalTests = result.PassedTests + result.FailedTests.Count + result.SkippedTests;
        }

        private void ParseMSTestResults(string output, TestRunResult result)
        {
            // Parse MSTest output
            var totalMatch = Regex.Match(output, @"Total tests:\s*(\d+)");
            var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)");
            var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)");

            if (totalMatch.Success) result.TotalTests = int.Parse(totalMatch.Groups[1].Value);
            if (passedMatch.Success) result.PassedTests = int.Parse(passedMatch.Groups[1].Value);
            if (failedMatch.Success)
            {
                var failedCount = int.Parse(failedMatch.Groups[1].Value);
                result.FailedTests = ParseFailedTests(output, failedCount);
            }
        }

        private void ParseJestResults(string output, TestRunResult result)
        {
            // Parse Jest output
            var testsMatch = Regex.Match(output, @"Tests:\s*(\d+)\s*failed,\s*(\d+)\s*passed,\s*(\d+)\s*total");
            if (testsMatch.Success)
            {
                result.FailedTests = ParseFailedTests(output, int.Parse(testsMatch.Groups[1].Value));
                result.PassedTests = int.Parse(testsMatch.Groups[2].Value);
                result.TotalTests = int.Parse(testsMatch.Groups[3].Value);
            }
        }

        private void ParsePytestResults(string output, TestRunResult result)
        {
            // Parse pytest output
            var resultsMatch = Regex.Match(output, @"(\d+)\s*passed(?:,\s*(\d+)\s*failed)?");
            if (resultsMatch.Success)
            {
                result.PassedTests = int.Parse(resultsMatch.Groups[1].Value);
                if (resultsMatch.Groups[2].Success)
                {
                    var failedCount = int.Parse(resultsMatch.Groups[2].Value);
                    result.FailedTests = ParseFailedTests(output, failedCount);
                }
                result.TotalTests = result.PassedTests + result.FailedTests.Count;
            }
        }

        private List<TestFailure> ParseFailedTests(string output, int expectedCount)
        {
            var failures = new List<TestFailure>();
            
            // Generic failure parsing - can be enhanced for specific frameworks
            var lines = output.Split('\n');
            TestFailure currentFailure = null;

            foreach (var line in lines)
            {
                if (line.Contains("Error Message:") || line.Contains("FAILED"))
                {
                    if (currentFailure != null)
                        failures.Add(currentFailure);

                    currentFailure = new TestFailure
                    {
                        TestName = ExtractTestName(line),
                        ErrorMessage = line.Trim()
                    };
                }
                else if (currentFailure != null && line.Contains("Stack Trace:"))
                {
                    currentFailure.StackTrace = line.Trim();
                }
            }

            if (currentFailure != null)
                failures.Add(currentFailure);

            return failures;
        }

        private string ExtractTestName(string line)
        {
            // Try to extract test name from various formats
            var match = Regex.Match(line, @"Tests\.(\w+)\.(\w+)");
            if (match.Success)
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}";

            match = Regex.Match(line, @"(\w+\.\w+)\s*\(");
            if (match.Success)
                return match.Groups[1].Value;

            return "Unknown Test";
        }

        private string ExtractStackTrace(string output, string testName)
        {
            var lines = output.Split('\n');
            var capturing = false;
            var stackTrace = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Contains(testName))
                {
                    capturing = true;
                    continue;
                }

                if (capturing)
                {
                    if (line.Contains("---") || string.IsNullOrWhiteSpace(line))
                        break;
                    stackTrace.AppendLine(line);
                }
            }

            return stackTrace.ToString();
        }

        #endregion
    }

    #region Data Models

    public class TestRunResult
    {
        public string ProjectPath { get; set; }
        public TestFramework TestFramework { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public List<TestFailure> FailedTests { get; set; } = new List<TestFailure>();
        public int SkippedTests { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum TestFramework
    {
        Unknown,
        XUnit,
        NUnit,
        MSTest,
        Jest,
        Pytest,
        JUnit
    }

    #endregion
}
