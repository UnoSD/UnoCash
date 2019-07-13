using System.Threading.Tasks;

namespace UnoCash.Core
{
    class ConfigurationReader
    {
        public static async Task<string> GetAsync(string name) =>
            EnvironmentVariableSettingStore.GetSetting(name);
    }
}