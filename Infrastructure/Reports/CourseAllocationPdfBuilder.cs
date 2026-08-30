using System.Text.RegularExpressions;
using CompSci.Core.DTOs;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CompSci.Infrastructure.Reports;

/// <summary>
/// Renders the compiled course allocation PDF in the university's standard layout: a bordered table
/// per program, split into year-of-study sections with a SUB-TOTAL credit-hour row — matching the
/// historical "&lt;Academic Year&gt; &lt;Semester&gt; Semester Allocation" documents this replaces
/// (e.g. "2021_2022 Second Semester Allocation").
/// </summary>
public class CourseAllocationPdfBuilder : ICourseAllocationPdfBuilder
{
    private static readonly string[] Headers = { "Course Code", "Course Description", "Credit Hrs", "Staff" };
    private static readonly double[] ColumnWidths = { 85, 260, 60, 130 };
    private const double MarginLeft = 30;
    private const double MarginTop = 40;
    private const double RowHeight = 20;

    public byte[] Build(string academicYear, Semester semester, IEnumerable<CourseAllocationResponse> allocations)
    {
        var tableWidth = ColumnWidths.Sum();

        var titleFont = new XFont("Arial", 13, XFontStyle.Bold);
        var programFont = new XFont("Arial", 12, XFontStyle.Bold);
        var yearFont = new XFont("Arial", 10, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 9, XFontStyle.Regular);
        var subtotalFont = new XFont("Arial", 9, XFontStyle.Bold);

        using var document = new PdfDocument();
        PdfPage page = null!;
        XGraphics gfx = null!;
        double y = 0;

        void NewPage()
        {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = MarginTop;
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

        void DrawSpanRow(string text, XFont font)
        {
            gfx.DrawRectangle(XPens.Black, MarginLeft, y, tableWidth, RowHeight);
            gfx.DrawString(text, font, XBrushes.Black,
                new XRect(MarginLeft, y + 2, tableWidth, RowHeight - 4), XStringFormats.TopCenter);
            y += RowHeight;
        }

        NewPage();

        var title = $"{semester.ToString().ToUpperInvariant()} SEMESTER COURSE ALLOCATION -{ShortenAcademicYear(academicYear)}";
        gfx.DrawString(title, titleFont, XBrushes.Black,
            new XRect(MarginLeft, y, tableWidth, RowHeight), XStringFormats.TopCenter);
        y += RowHeight + 10;

        var programs = allocations
            .GroupBy(a => a.ProgramName)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var programGroup in programs)
        {
            EnsureSpace(RowHeight * 3);

            gfx.DrawString(programGroup.Key, programFont, XBrushes.Black,
                new XRect(MarginLeft, y, tableWidth, RowHeight), XStringFormats.TopCenter);
            y += RowHeight + 4;

            DrawGridRow(Headers, headerFont);

            var years = programGroup.GroupBy(a => a.YearOfStudy).OrderBy(g => g.Key);
            foreach (var yearGroup in years)
            {
                EnsureSpace(RowHeight * 2);
                DrawSpanRow(YearLabel(yearGroup.Key), yearFont);

                var subtotal = 0;
                foreach (var row in yearGroup.OrderBy(a => a.CourseCode, StringComparer.OrdinalIgnoreCase))
                {
                    EnsureSpace(RowHeight);
                    DrawGridRow(new[] { row.CourseCode, row.CourseDescription, row.CreditHours, row.StaffName }, bodyFont);
                    subtotal += ParseLeadingInt(row.CreditHours);
                }

                EnsureSpace(RowHeight);
                DrawGridRow(new[] { "SUB-TOTAL", string.Empty, subtotal.ToString(), string.Empty }, subtotalFont);
            }

            y += 14;
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static string YearLabel(int yearOfStudy) => yearOfStudy switch
    {
        1 => "FIRST YEAR",
        2 => "SECOND YEAR",
        3 => "THIRD YEAR",
        4 => "FOURTH YEAR",
        5 => "FIFTH YEAR",
        6 => "SIXTH YEAR",
        _ => $"YEAR {yearOfStudy}"
    };

    private static int ParseLeadingInt(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    /// <summary>
    /// "2021/2022" -> "2021/22", matching the historical documents' shortened title year. Falls
    /// back to the raw value if it isn't in the expected "YYYY/YYYY" shape.
    /// </summary>
    private static string ShortenAcademicYear(string academicYear)
    {
        var parts = (academicYear ?? string.Empty).Split('/');
        return parts.Length == 2 && parts[1].Length == 4
            ? $"{parts[0]}/{parts[1][2..]}"
            : academicYear ?? string.Empty;
    }
}
