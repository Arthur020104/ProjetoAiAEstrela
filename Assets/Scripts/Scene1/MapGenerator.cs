using System.Collections;
using System.Collections.Generic;
using AStarPath;
using UnityEngine;
using System.Linq;


public class MapGenerator : MonoBehaviour
{
    public int gridWidth, gridHeight; // Using Cartesian coordinates: width (X) and height (Y)
    [SerializeField] private GameObject defaultWallPrefab, itemsPrefab;
    private Vector3 defaultWallSize;
    [SerializeField] private float renderProbability = 50.0f, wallYPosition = 0.5f;
    private HashSet<Vector3> noSpawnPositionsSet = new HashSet<Vector3>();
    [SerializeField] private Vector3[] noSpawnPositionsArray;

    [SerializeField] private int numberOfFakeGoals = 3;
    public GameObject[][] mapGrid;

    [SerializeField] private Material mapMaterial;

    public List<Vector2> itemsPosition;
    [SerializeField] private int _amountOfItems = 3;
    public int AmountOfItems
    {
        get => _amountOfItems;
    }
    private bool _mapCreated = false;

    [SerializeField]private Transform _playerTransform, _enemyTransform, _portalTransform;
    
    void Awake()
    {
        if (_playerTransform == null || _enemyTransform == null)
        {
            Debug.LogError("Player or Enemy Transform are not assigned in the Inspector.");
        }

        InitializeNoSpawnPositions();

        Vector2 playerV2Pos = new Vector2(Mathf.RoundToInt(_playerTransform.position.x), Mathf.RoundToInt(_playerTransform.position.z));

        // Ensure paths for enemy and portal
        GenerateRequiredPaths(playerV2Pos);

        // Get wall size and generate goals
        defaultWallSize = GetGameObjectPrefabSize(defaultWallPrefab);
        GenerateFakeGoals(playerV2Pos);

        // Initialize map grid
        mapGrid = new GameObject[gridWidth][];
        for (int i = 0; i < gridWidth; i++)
        {
            mapGrid[i] = new GameObject[gridHeight];
        }

        GenerateGrid();
        _mapCreated = true;
        OptimizeMap();

        // Generate items
        itemsPosition = new List<Vector2>();
        GameObject itemContainer = new GameObject { name = "Item Container" };
        GenerateItems(playerV2Pos, itemContainer);
    }
    private void InitializeNoSpawnPositions()
    {
        noSpawnPositionsSet.UnionWith(noSpawnPositionsArray);
        noSpawnPositionsSet.Add(_playerTransform.position);
        noSpawnPositionsSet.Add(_enemyTransform.position);
    }

    private void GenerateRequiredPaths(Vector2 playerV2Pos)
    {
        GeneratePath(playerV2Pos, new Vector2(Mathf.RoundToInt(_enemyTransform.position.x), Mathf.RoundToInt(_enemyTransform.position.z)));
        GeneratePath(playerV2Pos, new Vector2( Mathf.RoundToInt(_portalTransform.position.x + 1), Mathf.RoundToInt(_portalTransform.position.z)));
    }

    private void GenerateItems(Vector2 playerV2Pos, GameObject itemContainer)
    {
        for(int i = 0; i < _amountOfItems; i++)
        {
            Vector2 itemPos;
            Stack<(int, int)> pathToPlayer;
            Vector3 itemPosv3;

            do
            {
                itemPos = new Vector2(
                    Random.Range((int)1, (int)gridWidth - 1),
                    Random.Range(1, (int)gridHeight - 1)
                );
                pathToPlayer = AStar.AStarPathfinding(playerV2Pos, itemPos, this);
                itemPosv3 = new Vector3(itemPos.x, wallYPosition, itemPos.y);
            }
            while (IsBlocked((int)itemPos.x, (int)itemPos.y) || pathToPlayer == null || pathToPlayer.Count <= 0 || noSpawnPositionsArray.Contains(itemPosv3)
                || itemPosv3 == _enemyTransform.position || itemPos == playerV2Pos);

            mapGrid[(int)itemPos.x][(int)itemPos.y] = Instantiate(itemsPrefab, new Vector3(itemPos.x, itemsPrefab.transform.position.y, itemPos.y), Quaternion.identity);

            mapGrid[(int)itemPos.x][(int)itemPos.y].transform.parent = itemContainer.transform;
            itemsPosition.Add(itemPos);
        }
    }

    void Update()
    {
        
    }

