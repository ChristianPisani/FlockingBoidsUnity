using Assets.Scripts.ECS;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BoidTargetGameOject : MonoBehaviour {
    public Mesh RenderMesh;
    public float Strength;
    public float PullDistance;
    public float PushDistance;

    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var target = new BoidTarget()
        {
            Strength = Strength,
            PullDistance = PullDistance,
            PushbackDistance = PullDistance,
            Pos = transform.position
        };

        var targetArcheType = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(MoveComponent),
            typeof(BoidTarget)
        );

        var targetEntity = entityManager.CreateEntity(targetArcheType);
        entityManager.AddComponentData(targetEntity, target);
        entityManager.AddComponentData(targetEntity, new Translation() { Value = new float3(transform.position.x, transform.position.y, transform.position.z) });
    }
}
