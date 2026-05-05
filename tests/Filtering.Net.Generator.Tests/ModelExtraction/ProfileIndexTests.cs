using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.ModelExtraction;

public class ProfileIndexTests
{
    [Fact]
    public void Lookup_NoKeyInIndex_ReturnsEmpty()
    {
        // Arrange
        var profileIndex = new ProfileIndex(new Dictionary<string, List<string>>());

        // Act
        var lookupResult = profileIndex.Lookup("System.Int32");

        // Assert
        lookupResult.Should().BeEmpty();
    }

    [Fact]
    public void Lookup_OneMatchingProfile_ReturnsSingleProfileFullName()
    {
        // Arrange
        var entries = new Dictionary<string, List<string>>
        {
            ["System.Int32"] = ["Filtering.Net.Int32Filter"],
        };
        var profileIndex = new ProfileIndex(entries);

        // Act
        var lookupResult = profileIndex.Lookup("System.Int32");

        // Assert
        lookupResult.Should().Equal("Filtering.Net.Int32Filter");
    }

    [Fact]
    public void Lookup_MultipleMatchingProfiles_ReturnsAllInRegistrationOrder()
    {
        // Arrange
        var entries = new Dictionary<string, List<string>>
        {
            ["System.Int32"] = ["Filtering.Net.Int32Filter", "Sample.MyIntFilter"],
        };
        var profileIndex = new ProfileIndex(entries);

        // Act
        var lookupResult = profileIndex.Lookup("System.Int32");

        // Assert
        lookupResult.Should().Equal("Filtering.Net.Int32Filter", "Sample.MyIntFilter");
    }
}
