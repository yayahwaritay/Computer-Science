using System.Text;
using CompSci.Core.DTOs;

namespace CompSci.Core.Services.Export;

/// <summary>
/// Builds the compiled dissertation CSV export. Intentionally limited to
/// Student name, Student ID, Program, Topic, Academic year — no other fields.
/// </summary>
public static class DissertationCsvBuilder
{
    public static byte[] Build(IEnumerable<DissertationExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Student Name,Student ID,Program,Dissertation/Project Topic,Academic Year");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(row.StudentName),
                Escape(row.StudentId),
                Escape(row.Program),
                Escape(row.Topic),
                Escape(row.AcademicYear)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
