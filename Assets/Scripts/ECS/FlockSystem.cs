using Assets.Scripts.Extensions;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FlockSystem : SystemBase {
    List<BoidComponent> _boidTypes = new List<BoidComponent>();

    EntityQuery _boidQuery;

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
                    velocities[entityInQueryIndex] = moveComponent.Vel.ToFloat3();
                    headings[entityInQueryIndex] = heading.Value;
                    positions[entityInQueryIndex] = pos.Position;
                })
                .ScheduleParallel(Dependency);

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

            var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initializeJobHandle, initialCellCountJobHandle);

            var mergeCellsJob = new MergeCells
            {
                cellIndices = cellIndices,
                cellAlignment = cellAlignment,
                cellSeparation = cellSeparation,
                cellCount = cellCount,
                cellCohesion = cellCohesion,
                headings = headings,
                positions = positions,
                velocities = velocities
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

                    if(cellCount[cellIndex] != 0) {

                        var cohesionVal = cellCohesion[cellIndex] / cellCount[cellIndex];
                        var cohesion = boidSettings.CohesionMod *
                                            math.normalizesafe(cohesionVal - translation.Value);

                        var separation = boidSettings.SeparationMod
                                                      * -1 * math.normalizesafe((translation.Value - cellSeparation[cellIndex]) / cellCount[cellIndex]);

                        var alignment = boidSettings.AvgSpeedMod *
                                            math.normalizesafe((cellAlignment[cellIndex] + cellAlignment[entityInQueryIndex]) / cellCount[cellIndex]) - mover.Vel.ToFloat3();

                        var distanceFromCenter = -translation.Value / 200f;                        

                        var steeringForce = alignment + separation + cohesion + distanceFromCenter;

                        mover.Acl += new Vector3(steeringForce.x, steeringForce.y, steeringForce.z);
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
        }

        _boidTypes.Clear();
    }

    protected override void OnCreate()
    {
        _boidQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<BoidComponent>(), ComponentType.ReadWrite<LocalToWorld>() },
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


        // Resolves the distance of the nearest obstacle and target and stores the cell index.
        public void ExecuteFirst(int index)
        {
            cellIndices[index] = index;
            cellCohesion[index] = positions[index];
            cellAlignment[index] = velocities[index];
            cellSeparation[index] = positions[index];
        }

        public void ExecuteNext(int cellIndex, int index)
        {
            cellCount[cellIndex] += 1;
            cellCohesion[cellIndex] = positions[cellIndex] + positions[index];
            cellAlignment[cellIndex] = velocities[cellIndex] + velocities[index];
            cellSeparation[cellIndex] = positions[cellIndex] + positions[index];
            cellIndices[index] = cellIndex;
        }
    }
}
