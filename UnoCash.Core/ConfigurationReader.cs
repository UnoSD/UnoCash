using System.Threading.Tasks;

namespace UnoCash.Core
{
    public class ConfigurationReader
    {
        public static Task<string> GetAsync(string name) =>
            EnvironmentVariableSettingStore.GetSetting(name)
                                           .ToTask();
    }
}