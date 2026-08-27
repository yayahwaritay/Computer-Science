using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

/// <summary>
/// Builds the compiled dissertation PDF export. Intentionally limited to
/// Student name, Student ID, Program, Topic, Academic year — no other fields.
/// </summary>
public interface IDissertationPdfBuilder
{
    byte[] Build(IEnumerable<DissertationExportRow> rows);
}
