using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Host organization/company accounts, registered by Admin/Lecturer from the email the
/// organization sent in. An organization's login only ever reaches the internship evaluation
/// endpoints (see InternshipEvaluationsController) - see role check there and in AuthService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// Register an organization's login (email + default password, expires in 2 weeks)
    /// </summary>
    [HttpPost]
    [LogActivity("Organization", "Create")]
    public async Task<IActionResult> Register([FromBody] OrganizationRegisterRequest request)
    {
        var errors = OrganizationValidator.ValidateRegister(request);
        if (errors.Any())
            return BadRequest(ApiResponse<OrganizationResponse>.FailResponse("Validation failed.", errors));

        var result = await _organizationService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<OrganizationResponse>.SuccessResponse(result, "Organization registered successfully."));
    }

    /// <summary>
    /// Get all registered organizations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _organizationService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<OrganizationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get an organization by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _organizationService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<OrganizationResponse>.FailResponse($"Organization with ID {id} not found."));

        return Ok(ApiResponse<OrganizationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Generate a new default password and 2-week expiry window for an organization, e.g. after
    /// its credentials lapsed
    /// </summary>
    [HttpPost("{id}/reissue-credentials")]
    [LogActivity("Organization", "Update")]
    public async Task<IActionResult> ReissueCredentials(Guid id)
    {
        var result = await _organizationService.ReissueCredentialsAsync(id);
        return Ok(ApiResponse<OrganizationResponse>.SuccessResponse(result, "New credentials issued and emailed to the organization."));
    }

    /// <summary>
    /// Delete an organization's account (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("Organization", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _organizationService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Organization deleted successfully."));
    }
}
