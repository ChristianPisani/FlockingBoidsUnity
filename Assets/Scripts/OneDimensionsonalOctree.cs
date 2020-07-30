using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public struct OneDimensionalOctree {
    public int Capacity;
    public Bounds Bounds;
    public NativeArray<Boid> Points;
    public NativeArray<Octree<Boid>> Subdivisions;

    bool Subdivided;

    
}
