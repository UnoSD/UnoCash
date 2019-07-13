using System;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    class ConfigurationReader
    {
        public static async Task<string> GetAsync(string name) => 
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ??
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
    }
}