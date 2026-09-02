using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

/// <summary>
/// Builds the compiled internship grade PDF export — one section per program, each row
/// (Student Name, Student ID, Evaluation Score, Report Score, Grade).
/// </summary>
public interface IInternshipCompiledPdfBuilder
{
    byte[] Build(IEnumerable<CompiledGradeReport> reports);
}
