using System;
using System.Collections.Generic;
using UnityEngine;


// Class for a priority queue, which will be used in the Dijkstra's algorithm / enemy pathfinding. 
// Current implementation runs in O(n log n) and is not final iteration, just a placeholder for testing purposes.
public class PriorityQueue : MonoBehaviour
{ 
    private List<(Node node, int priority)> heap = new List<(Node node, int priority)>();

    // Enqueues a node with a given priority.
    public void Enqueue(Node node, int priority)
    {
        heap.Add((node, priority));
        heap.Sort((a, b) => a.priority.CompareTo(b.priority));
    }

    // Dequeues a node with the highest priority (lowest priority value).
    public Node Dequeue()
    {   
        if (IsEmpty())
            throw new InvalidOperationException("Queue is empty.");

        var best = heap[0];
        heap.RemoveAt(0);
        return best.node;
    }

    // Updates the priority of a given node.
    public void UpdatePriority(Node node, int newPriority)
    {   
        int index = heap.FindIndex(i => i.node == node);
        if (index == -1)
            throw new InvalidOperationException("Node not found in the queue.");

        heap[index] = (node, newPriority);
        heap.Sort((a, b) => a.priority.CompareTo(b.priority));
    }

    // Clears the priority queue.
    public void Clear()
    {
        heap.Clear();
    }

    // Checks if the queue contains a given node.
    public bool IsEmpty()
    {
        return heap.Count == 0;
    }
}
