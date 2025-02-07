using System;

namespace AcOpenServer.Logging
{
    public class Logger
    {
        public LogChannelFlags EnabledChannels { get; set; }

        public Logger()
        {
#if DEBUG
            EnabledChannels = LogChannelFlags.Info | LogChannelFlags.Warning | LogChannelFlags.Error | LogChannelFlags.Debug;
#else
            EnabledChannels = LogChannelFlags.Info | LogChannelFlags.Warning | LogChannelFlags.Error;
#endif
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            if ((EnabledChannels & LogChannelFlags.Info) != 0)
            {
                WriteLine($"Info: {message}");
            }
        }

        public void Warning(string message)
        {
            if ((EnabledChannels & LogChannelFlags.Warning) != 0)
            {
                WriteLine($"Warning: {message}");
            }
        }

        public void Error(string message)
        {
            if ((EnabledChannels & LogChannelFlags.Error) != 0)
            {
                WriteLine($"Error: {message}");
            }
        }


        public void Debug(string message)
        {
#if DEBUG
            if ((EnabledChannels & LogChannelFlags.Debug) != 0)
            {
                WriteLine($"Debug: {message}");
            }
#endif
        }

        [Flags]
        public enum LogChannelFlags
        {
            None,
            Info,
            Warning,
            Error,
            Debug
        }
    }
}
