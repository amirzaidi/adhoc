using AdHocMAC.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AdHocMAC.Nodes.Routing
{
    class Routing
    {
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
    }
}
