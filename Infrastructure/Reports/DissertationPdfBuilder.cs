using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CompSci.Infrastructure.Reports;

/// <summary>
/// Renders the compiled dissertation PDF export using PdfSharpCore (MIT-licensed, no revenue
/// restrictions). Intentionally limited to Student name, Student ID, Program, Topic, Academic year.
/// </summary>
public class DissertationPdfBuilder : IDissertationPdfBuilder
{
    private static readonly string[] Headers = { "Student Name", "Student ID", "Program", "Topic", "Academic Year" };
    private static readonly double[] ColumnWidths = { 130, 80, 110, 165, 75 };
    private const double MarginLeft = 30;
    private const double MarginTop = 40;
    private const double RowHeight = 20;

    public byte[] Build(IEnumerable<DissertationExportRow> rows)
    {
        using var document = new PdfDocument();
        var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9, XFontStyle.Regular);

        PdfPage page = null!;
        XGraphics gfx = null!;
        double y = 0;

        void DrawRow(string[] cells, XFont font)
        {
            double x = MarginLeft;
            for (var i = 0; i < cells.Length; i++)
            {
                gfx.DrawString(cells[i], font, XBrushes.Black,
                    new XRect(x, y, ColumnWidths[i], RowHeight), XStringFormats.TopLeft);
                x += ColumnWidths[i];
            }
            y += RowHeight;
        }

        void StartPage()
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = MarginTop;
            gfx.DrawString("Dissertation Records", titleFont, XBrushes.Black, new XPoint(MarginLeft, y));
            y += 25;
            DrawRow(Headers, headerFont);
        }

        StartPage();

        foreach (var row in rows)
        {
            if (y + RowHeight > page.Height - MarginTop)
                StartPage();

            DrawRow(new[] { row.StudentName, row.StudentId, row.Program, row.Topic, row.AcademicYear }, bodyFont);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }
}
