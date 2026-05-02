using ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct BulletCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var destroyedEntities = new NativeParallelHashSet<Entity>(128, Allocator.Temp);
        foreach (var (bulletTransform, bullet, bulletEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<BulletComponent>>().WithEntityAccess())
        {
            float3 bulletPos = bulletTransform.ValueRO.Position;
            float bulletRadius = bullet.ValueRO.radius;

            foreach (var (targetTransform, target, targetEntity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<RVOAIData>>().WithEntityAccess())
            {
                if (destroyedEntities.Contains(targetEntity) || destroyedEntities.Contains(bulletEntity) || bullet.ValueRO.belongingTo == target.ValueRO.faction)
                {
                    continue;
                }
                float distanceSq = math.distancesq(bulletPos, targetTransform.ValueRO.Position);
                float combinedRadius = bulletRadius + target.ValueRO.radius;

                if (distanceSq < (combinedRadius * combinedRadius))
                {
                    target.ValueRW.health -= bullet.ValueRO.damage;
                    ecb.DestroyEntity(bulletEntity);
                    destroyedEntities.Add(bulletEntity);
                    if (target.ValueRW.health <= 0)
                    {

                        ecb.DestroyEntity(targetEntity);
                        destroyedEntities.Add(targetEntity);
                    }
                    break;
                }
            }
        }
    }
}
