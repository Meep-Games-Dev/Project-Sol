using Unity.Entities;
using UnityEngine;
namespace ECS
{
    public class BulletSpawnerAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public float spawnRate;
    }
    class BulletSpawnerBaker : Baker<BulletSpawnerAuthoring>
    {
        public override void Bake(BulletSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new BulletSpawner
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}