using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS {
    struct BoidTarget : IComponentData {
        public float3 Strength;
        public float3 Pos;
        public float PushbackDistance;
        public float PullDistance;
        public bool Push;
    }
}
