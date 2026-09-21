using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests that a diagnostic routed through <see cref="DiagnosticInfo"/> reaches the compiler
/// with the registered descriptor's metadata, not a stand-in rebuilt from primitives.</summary>
public class DiagnosticInfoTests
{
    [Fact]
    public void ToDiagnostic_ForAnExtractorReportedRule_CarriesTheRegisteredHelpLinkAndDescription()
    {
        // Arrange — FN0001 is reported through DiagnosticInfo.From, unlike FN1003/FN1004/FN1008.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var diagnostic = DiagnosticTestHelpers.GetDiagnostics(source).First(d => d.Id == "FN0001");

        // Assert
        diagnostic.Descriptor.HelpLinkUri.Should().Be(DiagnosticDescriptors.DuplicateMapping.HelpLinkUri);
        diagnostic.Descriptor.HelpLinkUri.Should().NotBeNullOrEmpty();
        diagnostic.Descriptor.Description.ToString().Should()
            .Be(DiagnosticDescriptors.DuplicateMapping.Description.ToString());
        diagnostic.Descriptor.Description.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToDiagnostic_ForEveryRegisteredDescriptor_ResolvesBackToThatDescriptor()
    {
        // Arrange
        var descriptors = typeof(DiagnosticDescriptors)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.GetValue(null))
            .OfType<Microsoft.CodeAnalysis.DiagnosticDescriptor>()
            .ToList();

        // Act
        var resolved = descriptors.Select(descriptor => DiagnosticDescriptorRegistry.ById(descriptor.Id)).ToList();

        // Assert
        descriptors.Should().NotBeEmpty();
        resolved.Should().BeEquivalentTo(descriptors);
        descriptors.Select(descriptor => descriptor.Id).Should().OnlyHaveUniqueItems();
    }
}
