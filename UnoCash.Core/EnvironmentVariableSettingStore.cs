using System;

namespace UnoCash.Core
{
    static class EnvironmentVariableSettingStore
    {
        internal static string GetSetting(string name) =>
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ??
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
    }
}