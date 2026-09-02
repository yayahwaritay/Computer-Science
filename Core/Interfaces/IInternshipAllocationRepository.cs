using CompSci.Core.Entities;
using CompSci.Core.Enums;

namespace CompSci.Core.Interfaces;

public interface IInternshipAllocationRepository : IGenericRepository<InternshipAllocation>
{
    Task<InternshipAllocation?> GetForStudentPeriodAsync(Guid studentId, string academicYear, Semester semester);
}
