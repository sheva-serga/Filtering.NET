using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

public class ProfileBridgeEmitterTests
{
    [Fact]
    public void Emit_ProfileNameWithTypeArguments_EscapesTheDocCommentXml()
    {
        // Arrange — a generic profile's display name carries angle brackets, which would make the
        // doc comment malformed XML (CS1570) in a consumer that generates a documentation file.
        var bridge = new ProfileBridgeModel(
            ProfileFullName: "Sample.TextProfile<Sample.Unit>",
            ProfileName: "TextProfile",
            BridgeClassName: "Sample_TextProfile_Sample_Unit_Profile",
            ColumnTypeFqn: "string",
            BaseProfileReference: null,
            Operators: new EquatableList<ProfileBridgeOperatorModel>());

        // Act
        var emittedSource = ProfileBridgeEmitter.Emit([bridge]);

        // Assert
        var docCommentLine = emittedSource
            .Split('\n')
            .Single(line => line.Contains("<summary>", StringComparison.Ordinal));
        docCommentLine.Should().Contain("Sample.TextProfile&lt;Sample.Unit&gt;");
        docCommentLine.Should().NotContain("<Sample.Unit>");
    }

    [Fact]
    public void Emit_BridgeWithBaseProfile_UsesExtendWithOverrides()
    {
        // Arrange — a derived profile may re-declare an inherited operator; Extend rejects that and
        // would throw from the generated static field initialiser.
        var bridge = new ProfileBridgeModel(
            ProfileFullName: "Sample.CiStringFilter",
            ProfileName: "CiStringFilter",
            BridgeClassName: "Sample_CiStringFilterProfile",
            ColumnTypeFqn: "string",
            BaseProfileReference: "global::Filtering.Net.StringFilter.Profile",
            Operators: new EquatableList<ProfileBridgeOperatorModel>(
            [
                new ProfileBridgeOperatorModel("contains", "global::Filtering.Net.FilterOperator.Value<string, string>(\"contains\", global::Sample.CiStringFilter.Contains)"),
            ]));

        // Act
        var emittedSource = ProfileBridgeEmitter.Emit([bridge]);

        // Assert
        emittedSource.Should().Contain("global::Filtering.Net.StringFilter.Profile.ExtendWithOverrides(\"CiStringFilter\"");
    }
}