     void GenerateFakeGoals(Vector2 inicialFakePos)
    {
        // Create fake goals
        int counter = 0;
        Vector3 fakeStartPos = new Vector3(inicialFakePos.x, 0, inicialFakePos.y);//To make sure at least one fake pos starts at 0,0
        while (counter < numberOfFakeGoals)
        {
            Vector3 fakeGoalPos = new Vector3(Random.Range((int)1, (int)gridWidth - 1), wallYPosition , Random.Range(1, (int)gridHeight - 1));
            counter++;
            GeneratePath(fakeStartPos, fakeGoalPos);
            fakeStartPos = new Vector3(Random.Range((int)1, (int)gridWidth - 1), Random.Range(1, (int)gridHeight - 1));
        }
    }

    void GenerateGrid()
    {
        Vector3 position = new Vector3(0, wallYPosition, 0);
        GameObject wallContainer = new GameObject();
        wallContainer.name = "Wall Container";
        GameObject wallInstance;
        bool withinBounds;
        float randomValue;

        // Loop through the grid positions
        for (int x = 0; x < gridWidth; x++)
        {
            position.z = 0.0f;
            for (int y = 0; y < gridHeight; y++)
            {
                if (noSpawnPositionsSet.Contains(position))
                {
                    position.z += defaultWallSize.z;
                    continue;
                }
                withinBounds = x >= 0 && y >= 0 && y <= gridHeight - 1 && x <= gridWidth - 1;
                randomValue = Random.Range(0.0f, 100.0f);
                if (randomValue <= renderProbability && withinBounds)
                {
                    wallInstance = Instantiate(defaultWallPrefab, position, Quaternion.identity);
                    wallInstance.transform.parent = wallContainer.transform;
                    mapGrid[x][y] = wallInstance;
                }
                position.z += defaultWallSize.z;
            }
            position.x += defaultWallSize.x;
        }
        GameObject bordersContainer = new GameObject("Borders");
        Vector3 originalScale = defaultWallPrefab.transform.localScale;

        // Left border
        Vector3 leftPos = new Vector3(-defaultWallSize.x * 0.5f, wallYPosition, (gridHeight - 1) * defaultWallSize.z * 0.5f);
        GameObject leftWall = Instantiate(defaultWallPrefab, leftPos, Quaternion.identity, bordersContainer.transform);
        leftWall.transform.localScale = new Vector3(defaultWallSize.x, originalScale.y, defaultWallSize.z * (gridHeight + 1));
        var leftMat = leftWall.GetComponent<Renderer>().material;
        leftMat.mainTextureScale *= new Vector2(gridHeight + 1, 1);

        // Right border
        Vector3 rightPos = new Vector3((gridWidth - 0.5f) * defaultWallSize.x, wallYPosition, (gridHeight - 1) * defaultWallSize.z * 0.5f);
        GameObject rightWall = Instantiate(defaultWallPrefab, rightPos, Quaternion.identity, bordersContainer.transform);
        rightWall.transform.localScale = new Vector3(defaultWallSize.x, originalScale.y, defaultWallSize.z * (gridHeight + 1));
        var rightMat = rightWall.GetComponent<Renderer>().material;
        rightMat.mainTextureScale *= new Vector2(gridHeight + 1, 1);

        // Top border
        Vector3 topPos = new Vector3((gridWidth - 1) * defaultWallSize.x * 0.5f, wallYPosition, gridHeight * defaultWallSize.z);
        GameObject topWall = Instantiate(defaultWallPrefab, topPos, Quaternion.identity, bordersContainer.transform);
        topWall.transform.localScale = new Vector3(defaultWallSize.x * gridWidth, originalScale.y, defaultWallSize.z);
        var topMat = topWall.GetComponent<Renderer>().material;
        topMat.mainTextureScale *= new Vector2(gridWidth + 1, 1);

        // Bottom border
        Vector3 bottomPos = new Vector3((gridWidth - 1) * defaultWallSize.x * 0.5f, wallYPosition, -defaultWallSize.z);
        GameObject bottomWall = Instantiate(defaultWallPrefab, bottomPos, Quaternion.identity, bordersContainer.transform);
        bottomWall.transform.localScale = new Vector3(defaultWallSize.x * gridWidth, originalScale.y, defaultWallSize.z);
        var bottomMat = bottomWall.GetComponent<Renderer>().material;
        bottomMat.mainTextureScale *= new Vector2(gridWidth + 1, 1);
    }

