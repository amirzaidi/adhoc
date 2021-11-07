using AdHocMAC.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.Routing
{
    class Routing
    {
        struct RoutePacket
        {
            // We need a sequence number to check ACKs.
            public int From, To, Seq;
            public bool ACK;
            public string Data;
            public List<int> travelledNodes;
            public RouteType type;

            public static int GetLength(Packet Packet)
            {
                var byteCount = 3 * sizeof(int)
                    + 1 * sizeof(bool);
                    //+ Packet.Data.Length;

                return byteCount;
            }
        }

        enum RouteType 
        {
            RREQ,
            RREP,
            Data
        };

        public static void GetShortestPath(List<Node> nodes, Func<Node, Vector3D> getPosition, double range)
        {
            var directionalPaths = new List<(Node, Node, double)>();
            foreach (var n1 in nodes)
            {
                foreach (var n2 in nodes)
                {
                    if (n1 != n2)
                    {
                        var distance = Vector3D.Distance(getPosition(n1), getPosition(n2));
                        if (distance <= range)
                        {
                            var weight = distance;
                            directionalPaths.Add((n1, n2, weight));
                        }
                    }
                }
            }

            var nodesCount = nodes.Count;//nodeVisualiser.mNodes.Count;
            Debug.WriteLine($"Total number of nodes available : {nodesCount}");
            Debug.WriteLine($"Total number of lines available : {nodes.Count * (nodes.Count - 1)}");
            int[,] weights = CreateMatrix(nodes, directionalPaths);
            var initNode = 0;
            
            //Create list containg the table of Destination node, weight, visited property
            List<int> unvisitedNode = new List<int>();
            var previousVertex = new int[nodesCount];
            var shortestDistance = new int[nodesCount];
            var temp = 9999999;
            var tempIndex = 9999999;
            int a = 0;
            for (int i = 0; i < nodesCount; i++)
            {
                unvisitedNode.Add(i);
                shortestDistance[i] = 9999999;
                previousVertex[i] = initNode;
            }
            while (unvisitedNode.Count != 0)
            {
                for (int i = 0; i < nodesCount; i++)
                {
                    if (!unvisitedNode.Contains(i) || i.Equals(initNode))
                    {
                        continue;
                    }
                    // a = weights[previousVertex[i], initNode];
                    if (weights[initNode, i] != 0 && weights[initNode, i] + a < shortestDistance[i])
                    {

                        shortestDistance[i] = a + weights[initNode, i];
                        previousVertex[i] = initNode;
                    }
                }
                temp = shortestDistance.Max();
                unvisitedNode.Remove(initNode);
                for (int i = 0; i < nodesCount; i++)
                {
                    if (shortestDistance[i] != 0 && unvisitedNode.Contains(i))
                    {
                        if (shortestDistance[i] < temp)
                        {
                            temp = shortestDistance[i];
                            tempIndex = i;
                        }
                    }
                }

                initNode = tempIndex;
                a = shortestDistance[initNode];
            }
        }

        /// <summary>
        /// Create the adjecent matrix with weights
        /// </summary>
        /// <param name="nodeVisualiser"></param>
        /// <returns></returns>
        private static int[,] CreateMatrix(List<Node> nodes, List<(Node, Node, double)> paths)
        {
            Debug.WriteLine($"Creating the matrix of weights ");
            var nodesCount = nodes.Count;
            int[,] array = new int[nodesCount, nodesCount];

            foreach (var path in paths)
            {
                var (n1, n2, _) = path;
                array[n1.GetID(), n2.GetID()] = 1;
            }

            return array;
        }

        public static void InitiateRouting(List<Node> nodes, Func<Node, Vector3D> getPosition, double range)
        {
            int source = 0;
            int destination = 6;
            int seqNumber = 1;
            List<int> travelledPath = new List<int>();

            //Create the RReq packet as soon as the source and destination is known

            var rReqPacket = new RoutePacket();
            rReqPacket.From = source;
            rReqPacket.To = destination;
            travelledPath.Add(source);
            rReqPacket.travelledNodes =travelledPath;
            rReqPacket.Seq = seqNumber;
            rReqPacket.type = RouteType.RREQ;

            //Get the adjectnt nodes
            var connectedNodes =GetConnectedNodes(nodes, 0, getPosition, range);

            //Initiate tranfer
            Parallel.ForEach(connectedNodes, node =>
            {
                //Send with the specified protocol
            });
        }

        private static List<int> GetConnectedNodes(List<Node> nodes, int sourceNode, Func<Node, Vector3D> getPosition, double range)
        {
            var directionalPaths = new List<(Node, Node, double)>();
            var connectedNodes = new List<int>();
            Node n1 = nodes.FirstOrDefault(x => x.GetID().Equals(sourceNode));
            foreach (var n2 in nodes)
            {
                if (n1 != n2)
                {
                    var distance = Vector3D.Distance(getPosition(n1), getPosition(n2));
                    if (distance <= range)
                    {
                        connectedNodes.Add(n2.GetID());
                    }
                }
            }
            return connectedNodes;
        }

        private static List<int> GetConnectedNodes()
        {
            return new List<int>();
        }

        private static void onReceive(Node node, RoutePacket packet)
        {
            //First check if it is the destination
            RouteType type = packet.type;
            var newPacket = new RoutePacket();
            if (node.GetID() == packet.To)
            {
                if (type == RouteType.RREQ)
                {
                    //Create the RREP 
                    newPacket.From = node.GetID();
                    newPacket.To = packet.travelledNodes[0];
                    newPacket.Seq = packet.Seq;
                    newPacket.type = RouteType.RREP;
                }

                else if (type == RouteType.RREP)
                {
                    //Send the data
                    newPacket.From = node.GetID();
                    newPacket.To = packet.travelledNodes[packet.travelledNodes.Count-1];
                    newPacket.Seq = packet.Seq;
                    newPacket.type = RouteType.Data;
                }

            }
            else
            {
                if (type == RouteType.RREQ)
                {
                    //Add self to the travelled path
                    newPacket.travelledNodes.Add(node.GetID());

                    //Get the adjecent node and transmit
                    var connectedNodes = GetConnectedNodes();
                    foreach (var connectedNode in connectedNodes)
                    {
                        if (!packet.travelledNodes.Contains(connectedNode))
                        { 
                            //forward the packet
                        }
                    }
                    
                }
                else if (type == RouteType.RREP)
                {
                    //Send the data
                    newPacket.From = node.GetID();
                    newPacket.To = packet.travelledNodes[0];
                    newPacket.Seq = packet.Seq;
                    newPacket.type = RouteType.Data;
                }
            }
        }

    }
}
