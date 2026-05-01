using ECS;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (transform, bullet, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<BulletComponent>>().WithEntityAccess())
        {
            transform.ValueRW.Position += bullet.ValueRO.moveDir * bullet.ValueRO.moveSpeed * SystemAPI.Time.DeltaTime;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(bullet.ValueRO.moveDir, transform.ValueRO.Up());
            bullet.ValueRW.lifetime += SystemAPI.Time.DeltaTime;

            if (bullet.ValueRW.lifetime > bullet.ValueRW.maxLifetime)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}