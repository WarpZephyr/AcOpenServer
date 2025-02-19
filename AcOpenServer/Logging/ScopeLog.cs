using System;
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

        #region Timer

        public TimeSpan Period
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.Period;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.Period = value;
        }

        public bool DoTimer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Logger.DoTimer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Logger.DoTimer = value;
        }

        #endregion

        #region Constructors

        public ScopeLog(Logger logger, string scope) : this(logger, scope, null) { }

        private ScopeLog(Logger logger, string scope, ScopeLog? parent)
        {
            Logger = logger;
            Scope = $"{scope}->";
            Parent = parent;
        }

        #endregion

        #region Scope Factory

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScopeLog Push(string scope)
            => new(Logger, $"{Scope}{scope}", this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScopeLog Pop()
            => Parent ?? this;

        #endregion

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
