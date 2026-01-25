using System.Threading.Tasks;

namespace TeachAssistApp.Services;

public interface ICredentialService
{
    Task SaveCredentialsAsync(string username, string password, bool remember);
    Task<(string? username, string? password)> GetCredentialsAsync();
    Task ClearCredentialsAsync();
}
