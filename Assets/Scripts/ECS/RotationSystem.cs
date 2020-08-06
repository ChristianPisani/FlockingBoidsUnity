using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS {
    class RotationSystem : SystemBase {
        protected override void OnUpdate()
        {
            var rotationComponents = new List<RotationComponent>();

            EntityManager.GetAllUniqueSharedComponentData(rotationComponents);

            for (int i = 0; i < rotationComponents.Count; i++)
            {
                var rotationComponent = rotationComponents[i];

                Entities
                    .WithSharedComponentFilter(rotationComponent)
                    .ForEach((ref Rotation rotation, in MoveComponent mover) => {
                        var currentRotation = rotation.Value;
                        var wantedRotation = Quaternion.LookRotation(!mover.Vel.Equals(float3.zero) ? mover.Vel : new float3(0,0,1));

                        rotation.Value =  Quaternion.RotateTowards(currentRotation, wantedRotation, rotationComponent.MaxDegrees);
                    })
                .ScheduleParallel();
            }
        }
    }
}