    public bool IsBlocked(int x, int y)
    {
        // Check if x or y are out of bounds; if so, consider the position blocked.
        if (x < 0 || x >= gridWidth ||
            y < 0 || y >= gridHeight)
        {
            return true;
        }


        if(!_mapCreated)
            return false;

        // If the cell is not null and its not item, its considered blocked
        return mapGrid[x][y] != null && !mapGrid[x][y].CompareTag("Item");
    }

    void OptimizeMap()
    {
        // Returns a tuple (minPosX, maxPosX, minPosY, maxPosY, area)
        (int minPosX, int maxPosX, int minPosY, int maxPosY, int area) MaximalRectangle(bool[][] matrix)
        {
            int numRows = matrix.Length;
            if (numRows == 0)
                return (0, 0, 0, 0, 0);

            int numColumns = matrix[0].Length;
            // Array to store histogram heights for each column
            int[] heights = new int[numColumns];

            // Global best rectangle (initially with area 0)
            (int minPosX, int maxPosX, int minPosY, int maxPosY, int area) globalBest = (0, 0, 0, 0, 0);

            // Process each row to update the histogram and find the best rectangle ending at that row.
            for (int r = 0; r < numRows; r++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    // If the current cell is true, increment the histogram; otherwise, reset to 0.
                    heights[j] = matrix[r][j] ? heights[j] + 1 : 0;
                }

                // Get the best rectangle for the current row
                var bestForRow = LargestRectangleArea(heights, r);
                if (bestForRow.area > globalBest.area)
                {
                    globalBest = bestForRow;
                }
            }

            return globalBest;
        }

        // Computes the largest rectangle in the histogram represented by heights
        (int minPosX, int maxPosX, int minPosY, int maxPosY, int area) LargestRectangleArea(int[] heights, int currentRow)
        {
            int n = heights.Length;
            int maxArea = 0;
            (int minPosX, int maxPosX, int minPosY, int maxPosY, int area) bestResult = (0, 0, 0, 0, 0);

            Stack<int> stack = new Stack<int>();
            int[] leftBoundary = new int[n];
            int[] rightBoundary = new int[n];

            // Initialize right boundaries to the end of the array.
            for (int i = 0; i < n; i++)
            {
                rightBoundary[i] = n;
            }

            // Compute left and right boundaries for each bar in the histogram.
            for (int i = 0; i < n; i++)
            {
                while (stack.Count > 0 && heights[stack.Peek()] >= heights[i])
                {
                    rightBoundary[stack.Pop()] = i;
                }
                leftBoundary[i] = (stack.Count == 0) ? -1 : stack.Peek();
                stack.Push(i);
            }

            // Compute the area for each bar and update the best rectangle if necessary.
            for (int i = 0; i < n; i++)
            {
                int width = rightBoundary[i] - leftBoundary[i] - 1;
                int area = heights[i] * width;
                if (area > maxArea)
                {
                    maxArea = area;
                    // Boundaries in the x-direction (columns)
                    int minPosX = leftBoundary[i] + 1;
                    int maxPosX = rightBoundary[i] - 1;
                    // Boundaries in the y-direction (rows)
                    int minPosY = currentRow - heights[i] + 1;
                    int maxPosY = currentRow;

                    bestResult = (minPosX, maxPosX, minPosY, maxPosY, area);
                }
            }

            return bestResult;
        }

