using System;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    // Keep only DTOs in shared project
    class SecretReader
    {
        internal static async Task<string> GetAsync(string secretName) =>
            Environment.GetEnvironmentVariable(secretName, EnvironmentVariableTarget.Process) ??
            Environment.GetEnvironmentVariable(secretName, EnvironmentVariableTarget.User) ??
            Environment.GetEnvironmentVariable(secretName, EnvironmentVariableTarget.Machine);
    }
}