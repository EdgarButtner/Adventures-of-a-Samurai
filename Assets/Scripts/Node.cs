using UnityEngine;

public class Node
{
    public bool walkable;
    public Vector2 worldPosition;

    public Node(bool _walkable, Vector2 _worldPos) 
    {
        walkable = _walkable;
        worldPosition = _worldPos;
    }



}
