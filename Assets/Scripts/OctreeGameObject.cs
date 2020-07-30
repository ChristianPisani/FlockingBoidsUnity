using Assets.Scripts;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class OctreeGameObject : MonoBehaviour
{
    public int Capacity = 5;
    public int AmountOfPoints = 150;

    BoxCollider Bounds;

    [HideInInspector]
    public Octree<Boid> Octree;

    void Start()
    {
        Bounds = GetComponent<BoxCollider>();

        Octree = new Octree<Boid>(Capacity, Bounds.bounds);

        InsertRandomPoints();
    }

    private void OnDrawGizmos()
    {
        return;

        if (Octree.Equals(default(Octree<Boid>))) return;

        //Octree = new Octree(Capacity, Bounds.bounds);

        //InsertRandomPoints();

        Octree.Draw();
    }

    void InsertRandomPoints()
    {
        for (int i = 0; i < AmountOfPoints; i++)
        {
            var point = Octree.Bounds.center.RandomPoint(Octree.Bounds.size);
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = point
            });
        }
    }
}
