using System.Runtime.CompilerServices;

namespace AcOpenServer.Logging
{
    public static class LogExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScopeLog Scope(this Logger logger, string scope)
            => new(logger, scope);
    }
}
