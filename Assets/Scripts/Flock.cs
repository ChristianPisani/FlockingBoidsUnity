using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Flock : MonoBehaviour {
    public GameObject Boid;
    public int OctreeCapacity = 5;
    public float MaxSpeed = 5f;
    public float MinSpeed = 1f;
    public float MaxForce = 5f;
    public float PerceptionRadius = 2f;
    public bool Debugging = false;
    public bool UseOctree = true;

    public float AvgSpeedMod = 1f;
    public float CohesionMod = 1f;
    public float SeparationMod = 1f;

    List<BoidComponent> BoidComponents;
    BoxCollider Bounds;
    public Color Color;

    public static Octree<Boid> Octree { get; private set; }
    
    public int Amount;

    public bool UseECS = true;

    public void Start()
    {
        this.Bounds = GetComponent<BoxCollider>();

        Reset();
    }

    public void Reset()
    {        
        if (BoidComponents != null)
        {
            foreach (var boid in BoidComponents)
            {
                Destroy(boid.gameObject);
            }
        }

        Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

        BoidComponents = new List<BoidComponent>();

        for (int i = 0; i < Amount; i++)
        {
            Vector3 rndCoords = transform.position.RandomPoint(Bounds.bounds.size);
             
            var boid = Instantiate(Boid, transform, true);
            boid.transform.position = rndCoords;

            var boidComponent = boid.GetComponent<BoidComponent>();
            boidComponent.Boid.PerceptionRadius = PerceptionRadius;
            boidComponent.Boid.MaxSpeed = MaxSpeed;
            boidComponent.Boid.MinSpeed = MinSpeed;
            boidComponent.Boid.MaxForce = MaxForce;
            boidComponent.Spawn();

            BoidComponents.Add(boidComponent);
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = boidComponent.Boid.Pos,
                AttachedObject = boidComponent.Boid
            });
        }
    }

    public void Update()
    {                
        if (UseECS)
        {            
            ExecuteJob(BoidComponents.Select(x => x.Boid).ToArray());
        } else
        {
            Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

            var boidCopies = new List<Boid>();

            foreach (var boid in BoidComponents)
            {
                boid.Boid.AvgSpeedMod = AvgSpeedMod;
                boid.Boid.CohesionMod = CohesionMod;
                boid.Boid.SeparationMod = SeparationMod;

                Octree.InsertPoint(new OctreeData<Boid>()
                {
                    Point = boid.Boid.Pos,
                    AttachedObject = boid.Boid
                });

                boidCopies.Add(boid.Boid);
            }

            foreach (BoidComponent boid in BoidComponents)
            {
                if (UseOctree)
                {
                    var inRange = Octree
                        .Query(new Bounds(boid.Boid.Pos, Vector3.one * boid.Boid.PerceptionRadius));

                    boid.Boid.Acl += boid.Boid.SteeringForce(inRange);
                }
                else
                {
                    boid.Boid.Acl += boid.Boid.SteeringForce(boidCopies);
                }
                boid.Boid.Update();

                ConstrainToBounds(boid);
            }
        }
    }

    public void ExecuteJob(Boid[] boids)
    {
        var nativeBoids = new NativeArray<Boid>(boids, Allocator.TempJob);
        
        var flockJob = new FlockJob()
        {
            Boids = nativeBoids,
            OctreeCapacity = OctreeCapacity,
            Bounds = Bounds.bounds
        };

        var jobHandle = flockJob.Schedule(nativeBoids.Length, 24);
        jobHandle.Complete();

        for(int i = 0; i < nativeBoids.Length; i++)
        {
            BoidComponents[i].Boid = nativeBoids[i];

            BoidComponents[i].Boid.AvgSpeedMod = AvgSpeedMod;
            BoidComponents[i].Boid.CohesionMod = CohesionMod;
            BoidComponents[i].Boid.SeparationMod = SeparationMod;

            ConstrainToBounds(BoidComponents[i]);
        }

        nativeBoids.Dispose();
    }

    public void ConstrainToBounds(BoidComponent boid)
    {
        if (!Bounds.bounds.Contains(boid.Boid.Pos))
        {
            var dir = boid.transform.forward;

            var rayCast = Physics.Raycast(
                boid.Boid.Pos - dir * 10000f,
                dir,
                out var hitInfo,
                float.PositiveInfinity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (!rayCast)
            {
                return;
            }
            else
            {
                boid.Boid.Pos = Bounds.bounds.center;
            }
            
            boid.Boid.Pos = hitInfo.point;

            if (!Bounds.bounds.Contains(boid.Boid.Pos))
            {
                boid.Boid.Pos = Bounds.bounds.center;
            }
        }
    }

    public void OnDrawGizmos()
    {
        if (Bounds == null || Octree.Equals(default(Octree<Boid>))) return;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, Bounds.bounds.size);

        if (Debugging)
        {
            Octree.Draw();
        }
    }
}
