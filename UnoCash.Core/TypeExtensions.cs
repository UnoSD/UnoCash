namespace UnoCash.Core
{
    public static class TypeExtensions
    {
        internal static T Coalesce<T>(this T toCheck, T replacement, T comparison = default) =>
            toCheck.Equals(comparison) ? replacement : toCheck;
    }
}