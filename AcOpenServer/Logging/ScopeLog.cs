using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AcOpenServer.Logging
{
    public class ScopeLog
    {
        private readonly Logger Logger;
        private readonly string Scope;
        private readonly ScopeLog? Parent;

        #region Channel Flags

        public LogChannelFlags ChannelFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.ChannelFlags;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.ChannelFlags = value;
        }

        public bool DoLogInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.DoLogInfo;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.DoLogInfo = value;
        }

        public bool DoLogWarn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.DoLogWarn;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.DoLogWarn = value;
        }

        public bool DoLogError
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.DoLogError;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.DoLogError = value;
        }

        public bool DoLogDebug
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.DoLogDebug;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.DoLogDebug = value;
        }

        #endregion

        public ScopeLog(Logger logger, string scope) : this(logger, scope, null) { }

        private ScopeLog(Logger logger, string scope, ScopeLog? parent)
        {
            Logger = logger;
            Scope = $"{scope}->";
            Parent = parent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScopeLog Push(string scope)
            => new(Logger, $"{Scope}{scope}", this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScopeLog Pop()
            => Parent ?? this;

        #region Log

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string value)
            => Logger.ScopedInfo(Scope, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warn(string value)
            => Logger.ScopedWarn(Scope, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string value)
            => Logger.ScopedError(Scope, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEBUG")]
        public void Debug(string value)
            => Logger.ScopedDebug(Scope, value);

        #endregion
    }
}
