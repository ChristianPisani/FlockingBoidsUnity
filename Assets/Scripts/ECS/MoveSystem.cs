using Assets.Scripts.ECS;
using Assets.Scripts.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class MoveSystem : SystemBase {
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var elapsedTime = Time.ElapsedTime;

        Entities.ForEach((ref Translation translation, ref MoveComponent mover) => {
            mover.Vel += mover.Acl * deltaTime;
            mover.Acl = new Vector3(0,0,0);
            translation.Value = translation.Value + mover.Vel * deltaTime;
        })
        .ScheduleParallel();
    }
}
