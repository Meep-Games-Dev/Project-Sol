using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
public struct ShipLibraryItem : IBufferElementData
{
    public ShipType Type;
    public Entity Prefab;
    public Faction Faction;
}
public struct ShipLibraryTag : IComponentData { }
public class ShipLibraryAuthoring : MonoBehaviour
{
    [System.Serializable]
    public struct ShipEntry
    {
        public ShipType type;
        public GameObject prefab;
        public Faction faction;
    }

    public List<ShipEntry> shipPrefabs;
}

class ShipLibraryBaker : Baker<ShipLibraryAuthoring>
{
    public override void Bake(ShipLibraryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent<ShipLibraryTag>(entity);
        DynamicBuffer<ShipLibraryItem> buffer = AddBuffer<ShipLibraryItem>(entity);
        if (authoring.shipPrefabs.Count > 0)
        {
            foreach (var entry in authoring.shipPrefabs)
            {
                buffer.Add(new ShipLibraryItem
                {
                    Type = entry.type,
                    Prefab = GetEntity(entry.prefab, TransformUsageFlags.Dynamic),
                    Faction = entry.faction
                });
            }
        }

    }
}