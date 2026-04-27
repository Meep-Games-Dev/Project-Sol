using NUnit.Framework;
using System.Collections.Generic;
//using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

public class AlliedManager : MonoBehaviour
{
    public RVOManager AIManager;
    public SolarSystemManager solarSystemManager;
    public List<int> allAllied = new List<int>();
    public List<Squadron> squadrons = new List<Squadron>();
    public List<Resource> resourcesOwned = new List<Resource>();
    public UIManager uiManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void CreateSquadron(List<int> AIs, string name)
    {
        Debug.Log(AIs.Count);
        List<int> AIList = new List<int>(AIs);
        AIList.RemoveAt(0);
        Debug.Log(AIs.Count + "/" + AIList.Count);
        if (name != null)
        {
            squadrons.Add(new Squadron { AIidx = AIList, name = name, leadAI = AIs[0] });
        }
        else
        {
            squadrons.Add(new Squadron { AIidx = AIList, leadAI = AIs[0] });
        }
        AIManager.AIs[allAllied[AIs[0]]].squadron = squadrons[squadrons.Count - 1];
        Debug.Log("Setting AI " + AIManager.AIs[allAllied[AIs[0]]].gameObjectRef.name + " as leader of squad");
        for (int i = 0; i < AIList.Count; i++)
        {
            Debug.Log("Adding AI " + AIManager.AIs[allAllied[AIList[i]]].gameObjectRef.name + " to squad");
            AIManager.AIs[allAllied[AIList[i]]].squadron = squadrons[squadrons.Count - 1];
        }
    }
    public void RemoveFromSquadron(int AI, Squadron squadron)
    {
        int idx = squadron.AIidx.IndexOf(AI);
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
        for (int i = 0; i < squadrons[squadrons.IndexOf(squadron)].AIidx.Count; i++)
        {
            AIManager.AIs[squadrons[squadrons.IndexOf(squadron)].AIidx[i]].squadron = null;
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
        squadrons.RemoveAll(x => x.AIidx.Count == 0);
        uiManager.UpdateSquadronDropdown(squadrons);
        uiManager.UpdateResourceDropdown(resourcesOwned);


    }
}
