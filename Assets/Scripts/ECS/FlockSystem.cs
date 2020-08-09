using Assets.Scripts.ECS;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class FlockSystem : SystemBase {
    List<BoidComponent> _boidTypes = new List<BoidComponent>();

    EntityQuery _boidQuery;
    EntityQuery _targetQuery;

    protected override void OnUpdate()
    {
        EntityManager.GetAllUniqueSharedComponentData(_boidTypes);

        float deltaTime = Time.DeltaTime;

        for (int i = 0; i < _boidTypes.Count; i++)
        {
            var boidSettings = _boidTypes[i];

            _boidQuery.ResetFilter();
            _boidQuery.AddSharedComponentFilter<BoidComponent>(boidSettings);
            var boidCount = _boidQuery.CalculateEntityCount();

            if (boidCount == 0) continue;

            ////
            // Allocate memory
            ////

            var velocities = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var headings = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var positions = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellIndices = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellCount = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellAlignment = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellSeparation = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellCohesion = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var initializeJobHandle = Entities
                        .WithSharedComponentFilter(boidSettings)
                        .ForEach((int entityInQueryIndex, in LocalToWorld pos, in HeadingComponent heading, in MoveComponent moveComponent) => {
                            velocities[entityInQueryIndex] = moveComponent.Vel;
                            headings[entityInQueryIndex] = heading.Value;
                            positions[entityInQueryIndex] = pos.Position;
                        })
                        .ScheduleParallel(Dependency);

            ////
            // Find targets
            ////

            var targetCount = _targetQuery.CalculateEntityCount();

            var targets = new NativeArray<BoidTarget>(targetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var targetIndexes = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var avoidIndexes = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var targetsJobHandle = Entities.ForEach((int entityInQueryIndex, ref BoidTarget target, in Translation translation) => {
                target.Pos = translation.Value;

                targets[entityInQueryIndex] = target;
            })
            .ScheduleParallel(Dependency);

            var initialTargetIndexJob = new MemsetNativeArray<int>
            {
                Source = targetIndexes,
                Value = -1
            };
            var initialTargetIndexJobHandle = initialTargetIndexJob.Schedule(boidCount, 64, Dependency);

            var fillAvoidIndexes = new MemsetNativeArray<int>
            {
                Source = avoidIndexes,
                Value = -1
            };
            var fillAvoidIndexesJobHandle = fillAvoidIndexes.Schedule(boidCount, 64, Dependency);

            ////
            // CREATE HASHMAP
            ////

            var hashMap = new NativeMultiHashMap<int, int>(boidCount, Allocator.TempJob);

            var parallelHashMap = hashMap.AsParallelWriter();
            var hashPositionsJobHandle = Entities
                .WithName("HashPositionsJob")
                .WithAll<BoidComponent>()
                .ForEach((int entityInQueryIndex, in Translation translation) => {
                    var hash = (int)math.hash(new int3(math.floor(translation.Value / boidSettings.PerceptionRadius)));
                    parallelHashMap.Add(hash, entityInQueryIndex);
                })
                .ScheduleParallel(Dependency);

            hashPositionsJobHandle.Complete();

            ////
            // Initialize cell count with 1 for each cell
            ////

            var initialCellCountJob = new MemsetNativeArray<int>
            {
                Source = cellCount,
                Value = 1
            };
            var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, 64, Dependency);

            ////
            // Merge cells
            ////

            var barrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initializeJobHandle, initialCellCountJobHandle);
            var barrierJobHandle2 = JobHandle.CombineDependencies(barrierJobHandle, fillAvoidIndexesJobHandle, initialTargetIndexJobHandle);
            var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(targetsJobHandle, barrierJobHandle2);

            var mergeCellsJob = new MergeCells
            {
                cellIndices = cellIndices,
                cellAlignment = cellAlignment,
                cellSeparation = cellSeparation,
                cellCount = cellCount,
                cellCohesion = cellCohesion,
                headings = headings,
                positions = positions,
                velocities = velocities,
                targets = targets,
                targetIndexes = targetIndexes,
                avoidIndexes = avoidIndexes
            };
            var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, mergeCellsBarrierJobHandle);

            mergeCellsJobHandle.Complete();

            ////
            // Calculate new velocities
            ////
            
            var steeringForceJobHandle = Entities
                .WithSharedComponentFilter(boidSettings)
                .ForEach((int entityInQueryIndex, ref MoveComponent mover, ref Translation translation, ref Rotation rotation) => {
                    var cellIndex = cellIndices[entityInQueryIndex];

                    if (cellCount[cellIndex] != 0)
                    {

                        var cohesionVal = cellCohesion[cellIndex] / cellCount[cellIndex];
                        var cohesion = boidSettings.CohesionMod *
                                            math.normalizesafe(cohesionVal - translation.Value);

                        var separation = boidSettings.SeparationMod
                                                      * -1 * math.normalizesafe(translation.Value - (cellSeparation[cellIndex] / cellCount[cellIndex]));

                        var alignment = boidSettings.AvgSpeedMod *
                                            math.normalizesafe((cellAlignment[cellIndex] + cellAlignment[entityInQueryIndex]) / cellCount[cellIndex]) - mover.Vel;


                        var steeringForce = math.normalizesafe(alignment + separation + cohesion) * boidSettings.MinSpeed;

                        var targetIndex = targetIndexes[cellIndex];
                        if (targetIndex != -1)
                        {
                            var target = targets[targetIndex];
                            steeringForce += math.normalizesafe(target.Pos - translation.Value) * target.Strength;
                        }

                        var avoidIndex = avoidIndexes[cellIndex];
                        if (avoidIndex != -1)
                        {
                            var avoidForceTarget = targets[avoidIndex];
                            var diff = (translation.Value - avoidForceTarget.Pos);
                            
                            if (math.lengthsq(diff) < 1f) diff = new float3(1, 1, 1);

                            steeringForce = math.normalizesafe(diff) * avoidForceTarget.Strength;
                            mover.Vel = float3.zero;
                        }

                        mover.Acl += steeringForce;
                    }
                })
                .ScheduleParallel(mergeCellsJobHandle);

            steeringForceJobHandle.Complete();

            ////
            // Dispose
            ////            

            hashMap.Dispose();
            velocities.Dispose();
            headings.Dispose();
            positions.Dispose();
            cellIndices.Dispose();
            cellCount.Dispose();
            cellAlignment.Dispose();
            cellSeparation.Dispose();
            cellCohesion.Dispose();
            targets.Dispose();
            targetIndexes.Dispose();
            avoidIndexes.Dispose();
        }

        _boidTypes.Clear();
    }

    protected override void OnCreate()
    {
        _boidQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<BoidComponent>(), ComponentType.ReadWrite<LocalToWorld>() },
        });

        _targetQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<BoidTarget>(), ComponentType.ReadOnly<LocalToWorld>() },
        });
    }

    [BurstCompile]
    struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices {
        public NativeArray<float3> velocities;
        public NativeArray<float3> headings;
        public NativeArray<float3> positions;
        public NativeArray<int> cellIndices;
        public NativeArray<int> cellCount;
        public NativeArray<float3> cellAlignment;
        public NativeArray<float3> cellSeparation;
        public NativeArray<float3> cellCohesion;
        [ReadOnly] public NativeArray<BoidTarget> targets;
        public NativeArray<int> targetIndexes;
        public NativeArray<int> avoidIndexes;


        // Resolves the distance of the nearest obstacle and target and stores the cell index.
        public void ExecuteFirst(int index)
        {
            cellIndices[index] = index;
            cellCohesion[index] = positions[index];
            cellAlignment[index] = velocities[index];
            cellSeparation[index] = positions[index];

            float minDistanceFromTarget = float.PositiveInfinity;
            float minDistanceFromAvoider = float.PositiveInfinity;

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                var target = targets[targetIndex];

                var diff = positions[index] - target.Pos;
                var distanceFromTarget = math.lengthsq(diff);

                if (!target.Push && 
                    distanceFromTarget < minDistanceFromTarget &&
                    distanceFromTarget > target.PullDistance)
                {                    
                    minDistanceFromTarget = distanceFromTarget;
                    targetIndexes[index] = targetIndex;
                }

                if (target.Push && 
                    distanceFromTarget < minDistanceFromAvoider &&
                    distanceFromTarget < target.PushbackDistance)
                {
                    minDistanceFromAvoider = distanceFromTarget;
                    avoidIndexes[index] = targetIndex;
                }
            }
        }

        public void ExecuteNext(int cellIndex, int index)
        {            
            cellCount[cellIndex] += 1;
            cellCohesion[cellIndex] += positions[index];
            cellAlignment[cellIndex] += velocities[index];
            cellSeparation[cellIndex] += positions[index];
            cellIndices[index] = cellIndex;
        }
    }
}
