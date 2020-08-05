using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS {
    class VelocityLimiterSystem : SystemBase {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref MoveComponent mover, in VelocityLimiter limiter) => {
                var magnitude = math.lengthsq(mover.Vel);
                var normalized = math.normalizesafe(mover.Vel);

                if (magnitude < limiter.MinSpeed)
                {
                    mover.Vel = normalized * limiter.MinSpeed;
                }
                else if (magnitude > limiter.MinSpeed)
                {
                    mover.Vel = normalized * limiter.MaxSpeed;
                }
            })
            .ScheduleParallel();
        }
    }
}