        // Compute and replace by max retangle
        bool[][] occupancyMatrix = new bool[gridHeight][];
        for (int y = 0; y < gridHeight; y++)
        {
            occupancyMatrix[y] = new bool[gridWidth];
            for (int x = 0; x < gridWidth; x++)
            {
                occupancyMatrix[y][x] = mapGrid[x][y] != null;
            }
        }
        bool HasOccupiedCell()
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (occupancyMatrix[y][x])
                        return true;
                }
            }
            return false;
        }
        // Create a new GameObject to hold optimized walls.
        GameObject newWallContainer = new GameObject();
        newWallContainer.name = "New Walls";
        while (HasOccupiedCell())
        {
            var rectangle = MaximalRectangle(occupancyMatrix);
            //Debug.Log($"Maximal Rectangle: X[{rectangle.minPosX},{rectangle.maxPosX}], Y[{rectangle.minPosY},{rectangle.maxPosY}], Area: {rectangle.area}");

            Vector3 originalScale = defaultWallPrefab.transform.localScale;

            float distanceX = rectangle.maxPosX - rectangle.minPosX;
            float distanceY = rectangle.maxPosY - rectangle.minPosY;
            
            Vector3 scaledWallScale = new Vector3(originalScale.x * (distanceX + 1), originalScale.y, originalScale.z * (distanceY + 1));
            defaultWallPrefab.transform.localScale = scaledWallScale;
           
            GameObject scaledWall = Instantiate( defaultWallPrefab, new Vector3(rectangle.minPosX + distanceX / 2f, wallYPosition, rectangle.minPosY + distanceY / 2f), Quaternion.identity);
            scaledWall.transform.parent = newWallContainer.transform;

            // Adjust the materials texture scale to avoid stretching.
            Material wallMaterial = scaledWall.GetComponent<Renderer>().material;
            if (wallMaterial == null)
            {
                Debug.LogError("Could not find the object material");
            }
            else
            {
                wallMaterial.mainTextureScale *= new Vector2(distanceX + 1, 1);
            }

            // Restore the original scale of the prefab.
            defaultWallPrefab.transform.localScale = originalScale;

            //Debug.Log($"{scaledWallScale}, {rectangle.maxPosX - rectangle.minPosX} -- {rectangle.maxPosX} -> {rectangle.minPosX}");

            // Paint the maximal rectangle by replacing wall objects in mapGrid with the new scaled wall.
            for (int x = rectangle.minPosX; x <= rectangle.maxPosX; x++)
            {
                for (int y = rectangle.minPosY; y <= rectangle.maxPosY; y++)
                {
                    if (mapGrid[x][y] != null)
                    {
                        Destroy(mapGrid[x][y]);
                        mapGrid[x][y] = scaledWall;
                        occupancyMatrix[y][x] = false;
                    }
                }
            }
        }
    }

    private void GeneratePath(Vector3 start, Vector3 goal)
    {
        Vector3 currentPosition = new Vector3(start.x, wallYPosition, start.z);
        noSpawnPositionsSet.Add(currentPosition);

        while (currentPosition.x != goal.x || currentPosition.z != goal.z)
        {
            int deltaX = (int)(goal.x - currentPosition.x);
            int deltaZ = (int)(goal.z - currentPosition.z);

            bool canMoveX = deltaX != 0;
            bool canMoveZ = deltaZ != 0;

            if (canMoveX && canMoveZ)
            {
                if (Random.Range(0, 2) == 0)
                {
                    currentPosition.x += Mathf.Sign(deltaX);
                }
                else
                {
                    currentPosition.z += Mathf.Sign(deltaZ);
                }
            }
            else if (canMoveX)
            {
                currentPosition.x += Mathf.Sign(deltaX);
            }
            else if (canMoveZ)
            {
                currentPosition.z += Mathf.Sign(deltaZ);
            }

            Vector3 pathPosition = new Vector3(currentPosition.x, wallYPosition, currentPosition.z);
            noSpawnPositionsSet.Add(pathPosition);
        }
    }

    // Returns the size of the prefab by instantiating it temporarily.
    Vector3 GetGameObjectPrefabSize(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab);
        instance.name = "TestInstance";
        Renderer prefabRenderer;
        if (!instance.gameObject.TryGetComponent<Renderer>(out prefabRenderer))
        {
            Debug.LogError("Could not get renderer from the prefab; returning invalid Vector3");
            return new Vector3();
        }
        Destroy(instance);
        return prefabRenderer.bounds.size;
    }

    public GameObject SpawnGameObjectOnGrid(GameObject prefab, Vector3 position)
    {
        GameObject instance;
        // Estimate grid coordinates based on position.
        int x = Mathf.RoundToInt(position.x / defaultWallSize.x);
        int z = Mathf.RoundToInt(position.z / defaultWallSize.z);
        position.x = x;
        position.z = z;
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight)
        {
            Debug.LogWarning("Invalid position to spawn");
            return null;
        }
        if (mapGrid[x][z] != null)
        {
            Debug.LogWarning("Position already occupied");
            return null;
        }
        instance = Instantiate(prefab, position, prefab.transform.rotation);
        mapGrid[x][z] = instance;
        //Debug.Log("Spawned at X: " + x + " Z: " + z);
        return instance;
    }

    public bool RemoveObjectFromMap(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / defaultWallSize.x);
        int z = Mathf.RoundToInt(position.z / defaultWallSize.z);
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight)
        {
            Debug.LogWarning("Invalid position to remove");
            return true;
        }
        if (mapGrid[x][z] == null)
        {
            Debug.LogWarning("Position already empty");
            return false;
        }
        Destroy(mapGrid[x][z]);
        mapGrid[x][z] = null;
        return true;
    }
}
