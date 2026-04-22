using NUnit.Framework;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class RVOManager : MonoBehaviour
{
    public List<RVOAI> AIs = new List<RVOAI>();
    public List<int> activeAIs = new List<int>();
    public List<RVOobstacle> obstacles = new List<RVOobstacle>();
    public List<Planet> planets = new List<Planet>();
    public SolarSystemManager solarSystemManager;
    public float maxSpeed;
    public int AICount;
    public List<GameObject> AIPrefabs;
    public float AIRadiusBuffer;
    public float obstacleRadiusBuffer;
    public float pushSpeed;
    public float AIPickupRadius;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


        ObstacleTag[] obstacleComponents = FindObjectsByType<ObstacleTag>(FindObjectsSortMode.None);

        for (int i = 0; i < obstacleComponents.Length; i++)
        {
            SphereCollider collider = obstacleComponents[i].GetComponent<SphereCollider>();

            float rad = collider.radius * math.max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);

            obstacles.Add(new RVOobstacle
            {
                rad = rad + obstacleRadiusBuffer,
                pos = (Vector2)collider.gameObject.transform.position,
                objRef = collider.gameObject
            });
        }
        solarSystemManager = FindFirstObjectByType<SolarSystemManager>();
        planets = solarSystemManager.planetComponentList;
        Debug.Log(planets.Count);
    }
    public void SpawnAIs()
    {
        for (int i = 0; i < AICount; i++)
        {
            GameObject instantiatedObj = Instantiate(AIPrefabs[0], new Vector3(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-50, 50), 0), Quaternion.identity);
            SphereCollider collider = instantiatedObj.GetComponent<SphereCollider>();

            float rad = collider.radius * math.max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
            RVOAI ai = new RVOAI
            {
                pos = instantiatedObj.transform.position,
                vel = Vector2.zero,
                gameObjectRef = instantiatedObj,
                rad = rad + AIRadiusBuffer,
                AIType = 0
            };
            AIs.Add(ai);
        }
    }
    public void SendAI(RVOAI ai, Vector2 position, float distance)
    {
        if (!activeAIs.Contains(AIs.IndexOf(ai)))
        {
            ai.target = position + new Vector2(UnityEngine.Random.Range(-distance / 2, distance / 2), UnityEngine.Random.Range(-distance / 2, distance / 2));
            ai.enemyTarget = null;
            ai.targetSet = true;
            ai.distanceToKeep = UnityEngine.Random.Range(0f, distance);
            activeAIs.Add(AIs.IndexOf(ai));
        }
        else
        {
            ai.target = position + new Vector2(UnityEngine.Random.Range(-distance / 2, distance / 2), UnityEngine.Random.Range(-distance / 2, distance / 2));
            ai.enemyTarget = null;
            ai.targetSet = true;
            ai.distanceToKeep = UnityEngine.Random.Range(0f, distance);
        }
    }
    public void AttackAI(RVOAI attacker, RVOAI attacked)
    {
        if (!activeAIs.Contains(AIs.IndexOf(attacker)))
        {
            attacker.enemyTarget = attacked;
            attacker.targetSet = true;
            attacker.distanceToKeep = 10;
            activeAIs.Add(AIs.IndexOf(attacker));
        }
    }
    public void LoadAIs(SaveableAIGroup data)
    {
        for (int i = 0; i < AIs.Count; i++)
        {
            Destroy(AIs[i].gameObjectRef.gameObject);
        }
        AIs.Clear();
        activeAIs.Clear();
        AIs = new List<RVOAI>();
        for (int i = 0; i < data.AIs.Count; i++)
        {

            GameObject instantiatedAI = Instantiate(AIPrefabs[data.AIs[i].AIType], data.AIs[i].pos, data.AIs[i].rotation);
            RVOAI ai = new RVOAI
            {
                targetSet = data.AIs[i].targetSet,
                distanceToKeep = data.AIs[i].distanceToKeep,
                pos = data.AIs[i].pos,
                rad = data.AIs[i].rad,
                target = data.AIs[i].target,
                vel = data.AIs[i].vel,
                gameObjectRef = instantiatedAI
            };
            AIs.Add(ai);
            if (ai.targetSet)
            {
                activeAIs.Add(i);
            }
        }
        for (int i = 0; i < AIs.Count; i++)
        {
            if (data.AIs[i].enemyTargetIdx != -1)
            {
                AIs[i].enemyTarget = AIs[data.AIs[i].enemyTargetIdx];
            }
        }
    }
    public SaveableAIGroup Save()
    {
        SaveableAIGroup save = new SaveableAIGroup();
        List<SaveableRVOAI> saveableRVOAIs = new List<SaveableRVOAI>();
        for (int i = 0; i < AIs.Count; i++)
        {
            SaveableRVOAI saveableAI = new SaveableRVOAI
            {
                targetSet = AIs[i].targetSet,
                distanceToKeep = AIs[i].distanceToKeep,
                pos = AIs[i].pos,
                rad = AIs[i].rad,
                target = AIs[i].target,
                vel = AIs[i].vel,
                AIType = AIs[i].AIType,
                rotation = AIs[i].gameObjectRef.transform.rotation,
            };
            if (AIs[i].enemyTarget != null)
            {
                saveableAI.enemyTargetIdx = AIs.FindIndex(x => x == AIs[i].enemyTarget);
            }
            else
            {
                saveableAI.enemyTargetIdx = -1;
            }
            saveableRVOAIs.Add(saveableAI);
        }
        save.AIs = saveableRVOAIs;
        return save;
    }
    // Update is called once per frame
    void Update()
    {

        NativeArray<float2> positions = new NativeArray<float2>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float2> velocities = new NativeArray<float2>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float2> goals = new NativeArray<float2>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float> radiuses = new NativeArray<float>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float2> results = new NativeArray<float2>(activeAIs.Count, Allocator.TempJob);
        NativeArray<bool> finished = new NativeArray<bool>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float3> obstaclePosRad = new NativeArray<float3>(obstacles.Count, Allocator.TempJob);
        NativeArray<float2> obstacleVelocities = new NativeArray<float2>(obstacles.Count, Allocator.TempJob);
        NativeArray<float> distanceToKeep = new NativeArray<float>(activeAIs.Count, Allocator.TempJob);
        NativeArray<float2> pushResults = new NativeArray<float2>(AIs.Count, Allocator.TempJob);
        NativeArray<float2> allPositions = new NativeArray<float2>(AIs.Count, Allocator.TempJob);
        NativeArray<float> allRadius = new NativeArray<float>(AIs.Count, Allocator.TempJob);
        for (int i = 0; i < activeAIs.Count; i++)
        {
            RVOAI ai = AIs[activeAIs[i]];
            ai.pos = ai.gameObjectRef.transform.position;
            ai.target = AIs[activeAIs[i]].target;
            ai.rad = AIs[activeAIs[i]].rad;
            positions[i] = ai.pos;
            velocities[i] = ai.vel;
            goals[i] = ai.target;
            radiuses[i] = ai.rad;
            distanceToKeep[i] = ai.distanceToKeep;
        }

        for (int i = 0; i < obstacles.Count; i++)
        {
            obstacles[i].pos = (Vector2)obstacles[i].objRef.transform.position;
            obstaclePosRad[i] = new float3(obstacles[i].pos.x, obstacles[i].pos.y, obstacles[i].rad);
            obstacleVelocities[i] = obstacles[i].previousPos - obstacles[i].pos;
            obstacles[i].previousPos = (Vector2)obstacles[i].objRef.transform.position;

        }
        for (int i = 0; i < AIs.Count; i++)
        {
            allPositions[i] = (Vector2)AIs[i].gameObjectRef.transform.position;
            allRadius[i] = AIs[i].rad;
        }
        DetectNearbyJob nearbyJob = new DetectNearbyJob
        {
            allPositions = allPositions,
            nearbyMap = pushResults,
            detectionRadiusSq = allRadius
        };
        for (int i = 0; i < pushResults.Length; i++) pushResults[i] = float2.zero;
        JobHandle nearbyHandle = nearbyJob.Schedule(AIs.Count, 64);
        nearbyHandle.Complete();

        for (int i = 0; i < AIs.Count; i++)
        {
            AIs[i].gameObjectRef.transform.position += (Vector3)((Vector2)pushResults[i] * pushSpeed * Time.deltaTime);
            AIs[i].pos = AIs[i].gameObjectRef.transform.position;
            bool hasParent = false;
            for (int j = 0; j < planets.Count; j++)
            {
                SphereCollider collider = planets[j].gameObject.GetComponentInChildren<SphereCollider>();
                if (Vector2.Distance(planets[j].gameObject.transform.position, AIs[i].gameObjectRef.transform.position) < (collider.radius * math.max(collider.transform.lossyScale.x, collider.transform.lossyScale.y)) + AIPickupRadius)
                {
                    AIs[i].gameObjectRef.transform.parent = planets[j].gameObject.transform;
                    hasParent = true;
                    Debug.Log("Parented AI");
                }
            }
            if (!hasParent)
            {
                if (AIs[i].gameObjectRef.transform.parent != null)
                {
                    AIs[i].gameObjectRef.transform.parent = null;
                    Debug.Log("Unparented AI");
                }

            }
        }
        CalculateRelativeVelocities job = new CalculateRelativeVelocities
        {
            allPositions = positions,
            velocities = velocities,
            goals = goals,
            radiuses = radiuses,
            maxSpeed = maxSpeed,
            DirMap = results,
            obstacles = obstaclePosRad,
            obstacleVelocities = obstacleVelocities,
            gotToTarget = finished,
            distancesToKeep = distanceToKeep
        };
        for (int i = 0; i < results.Length; i++) results[i] = float2.zero;
        JobHandle handle = job.Schedule(activeAIs.Count, 64);
        handle.Complete();

        List<int> AIsToRemove = new List<int>();
        for (int i = 0; i < activeAIs.Count; i++)
        {
            if (finished[i] && AIs[activeAIs[i]].enemyTarget == null)
            {
                AIs[activeAIs[i]].targetSet = false;
                continue;
            }
            Vector2 smoothedVel = (Vector2.Lerp(AIs[activeAIs[i]].vel, new Vector2(results[i].x, results[i].y), Time.deltaTime * 5f));
            AIs[activeAIs[i]].gameObjectRef.transform.position += (Vector3)smoothedVel * Time.deltaTime;
            AIs[activeAIs[i]].vel = smoothedVel;
            if (smoothedVel.sqrMagnitude > 0.01)
            {
                float angle = Mathf.Atan2(smoothedVel.y, smoothedVel.x) * Mathf.Rad2Deg;

                Quaternion targetRotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

                AIs[activeAIs[i]].gameObjectRef.transform.rotation = Quaternion.Slerp(
                    AIs[activeAIs[i]].gameObjectRef.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );
            }

        }

        for (int i = activeAIs.Count - 1; i >= 0; i--)
        {
            if (finished[i] && AIs[activeAIs[i]].enemyTarget == null)
            {
                AIs[activeAIs[i]].targetSet = false;
                activeAIs.RemoveAt(i);
            }
        }



        positions.Dispose();
        velocities.Dispose();
        goals.Dispose();
        radiuses.Dispose();
        results.Dispose();
        obstacleVelocities.Dispose();
        obstaclePosRad.Dispose();
        finished.Dispose();
        distanceToKeep.Dispose();
        allPositions.Dispose();
        pushResults.Dispose();
        allRadius.Dispose();
    }
    [BurstCompile]
    public struct DetectNearbyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> allPositions;
        [ReadOnly] public NativeArray<float> detectionRadiusSq;
        public NativeArray<float2> nearbyMap;

        public void Execute(int i)
        {
            float2 myPos = allPositions[i].xy;
            float2 separationVec = float2.zero;

            for (int j = 0; j < allPositions.Length; j++)
            {
                if (i == j) continue;

                float dSq = math.distancesq(myPos, allPositions[j].xy);
                float dist = math.sqrt(dSq);
                //float2 diff = myPos - allPositions[j].xy;
                //float pushStrength = 1.0f - (dist / math.sqrt(detectionRadiusSq));
                //separationVec += math.normalizesafe(diff) * pushStrength;
                if (dSq <= detectionRadiusSq[j] + detectionRadiusSq[i] + 0.2 && dSq > 0.001f)
                {
                    float2 diff = myPos - allPositions[j].xy;

                    separationVec += math.normalizesafe(diff / dSq);
                }
            }
            nearbyMap[i] = separationVec;
        }
    }
    [BurstCompile]
    public struct CalculateRelativeVelocities : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> allPositions;
        [ReadOnly] public NativeArray<float2> velocities;
        [ReadOnly] public NativeArray<float2> goals;
        [ReadOnly] public NativeArray<float> radiuses;
        [ReadOnly] public NativeArray<float3> obstacles;
        [ReadOnly] public NativeArray<float2> obstacleVelocities;
        [ReadOnly] public NativeArray<float> distancesToKeep;
        [ReadOnly] public float maxSpeed;

        public NativeArray<float2> DirMap;
        public NativeArray<bool> gotToTarget;

        public void Execute(int i)
        {
            float2 myPos = allPositions[i];
            float2 myVel = velocities[i];
            float2 toGoal = goals[i] - myPos;
            float distToGoal = math.length(toGoal);

            if (distToGoal < distancesToKeep[i])
            {
                DirMap[i] = float2.zero;
                gotToTarget[i] = true;
                return;
            }
            else
            {
                gotToTarget[i] = false;
            }
            float2 prefVel = (toGoal / distToGoal) * maxSpeed;
            float myRad = radiuses[i];
            float timeHorizonObstacle = 4.0f;
            float timeHorizonAI = 1.5f;

            float2 bestVel = float2.zero;
            float minPenalty = float.MaxValue;

            NativeArray<float> speeds = new NativeArray<float>(3, Allocator.Temp);
            speeds[0] = 0f;
            speeds[1] = maxSpeed * 0.5f;
            speeds[2] = maxSpeed;

            foreach (float speed in speeds)
            {
                for (int step = -8; step <= 8; step++)
                {
                    float angle = step * 0.2f;
                    float cos = math.cos(angle);
                    float sin = math.sin(angle);
                    float2 dir = math.normalizesafe(prefVel);
                    float2 sampleVel = new float2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos) * speed;

                    float penalty = math.distancesq(sampleVel, prefVel);

                    for (int j = 0; j < allPositions.Length; j++)
                    {
                        if (i == j) continue;

                        float2 relPos = myPos - allPositions[j];
                        float2 relVel = sampleVel - velocities[j];
                        float combinedRad = myRad + radiuses[j] + 0.5f;

                        float2 leftNormal = new float2(-relPos.y, relPos.x);
                        float sideBias = (sampleVel.x * 0.01f);
                        penalty += sideBias;

                        float t = PredictCollisionTime(relPos, relVel, combinedRad);
                        if (t <= 0)
                        {
                            float dotProduct = math.dot(relVel, math.normalizesafe(relPos));
                            if (dotProduct < 0)
                                penalty += 10000.0f;
                            else
                                penalty -= 500.0f;
                        }
                        else if (t < timeHorizonAI)
                        {
                            penalty += 2000.0f * (1.0f / (t + 0.1f));
                        }
                    }

                    for (int j = 0; j < obstacles.Length; j++)
                    {
                        float2 relPos = myPos - new float2(obstacles[j].x, obstacles[j].y);
                        float2 relVel = sampleVel - obstacleVelocities[j];
                        float combinedRad = myRad + obstacles[j].z + 0.5f;

                        float2 leftNormal = new float2(-relPos.y, relPos.x);
                        float sideBias = (sampleVel.x * 0.01f);
                        penalty += sideBias;

                        float t = PredictCollisionTime(relPos, relVel, combinedRad);
                        if (t <= 0)
                        {
                            float dotProduct = math.dot(relVel, math.normalizesafe(relPos));
                            if (dotProduct < 0)
                                penalty += 10000.0f;
                            else
                                penalty -= 500.0f;
                        }
                        else if (t < timeHorizonObstacle)
                        {
                            penalty += 2000.0f * (1.0f / (t + 0.1f));
                        }
                    }

                    if (penalty < minPenalty)
                    {
                        minPenalty = penalty;
                        bestVel = sampleVel;
                    }
                }
            }
            speeds.Dispose();
            DirMap[i] = bestVel;
        }
        private float PredictCollisionTime(float2 relPos, float2 relVel, float combinedRad)
        {
            float a = math.dot(relVel, relVel);
            if (a <= 0.0001f) return float.MaxValue;

            float b = math.dot(relPos, relVel);
            float c = math.dot(relPos, relPos) - (combinedRad * combinedRad);
            if (c < 0) return 0;

            float discriminant = b * b - a * c;
            if (discriminant <= 0) return float.MaxValue;

            float t = -(b + math.sqrt(discriminant)) / a;
            return t > 0 ? t : float.MaxValue;
        }
    }

}
