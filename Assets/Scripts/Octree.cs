using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Octree<T> {
    public int Capacity = 5;
    public Bounds Bounds;
    public List<OctreeData<T>> Points;
    public List<Octree<T>> Subdivisions;

    bool Subdivided = false;

    public Octree(int capacity, Bounds bounds)
    {
        Bounds = bounds;
        Capacity = capacity;
        Points = new List<OctreeData<T>>();
    }

    public bool InsertPoint(OctreeData<T> octreeData)
    {
        if (Bounds.Contains(octreeData.Point))
        {
            if (Points.Count < Capacity)
            {
                Points.Add(octreeData);
                return true;
            }
            else
            {
                if (!Subdivided)
                {
                    Subdivide();
                }

                return InsertPointIntoSubdivision(octreeData);
            }
        }

        return false;
    }

    private bool InsertPointIntoSubdivision(OctreeData<T> octreeData)
    {
        foreach (var subdivision in Subdivisions)
        {
            if (subdivision.InsertPoint(octreeData))
            {
                return true;
            }
        }

        return false;
    }

    protected void Subdivide()
    {
        Subdivisions = new List<Octree<T>>();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    var offset = new Vector3(
                        (Bounds.size.x / 4f) * (x == 0 ? -1 : 1),
                        (Bounds.size.y / 4f) * (y == 0 ? -1 : 1),
                        (Bounds.size.z / 4f) * (z == 0 ? -1 : 1)
                    );

                    var bounds = new Bounds()
                    {
                        size = Bounds.size / 2f,
                        center = Bounds.center + offset
                    };

                    var octree = new Octree<T>(Capacity, bounds);

                    Subdivisions.Add(octree);
                }
            }
        }

        Subdivided = true;
    }

    public List<OctreeData<T>> Query(Bounds bounds) 
    {
        var points = new List<OctreeData<T>>();

        if(bounds.Intersects(this.Bounds))
        {
            points.AddRange(this.Points);

            if (Subdivided)
            {
                foreach (var subdivision in Subdivisions)
                {
                    points.AddRange(subdivision.Query(bounds));
                }
            }
        }

        return points;
    }

    public void Draw()
    {        
        if (Bounds == null) return;

        Gizmos.color = new Color(0, 255, 0, 0.1f);
        Gizmos.DrawWireCube(Bounds.center, Bounds.size);

        if (Subdivisions != null)
        {
            foreach (var octree in Subdivisions)
            {
                octree.Draw();
            }
        }

        if (Points != null)
        {
            foreach (var point in Points)
            {
                Gizmos.color = Color.red;
                //Gizmos.DrawWireSphere(point, 0.1f);
            }
        }
    }
}
