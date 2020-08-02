using Unity.Entities;
using Unity.Transforms;

public class MoveSystem : SystemBase {
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((ref Translation translation, in MoveComponent mover, in HeadingComponent heading) => {
            translation.Value = translation.Value + heading.Value * mover.Speed * deltaTime;
        })
        .ScheduleParallel();
    }
}
