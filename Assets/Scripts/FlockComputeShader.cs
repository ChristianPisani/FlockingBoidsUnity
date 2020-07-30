using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FlockComputeShader : MonoBehaviour {
    public GameObject Boid;
    MeshFilter MeshFilter;
    Renderer Renderer;

    public int Amount;

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

    public ComputeShader ComputeShader;

    List<Boid> Boids;
    BoxCollider Bounds;

    public static Octree<Boid> Octree;

    public void Start()
    {
        this.Bounds = GetComponent<BoxCollider>();
        MeshFilter = Boid.GetComponentInChildren<MeshFilter>();
        Renderer = Boid.GetComponentInChildren<Renderer>();

        InvokeRepeating("CreateOctree", 0, 1);

        Reset();
    }

    public void Reset()
    {
        Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

        Boids = new List<Boid>();

        for (int i = 0; i < Amount; i++)
        {
            Vector3 rndCoords = transform.position.RandomPoint(Bounds.bounds.size * 0.1f);

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

            Boids.Add(boid);
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = boid.Pos,
                AttachedObject = boid
            });
        }
    }

    public void Compute()
    {
        /*
        var boidBuffer = new ComputeBuffer(Boids.Count, BoidData.Size);
        boidBuffer.SetData(boidData);

        ComputeShader.SetBuffer(0, "boids", boidBuffer);
        ComputeShader.SetInt("numBoids", boids.Length);
        ComputeShader.SetFloat("viewRadius", settings.perceptionRadius);
        ComputeShader.SetFloat("avoidRadius", settings.avoidanceRadius);

        int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
        ComputeShader.Dispatch(0, threadGroups, 1, 1);

        boidBuffer.GetData(boidData);

        for (int i = 0; i < boids.Length; i++)
        {
            boids[i].avgFlockHeading = boidData[i].flockHeading;
            boids[i].centreOfFlockmates = boidData[i].flockCentre;
            boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
            boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

            boids[i].UpdateBoid();
        }

        boidBuffer.Release();
        */
    }

    public void Update()
    {

        //var ShaderData = CreateShaderData();

        //UpdateBoids();

        //Draw();
    }

    public void CreateOctree()
    {
        Octree = new Octree<Boid>(OctreeCapacity, Bounds.bounds);

        foreach(var boid in Boids)
        {
            Octree.InsertPoint(new OctreeData<Boid>()
            {
                Point = boid.Pos,
                AttachedObject = boid
            });
        }
    }

    public List<BoidShaderData> CreateShaderData()
    {
        var shaderData = new List<BoidShaderData>();

        for (int i = 0; i < Boids.Count; i++)
        {
            var boid = Boids[i];

            var inRange = Octree
                  .Query(new Bounds(boid.Pos, Vector3.one * boid.PerceptionRadius), MaxNeighBours);

            boid = ConstrainToBounds(boid);

            Boids[i] = boid;

            shaderData.Add(new BoidShaderData()
            {
                Boid = boid,
                InRange = inRange
            });
        }

        return shaderData;
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

        for (int i = 0; i < Boids.Count; i += drawAmount)
        {
            Graphics.DrawMeshInstanced(MeshFilter.sharedMesh,
                0,
                Renderer.sharedMaterial,
                Boids.Skip(i).Take(drawAmount).Select(x => x.matrix(
                    Renderer.transform.rotation, Boid.transform.localScale)).ToArray(),
                drawAmount < Boids.Count ? drawAmount : Boids.Count - 1);
        }
    }
}

public struct BoidShaderData {
    public Boid Boid;
    public List<OctreeData<Boid>> InRange;
}
