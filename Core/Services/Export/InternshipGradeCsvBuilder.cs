using System.Text;
using CompSci.Core.DTOs;

namespace CompSci.Core.Services.Export;

/// <summary>
/// Builds the compiled internship grade CSV export: Program, Student Name, Student ID,
/// Evaluation Score, Report Score, Grade — one row per student, grouped by program.
/// </summary>
public static class InternshipGradeCsvBuilder
{
    public static byte[] Build(IEnumerable<CompiledGradeReport> reports)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Program,Student Name,Student ID,Evaluation Score,Report Score,Grade");

        foreach (var report in reports)
        {
            foreach (var row in report.Rows)
            {
                sb.AppendLine(string.Join(",",
                    Escape(report.ProgramName),
                    Escape(row.StudentFullName),
                    Escape(row.StudentIdNumber),
                    row.EvaluationScore.ToString("0.00"),
                    row.ReportScore?.ToString("0.00") ?? string.Empty,
                    Escape(row.Grade ?? string.Empty)));
            }
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
