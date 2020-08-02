using Unity.Entities;
using Unity.Mathematics;

public struct BoidComponent : ISharedComponentData  {
    // ECS
    // Only store data in components
    // System manage components
    
    public float AvgSpeedMod;
    public float CohesionMod;
    public float SeparationMod;

    public float MaxSpeed;
    public float MinSpeed;
    public float MaxForce;
    public float PerceptionRadius;
}
