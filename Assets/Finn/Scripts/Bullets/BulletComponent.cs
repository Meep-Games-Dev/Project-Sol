using Unity.Entities;
using Unity.Mathematics;

namespace ECS
{
    public struct BulletComponent : IComponentData
    {
        public float3 moveDir;
        public float moveSpeed;
        public float lifetime;
        public float maxLifetime;
        public float radius;
        public float damage;
        public Faction belongingTo;
    }
    public struct FireBulletTag : IComponentData
    {
        public float3 pos;
        public float speed;
        public float3 dir;
        public float lifetime;
        public float damage;
        public Faction belongingTo;
    }
    public struct Damageable : IComponentData
    {
        public float health;
        public float radius;
        public Faction belongingTo;
    }
    public struct ECSFaction : IComponentData
    {
        public Faction faction;
    }
    public struct AIData : IComponentData
    {
        public RVOAIData data;
    }
}

