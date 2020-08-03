using Unity.Entities;
using UnityEngine;

public struct MoveComponent : IComponentData
{
    public Vector3 Vel;
    public Vector3 Acl;
}
