using CompSci.Core.Interfaces;
using CompSci.Core.Services;
using CompSci.Infrastructure;
using CompSci.Infrastructure.Data;
using CompSci.Infrastructure.FileStorage;
using CompSci.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Api.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IPastQuestionService, PastQuestionService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IStudentService, StudentService>();

        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
