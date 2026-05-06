namespace Filtering.Net;

/// <summary>Centralised page/pageSize bounds-checking called by emitted filter validators.</summary>
public static class PageValidation
{
    /// <summary>Returns <see cref="FilterValidationResult.Success"/> when both arguments are null or within bounds; otherwise one error per failing bound.</summary>
    public static FilterValidationResult Validate(int? page, int? pageSize, int maxPageSize)
    {
        if (page is null && pageSize is null) return FilterValidationResult.Success;
        var errors = new List<FilterValidationError>();
        if (page is int requestedPage && requestedPage < 1)
        {
            errors.Add(new FilterValidationError(
                "page",
                FilterValidationCode.PageInvalid,
                $"page must be 1 or greater (was {requestedPage})."));
        }
        if (pageSize is int requestedPageSize)
        {
            if (requestedPageSize < 1)
            {
                errors.Add(new FilterValidationError(
                    "pageSize",
                    FilterValidationCode.PageSizeInvalid,
                    $"pageSize must be 1 or greater (was {requestedPageSize})."));
            }
            else if (requestedPageSize > maxPageSize)
            {
                errors.Add(new FilterValidationError(
                    "pageSize",
                    FilterValidationCode.PageSizeTooLarge,
                    $"pageSize {requestedPageSize} exceeds the configured maximum of {maxPageSize}."));
            }
        }
        return errors.Count == 0
            ? FilterValidationResult.Success
            : new FilterValidationResult(errors);
    }
}
