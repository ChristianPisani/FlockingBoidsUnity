using Assets.Scripts;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class OctreeGameObject : MonoBehaviour
{
    public int Capacity = 5;
    public int AmountOfPoints = 150;

    BoxCollider Bounds;

    [HideInInspector]
    public Octree<object> Octree;

    void Start()
    {
        Bounds = GetComponent<BoxCollider>();

        Octree = new Octree<object>(Capacity, Bounds.bounds);

        InsertRandomPoints();
    }

    private void OnDrawGizmos()
    {
        if (Octree == null) return;

        //Octree = new Octree(Capacity, Bounds.bounds);

        //InsertRandomPoints();

        Octree.Draw();
    }

    void InsertRandomPoints()
    {
        for (int i = 0; i < AmountOfPoints; i++)
        {
            var point = Octree.Bounds.center.RandomPoint(Octree.Bounds.size);
            Octree.InsertPoint(new OctreeData<object>()
            {
                Point = point
            });
        }
    }
}
