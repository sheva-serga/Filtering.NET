using Filtering.Net;
using Filtering.Net.EntityFrameworkCore;

using Microsoft.AspNetCore.Mvc;

using UserManagement.WebApi.Data;
using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(AppDbContext dbContext, IFilterDefinition<User> userFilter) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IFilterDefinition<User> _userFilter = userFilter;

    [HttpPost("search")]
    public async Task<ActionResult<PageResult<User>>> SearchAsync(
        [FromBody] FilterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Read-only endpoint: AsNoTracking skips the identity-map entries and original-value
            // snapshots the change tracker would otherwise build for every returned row.
            var pageResult = await _dbContext.Users
                .AsNoTracking()
                .Include(user => user.Department)
                .ApplyPagedAsync(_userFilter, request, cancellationToken);

            return Ok(pageResult);
        }
        catch (FilterValidationException validationException)
        {
            return BadRequest(validationException.Result);
        }
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] FilterRequest request)
    {
        var validationResult = _userFilter.Validate(request);
        return validationResult.IsValid ? Ok(validationResult) : BadRequest(validationResult);
    }

    [HttpPost("export")]
    public async Task<ActionResult<User[]>> ExportAsync(
        [FromBody] FilterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = _userFilter.Validate(request);
            if (!validationResult.IsValid) return BadRequest(validationResult);

            // Read-only endpoint: see the AsNoTracking note in SearchAsync.
            var userQuery = _dbContext.Users.AsNoTracking().Include(user => user.Department);
            var filteredQuery = request.Where is not null
                ? _userFilter.ApplyFilter(userQuery, request.Where)
                : userQuery.AsQueryable();
            var sortedQuery = request.Sort is { Count: > 0 }
                ? _userFilter.ApplySorting(filteredQuery, request.Sort)
                : filteredQuery;
            var allMatches = await sortedQuery.ToArrayAsync(cancellationToken);
            return Ok(allMatches);
        }
        catch (FilterValidationException validationException)
        {
            return BadRequest(validationException.Result);
        }
    }
}
