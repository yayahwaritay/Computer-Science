using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Student self-submission of the dissertation/final-year-project write-up (as distinct from the
/// Admin/Lecturer-managed official Dissertation record at /api/dissertations). Access is scoped via
/// DissertationAllocation - see StudentSubmissionsControllerBase for the full behavior shared with
/// InternshipReportsController.
/// </summary>
[Route("api/[controller]")]
public class DissertationSubmissionsController : StudentSubmissionsControllerBase
{
    public DissertationSubmissionsController(IStudentSubmissionService submissionService) : base(submissionService) { }

    protected override SubmissionType Type => SubmissionType.Dissertation;
    protected override string UploadedMessage => "Dissertation/project write-up submitted successfully.";
    protected override string NotFoundLabel => "dissertation/project write-up";
}
