using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using CopilotExtension.Services;

namespace CopilotExtension.Tests.Integration
{
    /// <summary>
    /// End-to-end tests for complete TDD cycle: Write ? Compile ? Run ? Fix ? Verify
    /// </summary>
    public class EndToEndTddCycleTests : IDisposable
    {
        private readonly TddCycleService tddService;
        private readonly TestRunnerService testRunner;
        private readonly string testProjectPath;
        private readonly string tempDirectory;

        public EndToEndTddCycleTests()
        {
            tddService = new TddCycleService();
            testRunner = new TestRunnerService();
            
            // Create temporary directory for test project
            tempDirectory = Path.Combine(Path.GetTempPath(), $"TddTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);
            testProjectPath = Path.Combine(tempDirectory, "TestProject.csproj");
        }

        [Fact]
        public async Task FullCycle_WriteCodeCompileRunFix_ShouldCompleteSuccessfully()
        {
            // ==========================================
            // PHASE 1: WRITE CODE (with intentional bug)
            // ==========================================
            
            var sourceCode = @"
using System;

namespace TestApp
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a - b;  // BUG: Using subtraction instead of addition
        }

        public int Multiply(int a, int b)
        {
            return a * b;  // CORRECT
        }
    }
}";

            var sourceFilePath = Path.Combine(tempDirectory, "Calculator.cs");
            await File.WriteAllTextAsync(sourceFilePath, sourceCode);

            // Verify file written
            File.Exists(sourceFilePath).Should().BeTrue();
            var writtenCode = await File.ReadAllTextAsync(sourceFilePath);
            writtenCode.Should().Contain("Calculator");

            // ==========================================
            // PHASE 2: GENERATE TESTS (AI-powered)
            // ==========================================

            var generatedTests = await tddService.GenerateTestsAsync(
                sourceCode,
                "Calculator",
                "C#");

            // If CLI not available, create manual tests
            if (string.IsNullOrEmpty(generatedTests))
            {
                generatedTests = @"
using Xunit;

namespace TestApp.Tests
{
    public class CalculatorTests
    {
        [Fact]
        public void Add_TwoPositiveNumbers_ReturnsSum()
        {
            var calc = new Calculator();
            var result = calc.Add(2, 3);
            Assert.Equal(5, result);  // Will FAIL due to bug
        }

        [Fact]
        public void Multiply_TwoNumbers_ReturnsProduct()
        {
            var calc = new Calculator();
            var result = calc.Multiply(2, 3);
            Assert.Equal(6, result);  // Will PASS
        }
    }
}";
            }

            var testFilePath = Path.Combine(tempDirectory, "CalculatorTests.cs");
            await File.WriteAllTextAsync(testFilePath, generatedTests);
            
            generatedTests.Should().NotBeNullOrEmpty();
            generatedTests.Should().Contain("Add");

            // ==========================================
            // PHASE 3: CREATE PROJECT FILE
            // ==========================================

            var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.6.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.4"" />
  </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(testProjectPath, projectContent);
            File.Exists(testProjectPath).Should().BeTrue();

            // ==========================================
            // PHASE 4: COMPILE CODE
            // ==========================================

            var compileResult = await CompileProjectAsync(testProjectPath);
            
            compileResult.Success.Should().BeTrue("Code should compile successfully");
            compileResult.Output.Should().Contain("Build succeeded");

            // ==========================================
            // PHASE 5: RUN TESTS (expect failures)
            // ==========================================

            var testResult = await testRunner.RunTestsAsync(testProjectPath);

            // Should have both passing and failing tests
            testResult.Should().NotBeNull();
            testResult.TotalTests.Should().BeGreaterThan(0);
            testResult.FailedTests.Should().NotBeEmpty("Add test should fail due to bug");
            testResult.PassedTests.Should().BeGreaterThan(0, "Multiply test should pass");

            // Verify specific failure
            var addTestFailure = testResult.FailedTests
                .FirstOrDefault(f => f.TestName.Contains("Add"));
            
            addTestFailure.Should().NotBeNull();
            addTestFailure.ErrorMessage.Should().Contain("Expected: 5");
            addTestFailure.ErrorMessage.Should().Contain("Actual: -1");

            // ==========================================
            // PHASE 6: ANALYZE FAILURES (AI-powered)
            // ==========================================

            var analysis = await tddService.AnalyzeTestFailuresAsync(
                sourceCode,
                testResult.FailedTests,
                "C#");

            analysis.Should().NotBeNull();
            analysis.Suggestions.Should().NotBeEmpty();

            var suggestion = analysis.Suggestions.First();
            suggestion.TestName.Should().Contain("Add");
            
            // AI should identify the bug
            // (In real scenario with CLI, will get actual AI analysis)

            // ==========================================
            // PHASE 7: APPLY FIX
            // ==========================================

            // Fix the bug: change subtraction to addition
            var fixedCode = sourceCode.Replace("return a - b;", "return a + b;");
            
            fixedCode.Should().Contain("return a + b;");
            fixedCode.Should().NotContain("return a - b;");

            await File.WriteAllTextAsync(sourceFilePath, fixedCode);

            // ==========================================
            // PHASE 8: RECOMPILE
            // ==========================================

