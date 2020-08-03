using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS {
    struct BoidTarget : IComponentData {
        public float3 Pos;
    }
}
