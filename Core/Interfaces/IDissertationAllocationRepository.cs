using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IDissertationAllocationRepository : IGenericRepository<DissertationAllocation>
{
    Task<DissertationAllocation?> GetForStudentYearAsync(Guid studentId, string academicYear);
}
