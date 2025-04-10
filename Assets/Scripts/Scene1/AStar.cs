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
        // Usando o retorno de Stack<(int, int)> para facilitar percorrer do caminho. O primeiro passo de movimento é sempre o último adicionado à pilha, então ele se torna o primeiro para o deslocamento.
        public static Stack<(int,int)> AStarPathfinding(Vector2 start, Vector2 target, MapGenerator map)
        {
            if (map.IsBlocked((int)target.x, (int)target.y))
            {
                Debug.LogWarning("Can't trace path to a blocked Goal");
                return null;
            }

            Dictionary<(int, int), Node> openNodes = new Dictionary<(int, int), Node>();
            Dictionary<(int, int), Node> closedNodes = new Dictionary<(int, int), Node>();

            // Adicionando o nó inicial como o caminho inicial para a IA.
            AddOrReplace(openNodes, new Node((int)start.x, (int)start.y, 0, 0, 0, null));

            while (openNodes.Count > 0)
            {
                Node currentNode = GetNodeWithLowestFScore(openNodes);
                var currentKey = (currentNode.x, currentNode.y);
                openNodes.Remove(currentKey);
                AddOrReplace(closedNodes, currentNode);

                if (currentNode.x == (int)target.x && currentNode.y == (int)target.y)
                {
                    return CreatePath();
                }
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

                        // Ignorar células bloqueadas.
                        if (map.IsBlocked(nextX, nextY))
                            continue;

                        // Verificação especial para movimentos diagonais
                        // Como o mapa é feito de quadrados, os movimentos diagonais são impossíveis se os vizinhos
                        // adjacentes (relativos ao movimento diagonal) estiverem bloqueados
                        if(Mathf.Abs(i) == 1 && Mathf.Abs(j) == 1)
                        {
                            var neighborsI = (currentNode.x+ i, currentNode.y);
                            var neighborsJ = (currentNode.x, currentNode.y + j);
                            if(map.IsBlocked(neighborsI.Item1, neighborsI.Item2) || map.IsBlocked(neighborsJ.Item1, neighborsJ.Item2))
                                continue;
                        }

                        // Calcular custos para o vizinho.
                        int gCost = currentNode.g + CalculateMovementCost(0, 0, i, j);
                        int hCost = CalculateMovementCost((int)target.x, (int)target.y, nextX, nextY);
                        int fCost = gCost + hCost;

                        // Verificar lista fechada.
                        if (closedNodes.TryGetValue(coordinateKey, out Node closedNode) && fCost >= closedNode.f)
                        {
                            continue;
                        }

                        // Verificar lista aberta.
                        if (openNodes.TryGetValue(coordinateKey, out Node openNode) && fCost >= openNode.f)
                        {
                            continue;
                        }

                        // Se chegamos aqui, ou o vizinho não está em aberto/fechado,
                        // ou encontramos um caminho melhor. Adicionar ou atualizar.
                        AddOrReplace(openNodes, new Node(nextX, nextY, gCost, hCost, fCost, currentNode));
                    }
                }
            }

            // Função reconstruir o caminho do destino até o ponto inicial
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

                // Percorrer de volta do nó de destino até o nó inicial
                while (node != null)
                {
                    pathStack.Push((node.x, node.y));
                    node = node.parent;
                }

                return pathStack;
            }

            Debug.LogWarning("Did not find the path");
            return null;
        }
        
        // A heurística é o cálculo do custo (distância) entre dois pontos usando um custo de movimento diagonal de 14 e um custo de movimento reto de 10.
        // Basicamente, representa o custo de 1 e sqrt(2) para movimentos diagonais, apenas multiplicado por 10 para simplificar.
        // Prioriza movimentos diagonais porque eles cobrem uma maior distância em um único passo, tornando o caminho mais eficiente.
        static int CalculateMovementCost(int x1, int y1, int x2, int y2)
        {
            int deltaX = Mathf.Abs(x1 - x2);
            int deltaY = Mathf.Abs(y1 - y2);
            int cost = 0;

            // Calcular custo de movimento diagonal
            while (deltaX > 0 && deltaY > 0)
            {
                cost += 14;
                deltaX--;
                deltaY--;
            }

            // Adicionar o custo para os movimentos retos restantes
            cost += 10 * deltaX;
            cost += 10 * deltaY;

            return cost;
        }

        // Adiciona um novo nó ao dicionário ou substitui um nó existente se o novo tiver um custo total menor.
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

        // Encontra o nó com o menor custo total (valor f) no dicionário.
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
