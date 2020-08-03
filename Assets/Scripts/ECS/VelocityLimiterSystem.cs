using Unity.Entities;

namespace Assets.Scripts.ECS {
    class VelocityLimiterSystem : SystemBase {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref MoveComponent mover, in VelocityLimiter limiter) => {
                if (mover.Vel.magnitude < limiter.MinSpeed)
                {
                    mover.Vel = mover.Vel.normalized * limiter.MinSpeed;
                }
                else if (mover.Vel.magnitude > limiter.MinSpeed)
                {
                    mover.Vel = mover.Vel.normalized * limiter.MaxSpeed;
                }
            })
            .ScheduleParallel();
        }
    }
}
