using AdHocMAC.GUI;
using AdHocMAC.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace AdHocMAC.Nodes.Routing
{
    class Routing
    {

        public static void GetShortestPath(NodeVisualizer<INode<Packet>> nodeVisualiser)
        {

            var nodesCount = nodeVisualiser.mNodes.Count;//nodeVisualiser.mNodes.Count;
            Debug.WriteLine($"Total number of nodes available : {nodesCount}");
            Debug.WriteLine($"Total number of lines available : {nodeVisualiser.mLines.Count}");
            int[,] weights = CreateMatrix(nodeVisualiser);
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
        private static int[,] CreateMatrix(NodeVisualizer<INode<Packet>> nodeVisualiser)
        {
            Debug.WriteLine($"Creating the matrix of weights ");
            var nodesCount = nodeVisualiser.mNodes.Count;
            int[,] array = new int[nodesCount, nodesCount];
            foreach (var node in nodeVisualiser.mNodes)
            {
                var currentNodeId = node.Value.GetID();
                var matchingNodes = nodeVisualiser.mLines.Where(i => i.Value.Item1.GetID() == node.Value.GetID() || i.Value.Item2.GetID() == node.Value.GetID()).ToArray();
                foreach (var element in matchingNodes)
                {
                    //Calculate the weight
                    int calculatedWeight = 1;
                    //Enter in matrix
                    if (element.Value.Item1.GetID() != currentNodeId)
                    {
                        array[currentNodeId, element.Value.Item1.GetID()] = calculatedWeight;
                    }
                    else
                    {
                        array[currentNodeId, element.Value.Item2.GetID()] = calculatedWeight;
                    }
                }
            }
            return array;
        }
    }
}
