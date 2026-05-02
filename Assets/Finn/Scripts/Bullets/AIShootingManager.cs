using ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
[BurstCompile]
public partial struct AIShootingManager : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, rvoData, entity) in SystemAPI.Query<LocalTransform, RefRW<RVOAIData>>().WithEntityAccess())
        {

            RaycastInput input = new RaycastInput
            {
                Start = transform.Position + (transform.Up() * 3),
                End = transform.Position + (transform.Up() * 1000),
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            };
            if (physicsWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            {
                float distance = math.distance(input.Start, hit.Position);
                if (SystemAPI.HasComponent<RVOAIData>(hit.Entity))
                {
                    var data = SystemAPI.GetComponent<RVOAIData>(hit.Entity);

                    if (data.faction != rvoData.ValueRO.faction)
                    {
                        bool firing = false;
                        for (int i = 0; i < rvoData.ValueRO.weapons.Length; i++)
                        {
                            if (!firing)
                            {
                                var weapon = rvoData.ValueRO.weapons[i];
                                if (weapon.nextFireTime <= SystemAPI.Time.ElapsedTime)
                                {
                                    weapon.nextFireTime = (float)SystemAPI.Time.ElapsedTime + weapon.shootingSpeed;
                                    rvoData.ValueRW.weapons[i] = weapon;

                                    var requestEntity = ecb.CreateEntity();
                                    ecb.AddComponent(requestEntity, new FireBulletTag
                                    {
                                        pos = transform.Position + (transform.Up() * 3f),
                                        dir = transform.Up(),
                                        speed = rvoData.ValueRO.weapons[i].bulletSpeed,
                                        lifetime = 5f,
                                        damage = rvoData.ValueRO.weapons[i].damage,
                                        belongingTo = rvoData.ValueRO.faction
                                    });
                                    firing = true;
                                }
                            }

                        }

                    }
                }
            }
        }
    }
}