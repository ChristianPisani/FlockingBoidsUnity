using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Flock : MonoBehaviour {
    public GameObject Boid;
    MeshFilter MeshFilter;
    Renderer Renderer;

    public int BoidUpdateBatchSize = 1000;
    int _boidUpdateBatchNum = 0;

    public int MaxNeighBours = 12;
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

    NativeArray<Boid> Boids;
    List<Boid> TempBoids;
    BoxCollider Bounds;
    public Color Color;

    public static Octree<Boid> Octree { get; private set; }

    public int Amount;

    public bool UseECS = true;

    public void Start()
    {
        this.Bounds = GetComponent<BoxCollider>();
        MeshFilter = Boid.GetComponentInChildren<MeshFilter>();
        Renderer = Boid.GetComponentInChildren<Renderer>();

        InvokeRepeating(nameof(CreateOctree), 0, 4);

        Reset();
    }

    public void Reset()
    {
        Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

        var boids = new List<Boid>();

        for (int i = 0; i < Amount; i++)
        {
            Vector3 rndCoords = transform.position.RandomPoint(Bounds.bounds.size);

            var boid = new Boid()
            {
                Pos = rndCoords,
                PerceptionRadius = PerceptionRadius,
                MaxForce = MaxForce,
                MaxSpeed = MaxSpeed,
                MinSpeed = MinSpeed,
                Acl = Vector3.zero,
                Vel = Vector3.zero,
                AvgSpeedMod = AvgSpeedMod,
                CohesionMod = CohesionMod,
                SeparationMod = SeparationMod
            };

            boid.Spawn();

            boids.Add(boid);
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = boid.Pos,
                AttachedObject = boid
            });
        }

        Boids = new NativeArray<Boid>(boids.ToArray(), Allocator.Persistent);
    }

    public void Update()
    {
        UpdateBoids();
        Draw();
    }

    public void CreateOctree()
    {
        Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

        foreach (var boid in Boids)
        {
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = boid.Pos,
                AttachedObject = boid
            });
        }
    }

    public void UpdateBoids()
    {
        if (UseECS)
        {
            for (int i = 0; i < Boids.Length; i++)
            {
                var boid = Boids[i];

                boid.AvgSpeedMod = AvgSpeedMod;
                boid.CohesionMod = CohesionMod;
                boid.SeparationMod = SeparationMod;

                //boid.CurPos = Vector3.Lerp(boid.CurPos, boid.Pos, 1f / (Amount / BoidUpdateBatchSize));

                Boids[i] = boid;
            }

            ExecuteJob();
        }
        else
        {
            /*
            Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

            var boidCopies = new List<Boid>();

            for (int i = 0; i < Boids.Length; i++)
            {
                var boid = Boids[i];

                boid.AvgSpeedMod = AvgSpeedMod;
                boid.CohesionMod = CohesionMod;
                boid.SeparationMod = SeparationMod;

                Octree.InsertPoint(new OctreeData<Boid>()
                {
                    Point = boid.Pos,
                    AttachedObject = boid
                });

                boidCopies.Add(boid);

                Boids[i] = boid;
            }

            for (int i = 0; i < Boids.Count; i++)
            {
                var boid = Boids[i];

                if (UseOctree)
                {
                    var inRange = Octree
                        .Query(new Bounds(boid.Pos, Vector3.one * boid.PerceptionRadius), MaxNeighBours);

                    boid.Acl += boid.SteeringForce(inRange);
                }
                else
                {
                    boid.Acl += boid.SteeringForce(boidCopies);
                }

                boid = ConstrainToBounds(boid);

                Boids[i] = boid;
            }
            */
        }
    }

    public void ExecuteJob()
    {
        /*if (_boidUpdateBatchNum == 0)
        {
            TempBoids = boids.ToList();
        }

        _boidUpdateBatchNum++;
        if (_boidUpdateBatchNum * BoidUpdateBatchSize >= Boids.Length - 1)
        {
            _boidUpdateBatchNum = 0;
        }

        var nativeBoids = new NativeArray<Boid>(
            TempBoids
            .Skip(_boidUpdateBatchNum * BoidUpdateBatchSize)
            .Take(BoidUpdateBatchSize)
            .ToArray(), Allocator.TempJob);*/

        var flockJob = new FlockJob()
        {
            Boids = Boids,
            OctreeCapacity = OctreeCapacity,
            Bounds = Bounds.bounds,
            MaxNeighBours = MaxNeighBours,
            Scale = Boid.transform.localScale,
            ModelRotation = MeshFilter.transform.rotation,
            DeltaTime = Time.deltaTime
        };

        var jobHandle = flockJob.Schedule(Boids.Length, 128);
        jobHandle.Complete();

        for (int i = 0; i < Boids.Length; i++)
        {
            var boid = Boids[i];

            boid = ConstrainToBounds(boid);

            Boids[i] = boid;
        }
    }

    private void OnDestroy()
    {
        Boids.Dispose();
    }

    public Boid ConstrainToBounds(Boid boid)
    {
        if (!Bounds.bounds.Contains(boid.Pos))
        {
            var dir = boid.Vel;

            var rayCast = Physics.Raycast(
                boid.Pos - dir * 10000f,
                dir,
                out var hitInfo,
                float.PositiveInfinity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (rayCast)
            {
                boid.Pos = Bounds.bounds.center;
            }

            boid.Pos = hitInfo.point;

            if (!Bounds.bounds.Contains(boid.Pos))
            {
                boid.Pos = Bounds.bounds.center;
            }
        }

        return boid;
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

    public void Draw()
    {
        int drawAmount = 500;

        for (int i = 0; i < Boids.Length; i += drawAmount)
        {
            Graphics.DrawMeshInstanced(MeshFilter.sharedMesh,
                0,
                Renderer.sharedMaterial,
                Boids.Skip(i).Take(drawAmount).Select(x => x.matrix(
                    Renderer.transform.rotation, Boid.transform.localScale)).ToArray(),
                drawAmount < Boids.Length ? drawAmount : Boids.Length - 1);
        }

        if (Debugging)
        {
            Octree.Draw();
        }
    }
}
