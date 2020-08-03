using Assets.Scripts.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class MoveSystem : SystemBase {
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((ref Translation translation, ref MoveComponent mover, in HeadingComponent heading) => {
            mover.Vel += mover.Acl;
            mover.Acl = new Vector3(0,0,0);
            translation.Value = translation.Value + mover.Vel.ToFloat3() * deltaTime;
        })
        .ScheduleParallel();
    }
}