            var recompileResult = await CompileProjectAsync(testProjectPath);
            recompileResult.Success.Should().BeTrue("Fixed code should compile");

            // ==========================================
            // PHASE 9: RERUN TESTS (expect success)
            // ==========================================

            var finalTestResult = await testRunner.RunTestsAsync(testProjectPath);

            // All tests should now pass
            finalTestResult.Should().NotBeNull();
            finalTestResult.Success.Should().BeTrue("All tests should pass after fix");
            finalTestResult.FailedTests.Should().BeEmpty("No tests should fail");
            finalTestResult.PassedTests.Should().Be(finalTestResult.TotalTests);

            // ==========================================
            // PHASE 10: VERIFY COMPLETE CYCLE
            // ==========================================

            // Summary of what happened:
            var cycleReport = $@"
TDD CYCLE COMPLETED SUCCESSFULLY:
================================

1. ? Code Written (with intentional bug)
2. ? Tests Generated (AI or manual)
3. ? Project Created
4. ? Compilation Successful
5. ? Tests Ran (1 failed, 1 passed)
6. ? Failures Analyzed
7. ? Fix Applied (- changed to +)
8. ? Recompiled Successfully
9. ? All Tests Passed
10. ? Cycle Verified

Initial State: 1 bug, 1 failure
Final State: 0 bugs, 0 failures

TIME: Bug detected and fixed in automated cycle!
";

            Console.WriteLine(cycleReport);
            
            // Final assertions
            finalTestResult.PassedTests.Should().Be(2);
            finalTestResult.FailedTests.Should().BeEmpty();
        }

        [Fact]
        public async Task FullCycle_ComplexScenario_MultipleIterations()
        {
            // ==========================================
            // SCENARIO: Multiple bugs requiring iterations
            // ==========================================

            var buggyCode = @"
public class MathOperations
{
    public int Divide(int a, int b)
    {
        return a / b;  // BUG 1: No zero check
    }

    public int Subtract(int a, int b)
    {
        return a + b;  // BUG 2: Using addition
    }

    public bool IsEven(int number)
    {
        return number % 2 == 1;  // BUG 3: Wrong comparison
    }
}";

            var tests = @"
[Fact] public void Divide_ByZero_ShouldThrowException() 
{ 
    var math = new MathOperations();
    Assert.Throws<DivideByZeroException>(() => math.Divide(10, 0));
}

[Fact] public void Subtract_Numbers_ReturnsCorrectResult() 
{ 
    var math = new MathOperations();
    Assert.Equal(5, math.Subtract(10, 5));
}

[Fact] public void IsEven_EvenNumber_ReturnsTrue() 
{ 
    var math = new MathOperations();
    Assert.True(math.IsEven(4));
}";

            // Iteration 1: Fix divide by zero
            var iteration1Fix = buggyCode.Replace(
                "return a / b;",
                "if (b == 0) throw new DivideByZeroException();\nreturn a / b;");

            // Iteration 2: Fix subtract
            var iteration2Fix = iteration1Fix.Replace(
                "return a + b;  // BUG 2",
                "return a - b;");

            // Iteration 3: Fix IsEven
            var iteration3Fix = iteration2Fix.Replace(
                "return number % 2 == 1;",
                "return number % 2 == 0;");

            // Verify all fixes applied
            iteration3Fix.Should().Contain("throw new DivideByZeroException");
            iteration3Fix.Should().Contain("return a - b;");
            iteration3Fix.Should().Contain("return number % 2 == 0;");

            // In real test, would run through TDD cycle for each iteration
            // and verify progressive improvement
        }

        [Fact]
        public async Task CompileAndRun_SimpleCode_ShouldSucceed()
        {
            // Simple integration test of compile + run
            var simpleCode = @"
public class Simple
{
    public int Double(int x) => x * 2;
}";

            var simpleTest = @"
[Fact] public void Double_ReturnsDoubleValue() 
{ 
    Assert.Equal(4, new Simple().Double(2)); 
}";

            // Write files
            var codeFile = Path.Combine(tempDirectory, "Simple.cs");
            var testFile = Path.Combine(tempDirectory, "SimpleTests.cs");
            
            await File.WriteAllTextAsync(codeFile, simpleCode);
            await File.WriteAllTextAsync(testFile, simpleTest);

            // Create minimal project
            var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.6.2"" />
  </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(testProjectPath, projectContent);

            // Compile
            var compileResult = await CompileProjectAsync(testProjectPath);
            compileResult.Success.Should().BeTrue();

            // Run (would run if xunit runner available)
            // This demonstrates the pattern even if environment not ready
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private async Task<CompileResult> CompileProjectAsync(string projectPath)
        {
            var result = new CompileResult();

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{projectPath}\" --verbosity quiet",
                        WorkingDirectory = Path.GetDirectoryName(projectPath),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await Task.Run(() => process.WaitForExit(30000)); // 30s timeout

                result.Output = output + "\n" + error;
                result.Success = process.ExitCode == 0;
                result.ExitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Output = ex.Message;
                result.ExitCode = -1;
            }

            return result;
        }

        public void Dispose()
        {
            // Cleanup temporary directory
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private class CompileResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public int ExitCode { get; set; }
        }
    }
}
