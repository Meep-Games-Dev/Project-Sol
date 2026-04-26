using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AlliedManager : MonoBehaviour
{
    public RVOManager AIManager;
    public SolarSystemManager solarSystemManager;
    public List<int> allAllied = new List<int>();
    public List<Squadron> squadrons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void CreateSquadron(List<int> AIs, string name)
    {
        List<int> AIList = AIs;
        AIs.RemoveAt(0);
        if (name != null)
        {
            squadrons.Add(new Squadron { AIidx = AIs, name = name, leadAI = AIs[0] });
        }
        else
        {
            squadrons.Add(new Squadron { AIidx = AIs, leadAI = AIs[0] });
        }
        for (int i = 0; i < AIs.Count; i++)
        {
            AIManager.AIs[allAllied[AIs[i]]].squadron = squadrons[squadrons.Count - 1];
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
            for (int i = 0; i < squadrons[squadronIdx].AIidx.Count; i++)
            {
                AIManager.SendAI(AIManager.AIs[squadrons[squadronIdx].AIidx[i]], position, squadrons[squadronIdx].AIidx.Count * 0.5f);
            }
        }
        else if (squadrons[squadronIdx].formation == Formation.V)
        {
            List<Vector2> AILocalPos = FormationData.VData(squadrons[squadronIdx].AIidx.Count, AIManager.AIs[squadrons[squadronIdx].leadAI].gameObjectRef.transform.rotation);
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
                List<Vector2> AILocalPos = FormationData.VData(squadrons[i].AIidx.Count, AIManager.AIs[squadrons[i].leadAI].gameObjectRef.transform.rotation);
                for (int j = 0; j < squadrons[i].AIidx.Count; j++)
                {
                    AIManager.SendAI(AIManager.AIs[squadrons[i].AIidx[j]], AILocalPos[j] + AIManager.AIs[squadrons[i].leadAI].pos, 0.1f);
                }
            }
        }
        squadrons.RemoveAll(x => x.AIidx.Count == 0);
    }
}
