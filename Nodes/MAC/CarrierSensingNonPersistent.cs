using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingNonPersistent : CarrierSensing
    {

        //var deferTimeNonP = 10.0f;
        Task Attempt;
        public Action<Packet> SendAction;
        /*public CarrierSensingNonPersistent() : base()
        {
        }
        */

        override public async Task Send(Packet OutgoingPacket)
        {
            var Random = new Random();
            var time = DateTime.Now;
            int attempt = 0;
            bool isMsgSent = false;
            do
            {
                if (!mIsChannelBusy)
                {
                    SendAction(OutgoingPacket);
                    Debug.WriteLine($"[{OutgoingPacket.From}]NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                    isMsgSent = true;
                }
                else
                {
                    Debug.WriteLine($"[{OutgoingPacket.From}]LineBusy at Attempt {attempt} to send");
                    Debug.WriteLine($"[{OutgoingPacket.From}]Awaiting random");
                    var sleeper = Task.Delay(Random.Next(1, 2));
                    await sleeper;
                    attempt++;
                }
            } while (!isMsgSent);
            isMsgSent = false;
            attempt = 0;
        }
    }
}
