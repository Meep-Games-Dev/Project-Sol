using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public class EnemyManager : MonoBehaviour
{
    public RVOManager AIManager;
    public SolarSystemManager solarSystemManager;
    public List<Guid> allEnemies = new List<Guid>();
    public List<Guid> enemiesInSquad = new List<Guid>();
    public List<Guid> enemiesNotInSquad = new List<Guid>();
    public List<Squadron> squadrons = new List<Squadron>();
    public List<Resource> resourcesOwned = new List<Resource>();
    public Planet homePlanet;
    public AlliedManager alliedManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        alliedManager = FindFirstObjectByType<AlliedManager>();
    }

    public void CreateSquadron(List<Guid> AIs, string name)
    {

        List<Guid> AIList = new List<Guid>(AIs);
        AIList.RemoveAt(0);
        AddToSquadList(AIs[0]);
        if (name != null)
        {
            squadrons.Add(new Squadron { AIidx = AIList, name = name, leadAI = AIs[0] });
        }
        else
        {
            squadrons.Add(new Squadron { AIidx = AIList, leadAI = AIs[0] });
        }
        AIManager.AIs[allEnemies.Find(x => x == AIs[0])].squadron = squadrons[squadrons.Count - 1];
        //Debug.Log("Setting AI " + AIManager.AIs[allEnemies.Find(x => x == AIs[0])].gameObjectRef.name + " as leader of squad");
        for (int i = 0; i < AIList.Count; i++)
        {
            //Debug.Log("Adding AI " + AIManager.AIs[allEnemies.Find(x => x == AIList[i])].gameObjectRef.name + " to squad");
            AddToSquadList(AIs[i]);
            AIManager.AIs[allEnemies.Find(x => x == AIList[i])].squadron = squadrons[squadrons.Count - 1];
        }

    }
    public void AddToSquadList(Guid AI)
    {
        if (!enemiesInSquad.Contains(AI))
        {
            enemiesInSquad.Add(AI);
        }
        if (enemiesNotInSquad.Contains(AI))
        {
            enemiesNotInSquad.Remove(AI);
        }

    }
    public void DeleteFromSquadList(Guid AI)
    {
        if (enemiesInSquad.Contains(AI))
        {
            enemiesInSquad.Remove(AI);
        }
        if (!enemiesNotInSquad.Contains(AI))
        {
            enemiesNotInSquad.Add(AI);
        }

    }
    public void RemoveFromSquadron(Guid AI, Squadron squadron)
    {
        int idx = squadron.AIidx.IndexOf(AI);
        DeleteFromSquadList(AI);
        if (idx != -1)
        {
            squadron.AIidx.RemoveAt(idx);

        }
        else
        {
            if (AI == squadron.leadAI)
            {
                squadron.leadAI = squadron.AIidx[0];
                squadron.AIidx.RemoveAt(0);
            }
        }
    }
    public void DestroySquadron(Squadron squadron)
    {
        AIManager.AIs[squadrons[squadrons.IndexOf(squadron)].leadAI].squadron = null;
        DeleteFromSquadList(squadrons[squadrons.IndexOf(squadron)].leadAI);
        for (int i = 0; i < squadrons[squadrons.IndexOf(squadron)].AIidx.Count; i++)
        {
            AIManager.AIs[squadrons[squadrons.IndexOf(squadron)].AIidx[i]].squadron = null;
            DeleteFromSquadList(squadrons[squadrons.IndexOf(squadron)].AIidx[i]);
        }
        squadrons.Remove(squadron);
    }
    public void SendSquadron(Squadron squadron, Vector2 position)
    {
        int squadronIdx = squadrons.FindIndex(x => x == squadron);
        squadrons[squadronIdx].target = position;
        squadrons[squadronIdx].targetSet = true;
        if (squadrons[squadronIdx].formation == Formation.None)
        {
            AIManager.SendAI(AIManager.AIs[squadrons[squadronIdx].leadAI], position, 0.1f);
            for (int i = 0; i < squadrons[squadronIdx].AIidx.Count; i++)
            {
                AIManager.SendAI(AIManager.AIs[squadrons[squadronIdx].AIidx[i]], position, squadrons[squadronIdx].AIidx.Count * 0.5f);

            }
        }
        else if (squadrons[squadronIdx].formation == Formation.V)
        {
            AIManager.SendAI(AIManager.AIs[squadrons[squadronIdx].leadAI], position, 0.5f);
        }
    }

    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i < squadrons.Count; i++)
        {
            if (squadrons[i].formation == Formation.V)
            {
                if (squadrons[i].targetSet)
                {
                    float angle = Mathf.Atan2(AIManager.AIs[squadrons[i].leadAI].vel.y, AIManager.AIs[squadrons[i].leadAI].vel.x) * Mathf.Rad2Deg;
                    Quaternion leaderRotation = Quaternion.Euler(0, 0, angle - 90f);
                    List<Vector2> AILocalPos = FormationData.VData(squadrons[i].AIidx.Count, leaderRotation, 5);
                    AIManager.SendAI(AIManager.AIs[squadrons[i].leadAI], squadrons[i].target, 0.5f);
                    for (int j = 0; j < squadrons[i].AIidx.Count; j++)
                    {
                        AIManager.SendAI(AIManager.AIs[squadrons[i].AIidx[j]], AILocalPos[j] + AIManager.AIs[squadrons[i].leadAI].pos, 0.1f);
                        AIManager.AIs[squadrons[i].AIidx[j]].data.currentSpeed = AIManager.AIs[squadrons[i].AIidx[j]].data.maxSpeed + Vector2.Distance(AIManager.AIs[squadrons[i].AIidx[j]].pos, AILocalPos[j] + AIManager.AIs[squadrons[i].leadAI].pos);
                    }
                    if (Vector2.Distance(AIManager.AIs[squadrons[i].leadAI].gameObjectRef.transform.position, squadrons[i].target) < 0.5f)
                    {
                        squadrons[i].targetSet = false;
                    }
                }

            }
        }
        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (AIManager.AIs[allEnemies[i]].squadron == null && squadrons.Count < Mathf.FloorToInt(allEnemies.Count / 8))
            {
                List<Guid> AIsToAdd = new List<Guid>();
                AIsToAdd.Add(allEnemies[i]);
                for (int j = 0; j < Mathf.Min(enemiesNotInSquad.Count, 5); j++)
                {
                    AIsToAdd.Add(enemiesNotInSquad[j]);
                }
                CreateSquadron(AIsToAdd, RandUtils.RandomGreekLetter());
            }
            else if (AIManager.AIs[allEnemies[i]].squadron == null && squadrons.Count > Mathf.FloorToInt(allEnemies.Count / 8))
            {
                int squadronIdx = UnityEngine.Random.Range(0, squadrons.Count);
                squadrons[squadronIdx].AIidx.Add(allEnemies[i]);
                AIManager.AIs[allEnemies[i]].squadron = squadrons[squadronIdx];
            }
            if (AIManager.AIs[allEnemies[i]].attackingTarget)
            {
                if (AIManager.AIs[allEnemies[i]].followTarget != Guid.Empty)
                {
                    if (Vector2.Distance(AIManager.AIs[AIManager.AIs[allEnemies[i]].followTarget].pos, AIManager.AIs[allEnemies[i]].pos) < 10 && !AIManager.AIs[allEnemies[i]].flybyTarget)
                    {
                        AIManager.SendAI(AIManager.AIs[allEnemies[i]], Vector2.Normalize(AIManager.AIs[allEnemies[i]].pos - AIManager.AIs[AIManager.AIs[allEnemies[i]].followTarget].pos) * 4, 0.1f);
                        AIManager.AIs[allEnemies[i]].flybyTarget = true;
                    }
                }

            }
        }
        squadrons.RemoveAll(x => x.AIidx.Count == 0);


    }


}
