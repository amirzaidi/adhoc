using System;

namespace AdHocMAC.Utility
{
    class Timestamp
    {
        public static long UnixMS()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}
