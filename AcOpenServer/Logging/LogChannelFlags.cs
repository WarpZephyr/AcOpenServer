using System;

namespace AcOpenServer.Logging
{
    [Flags]
    public enum LogChannelFlags : byte
    {
        None,
        Info,
        Warn,
        Error,
        Debug
    }
}
