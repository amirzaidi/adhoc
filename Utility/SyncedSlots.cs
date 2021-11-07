using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Utility
{
    class SyncedSlots
    {
        private const long SLOT_TICKS = (long)(TimeSpan.TicksPerSecond * Configuration.SLOT_SECONDS);

        public static async Task WaitUntilSlot(int FullSlots, CancellationToken Token)
        {
            var currTime = DateTime.Now;
            var ticksToNextSlot = SLOT_TICKS - (currTime.Ticks % SLOT_TICKS);
            var msToNextSlot = ticksToNextSlot / (double)TimeSpan.TicksPerMillisecond;

            var ticksAdditionalSlots = FullSlots * SLOT_TICKS;
            var msAdditionalSlots = ticksAdditionalSlots / (double)TimeSpan.TicksPerMillisecond;

            await Task.Delay((int)Math.Ceiling(msToNextSlot + msAdditionalSlots), Token).IgnoreExceptions();
        }
    }
}
