namespace Filtering.Net.Generator;

/// <summary>
/// Emits <c>Validate(int? page, int? pageSize)</c>: bounds-check both arguments and surface
/// <see cref="FilterValidationCode.PageInvalid"/>, <see cref="FilterValidationCode.PageSizeInvalid"/>,
/// or <see cref="FilterValidationCode.PageSizeTooLarge"/> as appropriate.
/// </summary>
internal static class ValidatePageEmitter
{
    public static string Emit(FilterClassModel model)
    {
        _ = model;
        return ScribanRuntime.Render("ValidatePage", new ValidatePageView());
    }
}
