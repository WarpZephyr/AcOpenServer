﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace AcOpenServer.Logging
{
    public class Logger : IDisposable
    {
        public delegate void WriteDelegete(string value);
        private const string InfoHeader = "Info->";
        private const string WarnHeader = "Warn->";
        private const string ErrorHeader = "Error->";
        private const string DebugHeader = "Debug->";

        private readonly Timer Timer;
        private readonly StringBuilder Buffer;
        private readonly Queue<string> Queue;
        private int CurrentQueueLength;
        private TimeSpan PeriodInternal;
        private bool DoTimerInternal;
        private bool disposedValue;

        public WriteDelegete WriteCallback { get; set; }

        #region Timer

        public TimeSpan Period
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PeriodInternal;
            set
            {
                if (DoTimer)
                {
                    Timer.Change(TimeSpan.Zero, value);
                }

                PeriodInternal = value;
            }
        }

        public bool DoTimer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DoTimerInternal;
            set
            {
                if (value && !DoTimerInternal)
                {
                    Timer.Change(TimeSpan.Zero, Period);
                }
                else if (!value && DoTimerInternal)
                {
                    Timer.Change(TimeSpan.Zero, TimeSpan.Zero);
                }

                DoTimerInternal = value;
            }
        }

        #endregion

        #region Channel Flags

        public LogChannelFlags ChannelFlags { get; set; }

        public bool DoLogInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChannelFlags & LogChannelFlags.Info) != 0;
            set
            {
                if (value)
                {
                    ChannelFlags |= LogChannelFlags.Info;
                }
                else
                {
                    ChannelFlags &= ~LogChannelFlags.Info;
                }
            }
        }

        public bool DoLogWarn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChannelFlags & LogChannelFlags.Warn) != 0;
            set
            {
                if (value)
                {
                    ChannelFlags |= LogChannelFlags.Warn;
                }
                else
                {
                    ChannelFlags &= ~LogChannelFlags.Warn;
                }
            }
        }

        public bool DoLogError
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChannelFlags & LogChannelFlags.Error) != 0;
            set
            {
                if (value)
                {
                    ChannelFlags |= LogChannelFlags.Error;
                }
                else
                {
                    ChannelFlags &= ~LogChannelFlags.Error;
                }
            }
        }

        public bool DoLogDebug
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChannelFlags & LogChannelFlags.Debug) != 0;
            set
            {
                if (value)
                {
                    ChannelFlags |= LogChannelFlags.Debug;
                }
                else
                {
                    ChannelFlags &= ~LogChannelFlags.Debug;
                }
            }
        }

        #endregion

        #region Constructors

        public Logger(WriteDelegete writeCallback) : this(TimeSpan.Zero, writeCallback)
        {
        }

        public Logger(TimeSpan period, WriteDelegete writeCallback)
        {
            DoTimerInternal = period != TimeSpan.Zero;
            PeriodInternal = period;
            Timer = new Timer(TimerHandler, null, TimeSpan.Zero, period);
            Buffer = new StringBuilder();
            Queue = new Queue<string>();
            CurrentQueueLength = 0;
            WriteCallback = writeCallback;
        }

        #endregion

        #region Factory

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Logger FromConsole()
            => new(Console.Write);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Logger FromConsole(TimeSpan period)
            => new(period, Console.Write);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Logger FromConsoleSeconds(int seconds)
            => new(TimeSpan.FromSeconds(seconds), Console.Write);

        #endregion

        #region Callbacks

        private void TimerHandler(object? state)
        {
            lock (Timer)
            {
                Flush();
            }
        }

        #endregion

        #region Flush

        public void Flush()
        {
            if (CurrentQueueLength < 1)
            {
                return;
            }

            Buffer.Length = 0;
            Buffer.Length = CurrentQueueLength;
            while (Queue.TryDequeue(out string? result))
            {
                Buffer.Append(result);
            }

            CurrentQueueLength = 0;
            WriteCallback(Buffer.ToString());
        }

        private void CheckFlushState()
        {
            // Flush immediately
            if (!DoTimer)
            {
                Flush();
            }
        }

        #endregion

        #region Enqueue

        private void Enqueue(string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(value);
            CurrentQueueLength += value.Length;
            CheckFlushState();
        }

        private void EnqueueLine(string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(value);
            Queue.Enqueue(Environment.NewLine);
            CurrentQueueLength += value.Length + Environment.NewLine.Length;
            CheckFlushState();
        }

        private void EnqueueWrapLine(string start, string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(start);
            Queue.Enqueue(value);
            Queue.Enqueue(Environment.NewLine);
            CurrentQueueLength += value.Length + start.Length + Environment.NewLine.Length;
            CheckFlushState();
        }

        private void EnqueueScoped(string scope, string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(scope);
            Queue.Enqueue(value);
            CurrentQueueLength += value.Length + scope.Length;
            CheckFlushState();
        }

        private void EnqueueLineScoped(string scope, string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(scope);
            Queue.Enqueue(value);
            Queue.Enqueue(Environment.NewLine);
            CurrentQueueLength += value.Length + scope.Length + Environment.NewLine.Length;
            CheckFlushState();
        }

        private void EnqueueWrapLineScoped(string start, string scope, string value)
        {
            // Avoid allocating new string to combine them
            Queue.Enqueue(start);
            Queue.Enqueue(scope);
            Queue.Enqueue(value);
            Queue.Enqueue(Environment.NewLine);
            CurrentQueueLength += value.Length + start.Length + scope.Length + Environment.NewLine.Length;
            CheckFlushState();
        }

        #endregion

        #region Generic

        public void Write(string value)
        {
            Enqueue(value);
        }

        public void WriteLine(string value)
        {
            EnqueueLine(value);
        }

        #endregion

        #region Levels

        public void Info(string value)
        {
            if (DoLogInfo)
            {
                EnqueueWrapLine(InfoHeader, value);
            }
        }

        public void Warn(string value)
        {
            if (DoLogWarn)
            {
                EnqueueWrapLine(WarnHeader, value);
            }
        }

        public void Error(string value)
        {
            if (DoLogError)
            {
                EnqueueWrapLine(ErrorHeader, value);
            }
        }

        [Conditional("DEBUG")]
        public void Debug(string value)
        {
            if (DoLogDebug)
            {
                EnqueueWrapLine(DebugHeader, value);
            }
        }

        #endregion

        #region Scoped Generic

        public void ScopedWrite(string scope, string value)
        {
            EnqueueScoped(scope, value);
        }

        public void ScopedWriteLine(string scope, string value)
        {
            EnqueueLineScoped(scope, value);
        }

        #endregion

        #region Scoped Levels

        public void ScopedInfo(string scope, string value)
        {
            if (DoLogInfo)
            {
                EnqueueWrapLineScoped(InfoHeader, scope, value);
            }
        }

        public void ScopedWarn(string scope, string value)
        {
            if (DoLogWarn)
            {
                EnqueueWrapLineScoped(WarnHeader, scope, value);
            }
        }

        public void ScopedError(string scope, string value)
        {
            if (DoLogError)
            {
                EnqueueWrapLineScoped(ErrorHeader, scope, value);
            }
        }

        [Conditional("DEBUG")]
        public void ScopedDebug(string scope, string value)
        {
            if (DoLogDebug)
            {
                EnqueueWrapLineScoped(DebugHeader, scope, value);
            }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (Timer)
                    {
                        Timer.Dispose();
                    }

                    Flush();
                    Buffer.Length = 0;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
