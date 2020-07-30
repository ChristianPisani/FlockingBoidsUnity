using Assets.Scripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct FlockJob : IJobParallelFor {
    public NativeArray<Boid> Boids;
    
    public int MaxNeighBours;
    public int OctreeCapacity;
    public Bounds Bounds;

    public Quaternion ModelRotation;
    public Vector3 Scale;

    [BurstCompile(CompileSynchronously = false)]
    public void Execute(int index)
    {        
        var boid = Boids[index];

        var inRange = Flock.Octree
            .Query(new Bounds(boid.Pos, Vector3.one * boid.PerceptionRadius), MaxNeighBours);

        boid.Acl += boid.SteeringForce(inRange);        
        
        Boids[index] = boid;
    }    
}
