using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Export;

namespace CompSci.Core.Services;

public class InternshipEvaluationService : IInternshipEvaluationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInternshipCompiledPdfBuilder _pdfBuilder;

    public InternshipEvaluationService(IUnitOfWork unitOfWork, IInternshipCompiledPdfBuilder pdfBuilder)
    {
        _unitOfWork = unitOfWork;
        _pdfBuilder = pdfBuilder;
    }

    public async Task<InternshipEvaluationResponse> CreateAsync(InternshipEvaluationRequest request, Guid organizationUserId)
    {
        var student = await _unitOfWork.Students.GetByStudentIdAsync(request.StudentId)
            ?? throw new InvalidOperationException($"No student found with Student ID '{request.StudentId}'.");

        var rawTotal =
            request.RapportWithSupervisor + request.RapportWithStaffAndClient + request.CommunicatesWell +
            request.SeeksNewKnowledge + request.ShowsInitiative + request.ManagesTimeWell +
            request.ProducesAccurateReports + request.DemonstratesAdequateKnowledge +
            request.DressesProfessionally + request.IsPunctual +
            request.IsDependable + request.AcceptsConstructiveCriticism + request.DemonstratesEnthusiasm;

        var allocation = await _unitOfWork.InternshipAllocations.GetForStudentPeriodAsync(
            student.Id, request.AcademicYear, request.Semester);

        // An Organization may only evaluate a student who has actually been placed with it
        // (via InternshipAllocation) for that exact academic year + semester - this is the
        // server-side enforcement behind "Organization only sees/evaluates its own students".
        if (allocation == null || allocation.OrganizationUserId != organizationUserId)
            throw new InvalidOperationException(
                $"Student '{request.StudentId}' is not placed with your organization for {request.AcademicYear} {request.Semester} semester. " +
                "Ask an administrator to set up the internship placement first.");

        var evaluation = new InternshipEvaluation
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            StudentFullName = $"{student.FirstName} {student.LastName}",
            StudentIdNumber = student.StudentId,
            ProgramName = student.ProgramName,
            Year = student.Year,

            OrganizationUserId = organizationUserId,
            CompanySupervisorName = request.CompanySupervisorName,
            CompanySupervisorPhone = request.CompanySupervisorPhone,
            AcademicYear = request.AcademicYear,
            Semester = request.Semester,
            InternshipStartDate = request.InternshipStartDate,
            InternshipMonths = request.InternshipMonths,

            RapportWithSupervisor = request.RapportWithSupervisor,
            RapportWithStaffAndClient = request.RapportWithStaffAndClient,
            CommunicatesWell = request.CommunicatesWell,
            SeeksNewKnowledge = request.SeeksNewKnowledge,
            ShowsInitiative = request.ShowsInitiative,
            ManagesTimeWell = request.ManagesTimeWell,
            ProducesAccurateReports = request.ProducesAccurateReports,
            DemonstratesAdequateKnowledge = request.DemonstratesAdequateKnowledge,
            DressesProfessionally = request.DressesProfessionally,
            IsPunctual = request.IsPunctual,
            IsDependable = request.IsDependable,
            AcceptsConstructiveCriticism = request.AcceptsConstructiveCriticism,
            DemonstratesEnthusiasm = request.DemonstratesEnthusiasm,

            OtherRatingLabel = request.OtherRatingLabel,
            OtherRatingScore = request.OtherRatingScore,

            Comments = request.Comments,
            SupervisorSignatureName = request.SupervisorSignatureName,
            CertificationDate = request.CertificationDate,

            RawRatingTotal = rawTotal,
            EvaluationScore = GradeCalculator.EvaluationScoreFromRawTotal(rawTotal),
            AllocatedLecturerUserId = allocation.LecturerUserId,

            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.InternshipEvaluations.AddAsync(evaluation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(evaluation);
    }

    public async Task<InternshipEvaluationResponse?> GetByIdAsync(Guid id, InternshipEvaluationAccessContext access)
    {
        var evaluation = await _unitOfWork.InternshipEvaluations.GetByIdAsync(id);
        if (evaluation == null || !CanAccess(evaluation, access))
            return null;

        return await MapToResponseAsync(evaluation);
    }

    public async Task<IEnumerable<InternshipEvaluationResponse>> GetAllAsync(InternshipEvaluationAccessContext access)
    {
        var evaluations = await ScopedAsync(access);
        return await MapToResponsesAsync(evaluations);
    }

    public async Task<IEnumerable<InternshipEvaluationResponse>> GetByStudentIdAsync(string studentId, InternshipEvaluationAccessContext access)
    {
        var student = await _unitOfWork.Students.GetByStudentIdAsync(studentId);
        if (student == null)
            return Enumerable.Empty<InternshipEvaluationResponse>();

        var evaluations = await _unitOfWork.InternshipEvaluations.GetByStudentIdAsync(student.Id);
        evaluations = evaluations.Where(e => CanAccess(e, access));

        return await MapToResponsesAsync(evaluations);
    }

    public async Task<InternshipEvaluationResponse> SubmitReportGradeAsync(Guid id, ReportGradeRequest request, InternshipEvaluationAccessContext access)
    {
        var evaluation = await _unitOfWork.InternshipEvaluations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Internship evaluation with ID {id} not found.");

        var canGrade = access.IsAdmin || (access.IsLecturer && evaluation.AllocatedLecturerUserId == access.UserId);
        if (!canGrade)
            throw new UnauthorizedAccessException("Only the lecturer allocated to this student's internship report (or an Admin) may grade it.");

        evaluation.ReportScore = request.ReportScore;
        evaluation.ReportGradedByUserId = access.UserId;
        evaluation.ReportGradedAt = DateTime.UtcNow;
        evaluation.TotalScore = evaluation.EvaluationScore + request.ReportScore;
        evaluation.Grade = GradeCalculator.GradeFromTotal(evaluation.TotalScore.Value);
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.InternshipEvaluations.UpdateAsync(evaluation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(evaluation);
    }

    public async Task<IEnumerable<CompiledGradeReport>> GetCompiledAsync(InternshipEvaluationFilter filter)
    {
        var evaluations = await FilterAsync(filter);
        return BuildCompiledReports(evaluations);
    }

    public async Task<byte[]> ExportCompiledCsvAsync(InternshipEvaluationFilter filter)
    {
        var reports = await GetCompiledAsync(filter);
        return InternshipGradeCsvBuilder.Build(reports);
    }

    public async Task<byte[]> ExportCompiledPdfAsync(InternshipEvaluationFilter filter)
    {
        var reports = await GetCompiledAsync(filter);
        return _pdfBuilder.Build(reports);
    }

    private async Task<IEnumerable<InternshipEvaluation>> ScopedAsync(InternshipEvaluationAccessContext access)
    {
        if (access.IsAdmin)
            return await _unitOfWork.InternshipEvaluations.GetAllAsync();

        if (access.IsOrganization)
            return await _unitOfWork.InternshipEvaluations.GetByOrganizationAsync(access.UserId);

        if (access.IsLecturer)
            return await _unitOfWork.InternshipEvaluations.GetByAllocatedLecturerAsync(access.UserId);

        return Enumerable.Empty<InternshipEvaluation>();
    }

    private async Task<IEnumerable<InternshipEvaluation>> FilterAsync(InternshipEvaluationFilter filter)
    {
        var evaluations = await _unitOfWork.InternshipEvaluations.GetAllAsync();

        return evaluations.Where(e =>
            (string.IsNullOrWhiteSpace(filter.ProgramName) || e.ProgramName.Contains(filter.ProgramName, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(filter.AcademicYear) || e.AcademicYear == filter.AcademicYear) &&
            (filter.Semester == null || e.Semester == filter.Semester));
    }

    private static IEnumerable<CompiledGradeReport> BuildCompiledReports(IEnumerable<InternshipEvaluation> evaluations)
    {
        return evaluations
            .GroupBy(e => e.ProgramName)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CompiledGradeReport
            {
                ProgramName = g.Key,
                Rows = g
                    .OrderBy(e => e.StudentFullName, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new CompiledGradeRow
                    {
                        StudentFullName = e.StudentFullName,
                        StudentIdNumber = e.StudentIdNumber,
                        EvaluationScore = e.EvaluationScore,
                        ReportScore = e.ReportScore,
                        Grade = e.Grade
                    })
                    .ToList()
            });
    }

    /// <summary>
    /// Admin sees/manages every record. Organization is scoped to records it submitted.
    /// Lecturer is scoped to records allocated to it for report grading.
    /// </summary>
    private static bool CanAccess(InternshipEvaluation evaluation, InternshipEvaluationAccessContext access)
    {
        if (access.IsAdmin)
            return true;
        if (access.IsOrganization)
            return evaluation.OrganizationUserId == access.UserId;
        if (access.IsLecturer)
            return evaluation.AllocatedLecturerUserId == access.UserId;

        return false;
    }

    private async Task<InternshipEvaluationResponse> MapToResponseAsync(InternshipEvaluation evaluation)
    {
        var organization = await _unitOfWork.Organizations.GetByUserIdAsync(evaluation.OrganizationUserId);
        User? lecturer = evaluation.AllocatedLecturerUserId.HasValue
            ? await _unitOfWork.Users.GetByIdAsync(evaluation.AllocatedLecturerUserId.Value)
            : null;

        return MapToResponse(evaluation, organization?.Name, lecturer?.Username);
    }

    private async Task<IEnumerable<InternshipEvaluationResponse>> MapToResponsesAsync(IEnumerable<InternshipEvaluation> evaluations)
    {
        var list = evaluations.ToList();
        var organizations = await _unitOfWork.Organizations.GetAllAsync();
        var orgsByUserId = organizations.ToDictionary(o => o.UserId, o => o);
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(e =>
        {
            orgsByUserId.TryGetValue(e.OrganizationUserId, out var organization);
            User? lecturer = null;
            if (e.AllocatedLecturerUserId.HasValue)
                usersById.TryGetValue(e.AllocatedLecturerUserId.Value, out lecturer);

            return MapToResponse(e, organization?.Name, lecturer?.Username);
        });
    }

    private static InternshipEvaluationResponse MapToResponse(InternshipEvaluation e, string? organizationName, string? lecturerUsername)
    {
        return new InternshipEvaluationResponse
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentFullName = e.StudentFullName,
            StudentIdNumber = e.StudentIdNumber,
            ProgramName = e.ProgramName,
            Year = e.Year,

            OrganizationUserId = e.OrganizationUserId,
            OrganizationName = organizationName,
            CompanySupervisorName = e.CompanySupervisorName,
            CompanySupervisorPhone = e.CompanySupervisorPhone,
            AcademicYear = e.AcademicYear,
            Semester = e.Semester,
            InternshipStartDate = e.InternshipStartDate,
            InternshipMonths = e.InternshipMonths,

            RapportWithSupervisor = e.RapportWithSupervisor,
            RapportWithStaffAndClient = e.RapportWithStaffAndClient,
            CommunicatesWell = e.CommunicatesWell,
            SeeksNewKnowledge = e.SeeksNewKnowledge,
            ShowsInitiative = e.ShowsInitiative,
            ManagesTimeWell = e.ManagesTimeWell,
            ProducesAccurateReports = e.ProducesAccurateReports,
            DemonstratesAdequateKnowledge = e.DemonstratesAdequateKnowledge,
            DressesProfessionally = e.DressesProfessionally,
            IsPunctual = e.IsPunctual,
            IsDependable = e.IsDependable,
            AcceptsConstructiveCriticism = e.AcceptsConstructiveCriticism,
            DemonstratesEnthusiasm = e.DemonstratesEnthusiasm,

            OtherRatingLabel = e.OtherRatingLabel,
            OtherRatingScore = e.OtherRatingScore,

            Comments = e.Comments,
            SupervisorSignatureName = e.SupervisorSignatureName,
            CertificationDate = e.CertificationDate,

            RawRatingTotal = e.RawRatingTotal,
            EvaluationScore = e.EvaluationScore,

            AllocatedLecturerUserId = e.AllocatedLecturerUserId,
            AllocatedLecturerUsername = lecturerUsername,
            ReportScore = e.ReportScore,
            ReportGradedByUserId = e.ReportGradedByUserId,
            ReportGradedAt = e.ReportGradedAt,

            TotalScore = e.TotalScore,
            Grade = e.Grade,

            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
