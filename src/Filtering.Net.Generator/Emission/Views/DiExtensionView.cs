namespace Filtering.Net.Generator;

internal sealed record DiExtensionView(
    IReadOnlyList<DiRegistrationView> Registrations);

internal sealed record DiRegistrationView(
    string EntityFullName,
    string FilterClassFullName,
    bool ThreadsSerializerOptions);
