namespace Filtering.Net.Generator;

internal static class ValidatePageEmitter
{
    public static string Emit(FilterClassModel model)
    {
        _ = model;
        return ScribanRuntime.Render("ValidatePage", new ValidatePageView());
    }
}
