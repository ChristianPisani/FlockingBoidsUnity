using Assets.Scripts;
using Assets.Scripts.ECS;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class FlockSpawner : MonoBehaviour {
    public Mesh Mesh;
    public Material Material;

    public int Amount;

    public float MaxSpeed = 5f;
    public float MinSpeed = 1f;
    public float PerceptionRadius = 2f;
    public float SpawnBoundsPercentage = 0.25f;

    public float BoidScale = 0.1f;

    public bool Debugging = false;
    
    public float AvgSpeedMod = 1f;
    public float CohesionMod = 1f;
    public float SeparationMod = 1f;
    
    private EntityArchetype BoidArcheType;

    public float MaxRotationDegrees = 10f;
    public float BaseRotationX, BaseRotationY, BaseRotationZ;

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
            typeof(Rotation),
            typeof(RotationComponent),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(BoidComponent),
            typeof(HeadingComponent),
            typeof(MoveComponent),
            typeof(VelocityLimiter)
        );
    }

    void GetComponents()
    {
        _collider = GetComponent<BoxCollider>();
    }

    public void SpawnBoids()
    {        
        for (int i = 0; i < Amount; i++)
        {
            var boid = CreateBoid();
            CreateBoidEntity(boid);
        }
    }

    public BoidComponent CreateBoid()
    {
        var boid = new BoidComponent()
        {
            PerceptionRadius = PerceptionRadius,
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

        entityManager.AddSharedComponentData(boidEntity, new RotationComponent()
        {
            MaxDegrees = MaxRotationDegrees,
            BaseRotation = Quaternion.Euler(BaseRotationX, BaseRotationY, BaseRotationZ)
        });

        entityManager.AddComponentData(boidEntity, new MoveComponent()
        {
            Acl = Random.onUnitSphere * Random.Range(MinSpeed, MaxSpeed)
        });

        entityManager.AddComponentData(boidEntity, new HeadingComponent()
        {
            Value = Random.onUnitSphere
        });

        entityManager.AddComponentData(boidEntity, new VelocityLimiter()
        {
            MinSpeed = MinSpeed,
            MaxSpeed = MaxSpeed
        });

        entityManager.AddSharedComponentData(boidEntity, boid);
    }
}
