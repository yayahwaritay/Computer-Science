using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IInternshipEvaluationService
{
    Task<InternshipEvaluationResponse> CreateAsync(InternshipEvaluationRequest request, Guid organizationUserId);
    Task<InternshipEvaluationResponse?> GetByIdAsync(Guid id, InternshipEvaluationAccessContext access);
    Task<IEnumerable<InternshipEvaluationResponse>> GetAllAsync(InternshipEvaluationAccessContext access);
    /// <summary>studentId is the human-readable Student ID (e.g. "24807"), not the internal database ID.</summary>
    Task<IEnumerable<InternshipEvaluationResponse>> GetByStudentIdAsync(string studentId, InternshipEvaluationAccessContext access);

    /// <summary>
    /// The allocated Lecturer (or Admin) grades the internship report out of 30, which recomputes
    /// TotalScore and Grade.
    /// </summary>
    Task<InternshipEvaluationResponse> SubmitReportGradeAsync(Guid id, ReportGradeRequest request, InternshipEvaluationAccessContext access);

    Task<IEnumerable<CompiledGradeReport>> GetCompiledAsync(InternshipEvaluationFilter filter);
    Task<byte[]> ExportCompiledCsvAsync(InternshipEvaluationFilter filter);
    Task<byte[]> ExportCompiledPdfAsync(InternshipEvaluationFilter filter);
}
