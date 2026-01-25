using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeachAssistApp.Services;

public interface ITeachAssistService
{
    Task<bool> LoginAsync(string username, string password);
    Task<List<Models.Course>> GetCoursesAsync();
    Task<Models.Course?> GetCourseDetailsAsync(string subjectId, string studentId);
    Task LogoutAsync();
    bool IsLoggedIn { get; }
    string? LastError { get; }
}
