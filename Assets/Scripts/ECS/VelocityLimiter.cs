using Unity.Entities;

namespace Assets.Scripts.ECS {
    struct VelocityLimiter : IComponentData {
        public float MaxSpeed;
        public float MinSpeed;
    }
}
