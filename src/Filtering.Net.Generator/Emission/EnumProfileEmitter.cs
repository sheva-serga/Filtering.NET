namespace Filtering.Net.Generator;

internal static class EnumProfileEmitter
{
    public const string GeneratedNamespace = "Filtering.Net.Generated";

    public static string Emit(EnumProfileDescriptor descriptor) =>
        ScribanRuntime.Render("EnumProfile", BuildView(descriptor));

    internal static EnumProfileView BuildView(EnumProfileDescriptor descriptor) =>
        new(
            GeneratedNamespace: GeneratedNamespace,
            EnumFullName: "global::" + descriptor.EnumFullName,
            ClassName: descriptor.ClassName);
}
