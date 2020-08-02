using Assets.Scripts;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class FlockSpawner : MonoBehaviour {
    public Mesh Mesh;
    public Material Material;

    public int Amount;

    public int MaxNeighBours = 12;
    public int OctreeCapacity = 5;
    public float MaxSpeed = 5f;
    public float MinSpeed = 1f;
    public float MaxForce = 5f;
    public float PerceptionRadius = 2f;
    public float SpawnBoundsPercentage = 0.25f;

    public float BoidScale = 0.1f;

    public bool Debugging = false;
    public bool UseOctree = true;

    public float AvgSpeedMod = 1f;
    public float CohesionMod = 1f;
    public float SeparationMod = 1f;

    private NativeArray<BoidComponent> Boids;

    private EntityArchetype BoidArcheType;

    private BoxCollider _collider;
    [HideInInspector]
    public Bounds Bounds
    {
        get
        {
            return _collider.bounds;
        }
    }

    private EntityManager entityManager;

    public void Start()
    {
        GetComponents();
        SetupEntityManager();
        SpawnBoids();
    }

    void SetupEntityManager()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        BoidArcheType = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Scale),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(BoidComponent),
            typeof(HeadingComponent),
            typeof(MoveComponent)
        );
    }

    void GetComponents()
    {
        _collider = GetComponent<BoxCollider>();
    }

    public void SpawnBoids()
    {
        var boids = new List<BoidComponent>();

        for (int i = 0; i < Amount; i++)
        {
            var boid = CreateBoid();
            boids.Add(boid);
            CreateBoidEntity(boid);
        }

        Boids = new NativeArray<BoidComponent>(boids.ToArray(), Allocator.Persistent);
    }

    public BoidComponent CreateBoid()
    {
        var boid = new BoidComponent()
        {
            PerceptionRadius = PerceptionRadius,
            MaxForce = MaxForce,
            MaxSpeed = MaxSpeed,
            MinSpeed = MinSpeed,
            AvgSpeedMod = AvgSpeedMod,
            CohesionMod = CohesionMod,
            SeparationMod = SeparationMod
        };

        return boid;
    }

    private void CreateBoidEntity(BoidComponent boid)
    {
        var boidEntity = entityManager.CreateEntity(BoidArcheType);

        var rndPos = transform.position.RandomPoint(Bounds.size * SpawnBoundsPercentage);

        entityManager.AddComponentData(boidEntity, new Translation()
        {
            Value = rndPos
        });

        entityManager.AddComponentData(boidEntity, new Scale()
        {
            Value = BoidScale
        });

        entityManager.AddSharedComponentData(boidEntity, new RenderMesh()
        {
            mesh = Mesh,
            material = Material
        });

        entityManager.AddComponentData(boidEntity, new MoveComponent()
        {
            Speed = Random.Range(MinSpeed, MaxSpeed)
        });

        entityManager.AddComponentData(boidEntity, new HeadingComponent()
        {
            Value = Random.onUnitSphere
        });

        entityManager.AddSharedComponentData(boidEntity, boid);
    }

    private void OnDestroy()
    {
        Boids.Dispose();
    }
}
