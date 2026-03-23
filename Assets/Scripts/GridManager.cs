using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    public LayerMask[] unwalkableMasks;
    public Vector2 gridWorldSize;
    [SerializeField] private float nodeRadius;
    Node[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    

    void Start()
    {
        nodeDiameter = nodeRadius * 2;

        // Sets the grid x, y size
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        // Makes the grid
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];

        // Gets the player position
        Vector2 gridPlayer = new Vector2(Mathf.RoundToInt(player.position.x), Mathf.RoundToInt(player.position.y));
        // Gets the bottom left of the grid
        Vector2 gridBottomLeft = gridPlayer - Vector2.right * Mathf.RoundToInt(gridWorldSize.x / 2) - Vector2.up * Mathf.RoundToInt(gridWorldSize.y / 2);

        // Go through the grid X and Y size and add nodes 
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = gridBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);
                // To change not physics based but tileset based
                //bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius));
/*                 if(Tilemap.GetTile() == )
                {
                    
                } */
                bool walkable = true;
                grid[x, y] = new Node(walkable, worldPoint);
            }
        }
    }

    /**
    * Draws the grid of nodes.
    */
    void OnDrawGizmos()
    {
        Vector2 gridSize = new Vector2(gridWorldSize.x, gridWorldSize.y);
        Gizmos.DrawWireCube(transform.position, gridSize);
        Vector2 nodeSize = new Vector2(nodeDiameter, nodeDiameter);

        if (grid != null)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = Color.red;
                //Gizmos.DrawCube(n.gridSizeX, n.gridSizeY);
                Gizmos.DrawWireCube(n.worldPosition, nodeSize);
                //.Log("Drew cube");
            }
        }
        else
        {
            //Debug.log("x"); 
        }

    }

    public void Dijkstras()
    {

        //PriorityQueue<Node, int> queue = new();

    }
}
