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
            float3 rayOrigin = transform.Position + (transform.Up() * 3);
            float3 forward = transform.Up();

            // FOV settings — tweak these or move them into RVOAIData
            const float fovAngleDegrees = 90f;
            const int rayCount = 7; // odd number keeps a centre ray
            const float maxRange = 1000f;

            Entity detectedEnemy = Entity.Null;
            float3 dirToEnemy = forward; // default: shoot straight ahead

            float halfFov = math.radians(fovAngleDegrees * 0.5f);
            float angleStep = (rayCount > 1) ? (halfFov * 2f / (rayCount - 1)) : 0f;

            for (int r = 0; r < rayCount; r++)
            {
                // Spread rays evenly across the FOV arc
                float angle = -halfFov + angleStep * r;
                float3 rayDir = RotateAroundUp(forward, angle);

                RaycastInput input = new RaycastInput
                {
                    Start = rayOrigin,
                    End = rayOrigin + rayDir * maxRange,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = ~0u,
                        GroupIndex = 0
                    }
                };

                if (physicsWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
                {
                    if (SystemAPI.HasComponent<RVOAIData>(hit.Entity))
                    {
                        var data = SystemAPI.GetComponent<RVOAIData>(hit.Entity);
                        if (data.faction != rvoData.ValueRO.faction)
                        {
                            detectedEnemy = hit.Entity;
                            // Aim at the actual hit point, not just the ray direction
                            dirToEnemy = math.normalize(hit.Position - rayOrigin);
                            break; // first enemy found wins; remove to pick closest instead
                        }
                    }
                }
            }

            // Only fire if an enemy was spotted inside the FOV
            if (detectedEnemy != Entity.Null)
            {
                bool firing = false;
                for (int i = 0; i < rvoData.ValueRO.weapons.Length; i++)
                {
                    if (firing) break;

                    var weapon = rvoData.ValueRO.weapons[i];
                    if (weapon.nextFireTime <= SystemAPI.Time.ElapsedTime)
                    {
                        weapon.nextFireTime = (float)SystemAPI.Time.ElapsedTime + weapon.shootingSpeed;
                        rvoData.ValueRW.weapons[i] = weapon;

                        var requestEntity = ecb.CreateEntity();
                        ecb.AddComponent(requestEntity, new FireBulletTag
                        {
                            pos = rayOrigin,
                            dir = dirToEnemy,   // fires toward the detected hit point
                            speed = weapon.bulletSpeed,
                            lifetime = 5f,
                            damage = weapon.damage,
                            belongingTo = rvoData.ValueRO.faction
                        });
                        firing = true;
                    }
                }
            }
        }
    }

    // Rotates a direction vector around the world Y axis (or whatever "up" is in your scene).
    // If your game is 3-D and agents can tilt, replace math.up() with the agent's right/up accordingly.
    [BurstCompile]
    private float3 RotateAroundUp(float3 dir, float radians)
    {
        float sin = math.sin(radians);
        float cos = math.cos(radians);
        // Rodrigues rotation around Y
        return new float3(
            cos * dir.x + sin * dir.z,
            dir.y,
            -sin * dir.x + cos * dir.z
        );
    }
}