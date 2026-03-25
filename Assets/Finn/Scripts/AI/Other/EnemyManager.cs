using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public class EnemyManager : MonoBehaviour
{
    public AIManager AIManager;

    void Start()
    {
        AIManager = GetComponent<AIManager>();
    }


    void Update()
    {
        List<PathFinderAI> AIs = AIManager.AIs;

        foreach (PathFinderAI ai in AIs)
        {
            if (ai.faction != Faction.Enemy) continue;

            if (AIManager.simulatedSectors.Contains(new int2((int)AIManager.findCurrentSector(transform.position).x, (int)AIManager.findCurrentSector(transform.position).y)))
            {

            }
        }
    }
}
