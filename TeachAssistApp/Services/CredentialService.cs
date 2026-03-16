using System;
using System.Threading.Tasks;
using CredentialManagement;
using System.Runtime.InteropServices;

namespace TeachAssistApp.Services;

public class CredentialService : ICredentialService
{
    private const string TargetName = "TeachAssistApp_TeachAssist";

    public Task SaveCredentialsAsync(string username, string password, bool remember)
    {
        if (!remember)
        {
            // If not remembering, just clear any existing credentials
            return ClearCredentialsAsync();
        }

        return Task.Run(() =>
        {
            try
            {
                using var cred = new Credential
                {
                    Target = TargetName,
                    Username = username,
                    Password = password,
                    Type = CredentialType.Generic,
                    PersistanceType = PersistanceType.LocalComputer
                };
                cred.Save();
            }
            catch (Exception)
            {
                // Fail silently - credential saving is not critical
            }
        });
    }

    public Task<(string? username, string? password)> GetCredentialsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var cred = new Credential { Target = TargetName };
                if (cred.Load())
                {
                    return (cred.Username, cred.Password);
                }
            }
            catch (Exception)
            {
                // Return empty on error
            }

            return (null, null);
        });
    }

    public Task ClearCredentialsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var cred = new Credential { Target = TargetName };
                if (cred.Load())
                {
                    cred.Delete();
                }
            }
            catch (Exception)
            {
                // Fail silently
            }
        });
    }
}
