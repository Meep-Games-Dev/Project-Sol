using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace ECS
{
    [BurstCompile]
    public partial struct BulletSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<BulletSpawner>(out var spawner)) return;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (tag, entity) in SystemAPI.Query<FireBulletTag>().WithEntityAccess())
            {
                Entity newBullet = ecb.Instantiate(spawner.prefab);

                ecb.SetComponent(newBullet, LocalTransform.FromPosition(tag.pos));
                ecb.AddComponent(newBullet, new BulletComponent
                {
                    moveDir = tag.dir,
                    moveSpeed = tag.speed,
                    maxLifetime = tag.lifetime,
                    damage = tag.damage,
                    belongingTo = tag.belongingTo,
                    radius = tag.radius,
                });
                ecb.DestroyEntity(entity);
            }
        }
    }
}

