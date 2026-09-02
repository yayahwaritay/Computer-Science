using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CompSci.Infrastructure.Reports;

/// <summary>
/// Renders the compiled internship grade PDF: one bordered table per program, columns
/// Student Name / Student ID / Evaluation Score / Report Score / Grade — mirroring
/// CourseAllocationPdfBuilder's per-program section layout.
/// </summary>
public class InternshipCompiledPdfBuilder : IInternshipCompiledPdfBuilder
{
    private static readonly string[] Headers = { "Student Name", "Student ID", "Evaluation Score", "Report Score", "Grade" };
    private static readonly double[] ColumnWidths = { 180, 90, 100, 90, 75 };
    private const double MarginLeft = 30;
    private const double MarginTop = 40;
    private const double RowHeight = 20;

    public byte[] Build(IEnumerable<CompiledGradeReport> reports)
    {
        var tableWidth = ColumnWidths.Sum();

        var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
        var programFont = new XFont("Arial", 12, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9, XFontStyle.Regular);

        using var document = new PdfDocument();
        PdfPage page = null!;
        XGraphics gfx = null!;
        double y = 0;
        var isFirstPage = true;

        void NewPage()
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = MarginTop;

            if (isFirstPage)
            {
                gfx.DrawString("Internship Evaluation - Compiled Grades", titleFont, XBrushes.Black,
                    new XRect(MarginLeft, y, tableWidth, RowHeight), XStringFormats.TopCenter);
                y += RowHeight + 10;
                isFirstPage = false;
            }
        }

        void EnsureSpace(double needed)
        {
            if (y + needed > page.Height - MarginTop)
                NewPage();
        }

        void DrawGridRow(string[] cells, XFont font)
        {
            double x = MarginLeft;
            for (var i = 0; i < cells.Length; i++)
            {
                gfx.DrawRectangle(XPens.Black, x, y, ColumnWidths[i], RowHeight);
                gfx.DrawString(cells[i], font, XBrushes.Black,
                    new XRect(x + 3, y + 2, ColumnWidths[i] - 6, RowHeight - 4), XStringFormats.TopLeft);
                x += ColumnWidths[i];
            }
            y += RowHeight;
        }

        NewPage();

        foreach (var report in reports)
        {
            EnsureSpace(RowHeight * 3);

            gfx.DrawString(report.ProgramName, programFont, XBrushes.Black,
                new XRect(MarginLeft, y, tableWidth, RowHeight), XStringFormats.TopCenter);
            y += RowHeight + 4;

            DrawGridRow(Headers, headerFont);

            foreach (var row in report.Rows)
            {
                EnsureSpace(RowHeight);
                DrawGridRow(new[]
                {
                    row.StudentFullName,
                    row.StudentIdNumber,
                    row.EvaluationScore.ToString("0.00"),
                    row.ReportScore?.ToString("0.00") ?? "-",
                    row.Grade ?? "-"
                }, bodyFont);
            }

            y += 14;
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }
}
