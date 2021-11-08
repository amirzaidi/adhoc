using AdHocMAC.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AdHocMAC.Nodes.Routing
{
    class Router
    {
        private const bool DEBUG = false;

        private readonly RoutingCache mRoutingCache = new RoutingCache();
        private readonly Dictionary<(int, int), string> mToSend = new Dictionary<(int, int), string>();
        private readonly List<(long, string)> mLog = new List<(long, string)>();

        private readonly int mId;
        private readonly Action<int, object, CancellationToken> mSendLink;
        private readonly Action<object, CancellationToken> mSendBroadcast;
        private readonly Action<int, string> mOnDeliver;

        private int mSequenceNumber;

        public Router(int Id, Action<int, object, CancellationToken> SendLink, Action<object, CancellationToken> SendBroadcast, Action<int, string> OnDeliver)
        {
            mId = Id;
            mSendLink = SendLink;
            mSendBroadcast = SendBroadcast;
            mOnDeliver = OnDeliver;
        }

        public int UndeliveredMessages()
        {
            return mToSend.Count;
        }

        public void TryHandlePacket(object Packet, CancellationToken Token)
        {
            if (Packet is RoutingPacketControl control)
            {
                if (control.To == mId) // If we are the destination.
                {
                    if (control.Reply)
                    {
                        if (DEBUG) Debug.WriteLine($"RREP {mId} END: {string.Join("|", control.Nodes)}");
                        Log($"RREP -> Data {mId}: {string.Join("|", control.Nodes)}");

                        // To-Do: We now know the route, what do we want to do?
                        var tuple = (control.From, control.Seq);
                        if (mToSend.TryGetValue(tuple, out var data))
                        {
                            mToSend.Remove(tuple);
                            mSendLink(control.Nodes[1], new RoutingPacketData
                            {
                                From = mId,
                                To = control.From,
                                Nodes = control.Nodes,
                                Data = data,
                            }, Token);
                        }
                    }
                    else
                    {
                        control.To = control.From;
                        control.From = mId;
                        control.Nodes = AddIdToNodeArray(control.Nodes); // Since this is a struct, the original packet is not modified.
                        control.Reply = true;

                        // Send back to the node before us, so (len - 2).
                        if (DEBUG) Debug.WriteLine($"RREQ {mId} END: {string.Join("|", control.Nodes)}");
                        Log($"RREQ -> RREP At Destination {mId}: {string.Join("|", control.Nodes)}");

                        mSendLink(control.Nodes[^2], control, Token);
                    }
                }
                else
                {
                    if (control.Reply)
                    {
                        ForwardRouteReply(control, Token);
                    }
                    else
                    {
                        ForwardRouteRequest(control, Token);
                    }
                }

            }
            else if (Packet is RoutingPacketData data)
            {
                if (data.To == mId) // If we are the destination.
                {
                    if (DEBUG) Debug.WriteLine($"ROUTED DATA {mId} END: {string.Join("|", data.Nodes)}");
                    Log($"Routed Data Arrived At Destination {mId}: {string.Join("|", data.Nodes)}");

                    // Deliver it to event loop.
                    mOnDeliver(data.From, data.Data);
                }
                else
                {
                    ForwardData(data, Token);
                }
            }
        }

        public void FindRouteThenSend(int To, string Data, CancellationToken Token)
        {
            int seq = mSequenceNumber++;
            if (mRoutingCache.TryUpdateRREQLastBroadcastSeq(mId, To, seq))
            {
                // Save "Data" to send when we get reply.
                mToSend[(To, seq)] = Data;

                int[] nodes = { mId };
                Log($"RREQ Started At {mId} to {To}: {string.Join("|", nodes)}");

                mSendBroadcast(new RoutingPacketControl
                {
                    From = mId,
                    To = To,
                    Seq = seq,
                    Nodes = nodes,
                }, Token);
            }
            else
            {
                // This should never happen.
                throw new Exception();
            }
        }

        public void ForwardData(RoutingPacketData Data, CancellationToken Token)
        {
            if (DEBUG) Debug.WriteLine($"Forward Data {Data.From}, {mId}, {Data.To} [{string.Join("|", Data.Nodes)}]");
            Log($"Routed Data Forwarded At {mId}: {string.Join("|", Data.Nodes)}");

            int ourPosition = Array.IndexOf(Data.Nodes, mId);
            int nextNode = Data.Nodes[ourPosition + 1];
            mSendLink(nextNode, Data, Token);
        }

        public void ForwardRouteReply(RoutingPacketControl Control, CancellationToken Token)
        {
            int ourPosition = Array.IndexOf(Control.Nodes, mId);
            int nextNode = Control.Nodes[ourPosition - 1];

            if (DEBUG) Debug.WriteLine($"Forward RREP {Control.From}, {mId}, {Control.To} [{string.Join("|", Control.Nodes)}]");
            Log($"RREP Forwarded At {mId}: {string.Join("|", Control.Nodes)}");

            mSendLink(nextNode, Control, Token);
        }

        public void ForwardRouteRequest(RoutingPacketControl Control, CancellationToken Token)
        {
            if (mRoutingCache.TryUpdateRREQLastBroadcastSeq(Control.From, Control.To, Control.Seq))
            {
                Control.Nodes = AddIdToNodeArray(Control.Nodes); // Since this is a struct, the original packet is not modified.

                if (DEBUG) Debug.WriteLine($"Forward RREQ {Control.From}, {mId}, {Control.To} [{string.Join("|", Control.Nodes)}]");
                Log($"RREQ Forwarded At {mId} to {Control.To}: {string.Join("|", Control.Nodes)}");

                mSendBroadcast(Control, Token);
            }
        }

        private int[] AddIdToNodeArray(int[] Nodes)
        {
            int[] newNodes = new int[Nodes.Length + 1];
            Array.Copy(Nodes, newNodes, Nodes.Length);
            newNodes[Nodes.Length] = mId;
            return newNodes;
        }

        public List<(long, string)> GetLog() => mLog;

        private void Log(string Log)
        {
            mLog.Add((Timestamp.UnixMS(), Log));
        }
    }
}
