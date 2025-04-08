using UnityEngine;
using System.Collections.Generic;
using System.Text;
namespace AStarPath
{
    public class AStar
    {
        private class Node
        {
            public int x,y;
            public int f, g,h;

            public Node parent;

            public Node(int x, int y,int g, int h, int f, Node parent)
            {
                this.x = x; this.y = y; this.f = f; this.g = g; this.h = h; this.parent = parent;
            }
            public override bool Equals(object obj)
            {
                if (obj is Node other)
                {
                    return this.x == other.x && this.y == other.y;
                }
                return false;
            }
            public override int GetHashCode()
            {
                return x.GetHashCode() ^ y.GetHashCode();
            }
        }
        public static Stack<(int,int)> AStarPathfinding(Vector2 start, Vector2 target, MapGenerator map)
        {
            if (map.IsBlocked((int)target.x, (int)target.y))
            {
                Debug.LogWarning("Can't trace path to a blocked Goal");
                return null;
            }

            Dictionary<(int, int), Node> openNodes = new Dictionary<(int, int), Node>();
            Dictionary<(int, int), Node> closedNodes = new Dictionary<(int, int), Node>();

            // Adding starting node as the initial path for the IA.
            AddOrReplace(openNodes, new Node((int)start.x, (int)start.y, 0, 0, 0, null));

            while (openNodes.Count > 0)
            {
                Node currentNode = GetNodeWithLowestFScore(openNodes);
                var currentKey = (currentNode.x, currentNode.y);
                openNodes.Remove(currentKey);
                AddOrReplace(closedNodes, currentNode);

                //Debug.Log($"On node {currentNode.x}, {currentNode.y}, open list size {openNodes.Count}");

                if (currentNode.x == (int)target.x && currentNode.y == (int)target.y)
                {
                    //Debug.Log("Found it");
                    return CreatePath();
                    
                }
                //LogNodesMatrix(closedNodes, openNodes); //To debug
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                        {
                            continue;
                        }

                        int nextX = (int)currentNode.x + i;
                        int nextY = (int)currentNode.y + j;
                        var coordinateKey = (nextX, nextY);

                        // Skip blocked cells.
                        if (map.IsBlocked(nextX, nextY))
                        {
                            continue;
                        }

                        // Calculate costs for the neighbor.
                        int gCost = currentNode.g + CalculateMovementCost(0, 0, i, j);
                        int hCost = CalculateMovementCost((int)target.x, (int)target.y, nextX, nextY);
                        int fCost = gCost + hCost;

                        // Check closed list.
                        if (closedNodes.TryGetValue(coordinateKey, out Node closedNode) && fCost >= closedNode.f)
                        {
                            //Debug.Log($"Ignoring node {nextX}, {nextY} from closed list");
                            continue;
                        }

                        // Check open list.
                        if (openNodes.TryGetValue(coordinateKey, out Node openNode) && fCost >= openNode.f)
                        {
                            //Debug.Log($"Ignoring node {nextX}, {nextY} from open list");
                            continue;
                        }

                        // If we've gotten here, either the neighbor isn't in open/closed,
                        // or we found a better path. Add or update it.
                        AddOrReplace(openNodes, new Node(nextX, nextY, gCost, hCost, fCost, currentNode));

                    }
                }
            }

            // Local function to reconstruct the path
            Stack<(int,int)> CreatePath()
            {
                var currentKey = ((int)target.x, (int)target.y);
                Stack<(int, int)> pathStack = new Stack<(int, int)>();

                Node node;
                if (!closedNodes.TryGetValue(currentKey, out node))
                {
                    Debug.LogError($"Could not find node at target position {target.x}, {target.y}");
                    throw new System.Exception("Path creation failed.");
                }

                // Traverse back from the target node to the start node
                while (node != null && (node.x != (int)start.x || node.y != (int)start.y))
                {
                    pathStack.Push((node.x, node.y));
                    node = node.parent;
                }
                // Push the start node
                pathStack.Push(((int)start.x, (int)start.y));

                return pathStack;
            }

            Debug.LogWarning("Did not find the path");
            return null;
        }
        
        // To Debug to Current Step in the algorithm
        static void LogNodesMatrix(Dictionary<(int, int), Node> closedNodes, Dictionary<(int, int), Node> openNodes)
        {
            
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var dict in new[] { closedNodes, openNodes })
            {
                foreach (var key in dict.Keys)
                {
                    if (key.Item1 < minX) minX = key.Item1;
                    if (key.Item1 > maxX) maxX = key.Item1;
                    if (key.Item2 < minY) minY = key.Item2;
                    if (key.Item2 > maxY) maxY = key.Item2;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("----- Nodes Matrix -----");
            
            for (int y = maxY; y >= minY; y--)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    (int, int) coord = (x, y);
                    if (closedNodes.TryGetValue(coord, out Node closedNode))
                    {
                        // Format: C(g,h,f)
                        sb.Append($" C(g:{closedNode.g},h:{closedNode.h},f:{closedNode.f})\t");
                    }
                    else if (openNodes.TryGetValue(coord, out Node openNode))
                    {
                        // Format: O(g,h,f)
                        sb.Append($" O(g:{openNode.g},h:{openNode.h},f:{openNode.f})\t");
                    }
                    else
                    {
                        sb.Append(" ------ \t");
                    }
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }


        // Calculates the cost (distance) between two points using a diagonal movement cost of 14 and straight cost of 10.
        static int CalculateMovementCost(int x1, int y1, int x2, int y2)
        {
            int deltaX = Mathf.Abs(x1 - x2);
            int deltaY = Mathf.Abs(y1 - y2);
            int cost = 0;

            // Calculate diagonal movement cost
            while (deltaX > 0 && deltaY > 0)
            {
                cost += 14;
                deltaX--;
                deltaY--;
            }

            // Add the cost for remaining straight moves
            cost += 10 * deltaX;
            cost += 10 * deltaY;

            return cost;
        }

        // Adds a new node to the dictionary or replaces an existing node if the new one has a lower total cost.
        static void AddOrReplace(Dictionary<(int, int), Node> nodeDict, Node newNode)
        {
            var key = (newNode.x, newNode.y);
            if (nodeDict.TryGetValue(key, out Node existingNode))
            {
                if (newNode.f < existingNode.f)
                {
                    nodeDict[key] = newNode;
                }
            }
            else
            {
                nodeDict.Add(key, newNode);
            }
        }

        // Finds the node with the lowest total cost (f value) in the dictionary.
        static Node GetNodeWithLowestFScore(Dictionary<(int, int), Node> nodeDict)
        {
            if (nodeDict == null)
            {
                Debug.LogError("Cannot find the lowest value in a null dictionary.");
                throw new System.Exception("Dictionary is null.");
            }

            Node lowestNode = null;
            foreach (var pair in nodeDict)
            {
                Node currentNode = pair.Value;
                if (lowestNode == null || currentNode.f < lowestNode.f ||  (currentNode.f == lowestNode.f && currentNode.h < lowestNode.h))
                {
                    lowestNode = currentNode;
                }
            }
            return lowestNode;
        }
    }
    
}
