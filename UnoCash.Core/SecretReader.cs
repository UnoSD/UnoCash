using System.Threading.Tasks;

namespace UnoCash.Core
{
    // Keep only DTOs in shared project
    class SecretReader
    {
        internal static Task<string> GetAsync(string secretName) =>
            EnvironmentVariableSettingStore.GetSetting(secretName)
                                           .ToTask();
    }
}