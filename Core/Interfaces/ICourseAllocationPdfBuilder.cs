using CompSci.Core.DTOs;
using CompSci.Core.Enums;

namespace CompSci.Core.Interfaces;

public interface ICourseAllocationPdfBuilder
{
    /// <summary>
    /// Renders a compiled allocation document: one table per program (grouped from the rows given),
    /// each split into year-of-study sections with a SUB-TOTAL credit-hour row, matching the
    /// university's standard "&lt;Semester&gt; Semester Course Allocation" layout.
    /// </summary>
    byte[] Build(string academicYear, Semester semester, IEnumerable<CourseAllocationResponse> allocations);
}
