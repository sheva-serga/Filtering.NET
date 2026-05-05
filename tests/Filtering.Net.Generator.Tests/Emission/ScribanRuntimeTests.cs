using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

public class ScribanRuntimeTests
{
    [Fact]
    public void Render_KnownTemplate_ReturnsRendered()
    {
        // Arrange
        var view = new SmokeView("world");

        // Act
        var renderedOutput = ScribanRuntime.Render("Smoke", view);

        // Assert
        renderedOutput.Trim().Should().Be("hello world");
    }

    [Fact]
    public void Render_UnknownTemplate_ThrowsFilterEmissionException()
    {
        // Act
        Action renderUnknownTemplate = () => ScribanRuntime.Render("DoesNotExist", new SmokeView("x"));

        // Assert
        renderUnknownTemplate.Should().Throw<FilterEmissionException>()
            .WithMessage("*DoesNotExist*");
    }

    [Fact]
    public void Render_CachesParsedTemplate()
    {
        // Arrange
        var firstView = new SmokeView("a");
        var secondView = new SmokeView("b");

        // Act
        var firstRendered = ScribanRuntime.Render("Smoke", firstView);
        var secondRendered = ScribanRuntime.Render("Smoke", secondView);

        // Assert
        firstRendered.Trim().Should().Be("hello a");
        secondRendered.Trim().Should().Be("hello b");
    }

    private sealed record SmokeView(string Name);
}
