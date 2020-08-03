using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.ECS {
    struct RotationComponent : ISharedComponentData {
        public float MaxDegrees;
        public Quaternion BaseRotation;
    }
}
