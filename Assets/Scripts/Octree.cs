using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Octree {
    public int Capacity = 5;
    public Bounds Bounds;
    public List<Vector3> Points;
    public List<Octree> Subdivisions;

    bool Subdivided = false;

    public Octree(int capacity, Bounds bounds)
    {
        Bounds = bounds;
        Capacity = capacity;
        Points = new List<Vector3>();
    }

    public bool InsertPoint(Vector3 point)
    {
        if (Bounds.Contains(point))
        {
            if (Points.Count < Capacity)
            {
                Points.Add(point);
                return true;
            }
            else
            {
                if (!Subdivided)
                {
                    Subdivide();
                }

                return InsertPointIntoSubdivision(point);
            }
        }

        return false;
    }

    private bool InsertPointIntoSubdivision(Vector3 point)
    {
        foreach (var subdivision in Subdivisions)
        {
            if (subdivision.InsertPoint(point))
            {
                return true;
            }
        }

        return false;
    }

    protected void Subdivide()
    {
        Subdivisions = new List<Octree>();

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

                    var octree = new Octree(Capacity, bounds);

                    Subdivisions.Add(octree);
                }
            }
        }

        Subdivided = true;
    }

    public List<Vector3> Query(Bounds bounds) 
    {
        var points = new List<Vector3>();

        if(bounds.Intersects(this.Bounds))
        {
            points.AddRange(this.Points.Where(point => bounds.Contains(point)));

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

        Gizmos.color = Color.green;
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
