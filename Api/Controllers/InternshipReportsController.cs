using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Student self-submission of the internship report (as distinct from the Admin/Lecturer-managed
/// official Dissertation-style record). Access is scoped via InternshipAllocation - see
/// StudentSubmissionsControllerBase for the full behavior shared with DissertationSubmissionsController.
/// </summary>
[Route("api/[controller]")]
public class InternshipReportsController : StudentSubmissionsControllerBase
{
    public InternshipReportsController(IStudentSubmissionService submissionService) : base(submissionService) { }

    protected override SubmissionType Type => SubmissionType.InternshipReport;
    protected override string UploadedMessage => "Internship report submitted successfully.";
    protected override string NotFoundLabel => "internship report";
}
