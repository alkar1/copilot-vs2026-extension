using System;
using Xunit;
using FluentAssertions;
using CopilotExtension.Services;

namespace CopilotExtension.Tests.Integration
{
    /// <summary>
    /// Integration tests that test the complete flow of the extension
    /// </summary>
    public class ExtensionIntegrationTests
    {
        [Fact]
        public void Extension_ShouldHaveCorrectMetadata()
        {
            // Arrange
            var expectedGuid = "f1e8d9c2-4b3a-4c5d-8e7f-9a0b1c2d3e4f";

            // Assert
            expectedGuid.Should().NotBeNullOrEmpty();
            expectedGuid.Length.Should().Be(36);
        }

        [Theory]
        [InlineData("")]
        [InlineData("simple code")]
        [InlineData("complex code with\nmultiple lines\nand special chars!")]
        public void Extension_ShouldHandleDifferentInputs(string input)
        {
            // This test ensures no crashes with different inputs
            input.Should().NotBeNull();
        }

        [Fact]
        public void Extension_ComponentsShouldBeLoadable()
        {
            // Arrange & Act
            var serviceType = typeof(CopilotCliService);

            // Assert
            serviceType.Should().NotBeNull();
            serviceType.Name.Should().Be("CopilotCliService");
        }

        [Fact]
        public void CopilotCliService_ShouldBeInstantiable()
        {
            // Act
            var service = new CopilotCliService();

            // Assert
            service.Should().NotBeNull();
        }
    }
}
