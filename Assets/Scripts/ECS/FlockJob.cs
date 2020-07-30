using Assets.Scripts;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct FlockJob : IJobParallelFor {
    public NativeArray<Boid> Boids;
    
    public int OctreeCapacity;
    public Bounds Bounds;

    public void Execute(int index)
    {        
        var boid = Boids[index];

        var inRange = Flock.Octree
            .Query(new Bounds(boid.Pos, Vector3.one * boid.PerceptionRadius));

        boid.Acl += boid.SteeringForce(inRange);

        Boids[index] = boid;
    }    
}
