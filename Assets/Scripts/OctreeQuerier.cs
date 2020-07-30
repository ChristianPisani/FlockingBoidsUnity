using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OctreeQuerier : MonoBehaviour
{
    public OctreeGameObject Octree;

    BoxCollider Bounds;
    List<Vector3> Points;

    void Start()
    {
        Bounds = GetComponent<BoxCollider>();
        Points = new List<Vector3>();
    }

    void Update()
    {
        Points = Octree.Octree.Query(Bounds.bounds, int.MaxValue)
                              .Select(x => x.Point)
                              .ToList();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        foreach (var point in Points) {
            Gizmos.DrawSphere(point, 0.1f);
        }
    }
}
