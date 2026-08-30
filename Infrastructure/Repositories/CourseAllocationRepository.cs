using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;

namespace CompSci.Infrastructure.Repositories;

public class CourseAllocationRepository : GenericRepository<CourseAllocation>, ICourseAllocationRepository
{
    public CourseAllocationRepository(AppDbContext context) : base(context) { }
}
