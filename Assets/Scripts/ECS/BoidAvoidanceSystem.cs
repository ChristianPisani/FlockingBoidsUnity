using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS {
    class BoidAvoidanceSystem : SystemBase {
        protected override void OnUpdate()
        {
            return;

            var boidTypes = new List<BoidComponent>();

            EntityManager.GetAllUniqueSharedComponentData(boidTypes);

            float deltaTime = Time.DeltaTime;
            
            for (int i = 0; i < boidTypes.Count; i++)
            {
                var boidSettings = boidTypes[i];

                Entities
                .WithSharedComponentFilter<BoidComponent>(boidSettings)
                .ForEach((ref MoveComponent mover, in Translation translation) => {
                    if (Physics.Raycast(
                        translation.Value,
                        mover.Vel,
                        out var hitInfo,
                        math.lengthsq(mover.Vel) * deltaTime * 3f,
                        Physics.IgnoreRaycastLayer,
                        QueryTriggerInteraction.Ignore))
                    {
                        mover.Vel *= -1;
                    }
                })
                .Run();
            }
        }
    }
}
