using ECS;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AIStats : MonoBehaviour
{
    public float maxSpeed;
    public int health;
    public List<WeaponStats> weapons;
    public ShipType type;
    public Faction faction;
}
