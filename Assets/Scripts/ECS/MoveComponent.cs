using Unity.Entities;
using Unity.Mathematics;

public struct MoveComponent : IComponentData
{
    public float3 Vel;
    public float3 Acl;
}
