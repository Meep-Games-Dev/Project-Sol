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
    public float maxSpeed;
    public int AICount;
    public GameObject AIPrefab;
    public float radius;
    public float pushSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < AICount; i++)
        {
            GameObject instantiatedObj = Instantiate(AIPrefab, new Vector3(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-50, 50), 0), Quaternion.identity);

            RVOAI ai = new RVOAI
            {
                pos = instantiatedObj.transform.position,
                vel = Vector2.zero,
                gameObjectRef = instantiatedObj,
                rad = radius
            };
            AIs.Add(ai);
        }

        ObstacleTag[] obstacleComponents = FindObjectsByType<ObstacleTag>(FindObjectsSortMode.None);

        for (int i = 0; i < obstacleComponents.Length; i++)
        {
            CircleCollider2D collider = obstacleComponents[i].GetComponent<CircleCollider2D>();

            float rad = collider.radius * math.max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);

            obstacles.Add(new RVOobstacle
            {
                rad = rad,
                pos = (Vector2)collider.gameObject.transform.position,
                objRef = collider.gameObject
            });
        }
    }
    public void SendAI(RVOAI ai, Vector2 position)
    {
        if (!activeAIs.Contains(AIs.IndexOf(ai)))
        {
            ai.target = position;
            ai.enemyTarget = null;
            ai.targetSet = true;
            ai.distanceToKeep = 1;
            activeAIs.Add(AIs.IndexOf(ai));
        }
        else
        {
            ai.target = position;
            ai.enemyTarget = null;
            ai.targetSet = true;
            ai.distanceToKeep = 1;
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
        for (int i = 0; i < activeAIs.Count; i++)
        {
            RVOAI ai = AIs[activeAIs[i]];
            ai.pos = ai.gameObjectRef.transform.position;
            ai.target = AIs[activeAIs[i]].target;
            ai.rad = radius;
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
        activeAIs.RemoveAll(index => !AIs[index].targetSet && AIs[index].enemyTarget != null);
        for (int i = 0; i < AIs.Count; i++)
        {
            allPositions[i] = (Vector2)AIs[i].gameObjectRef.transform.position;
        }
        DetectNearbyJob nearbyJob = new DetectNearbyJob
        {
            allPositions = allPositions,
            nearbyMap = pushResults,
            detectionRadiusSq = radius
        };
        for (int i = 0; i < pushResults.Length; i++) pushResults[i] = float2.zero;
        JobHandle nearbyHandle = job.Schedule(AIs.Count, 64);
        nearbyHandle.Complete();

        for (int i = 0; i < AIs.Count; i++)
        {
            AIs[i].gameObjectRef.transform.Translate((Vector2)pushResults[i] * pushSpeed * Time.deltaTime);
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
    }
    [BurstCompile]
    public struct DetectNearbyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> allPositions;
        [ReadOnly] public float detectionRadiusSq;
        public NativeArray<float2> nearbyMap;

        public void Execute(int i)
        {
            float2 myPos = allPositions[i].xy;
            float2 separationVec = float2.zero;

            for (int j = 0; j < allPositions.Length; j++)
            {
                if (i == j) continue;

                float dSq = math.distancesq(myPos, allPositions[j].xy);

                if (dSq <= 4.0f && dSq > 0.001f)
                {
                    float2 diff = myPos - allPositions[j].xy;

                    separationVec += diff / dSq;
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
            float timeHorizon = 2.0f;

            float2 bestVel = float2.zero;
            float minPenalty = float.MaxValue;

            NativeArray<float> speeds = new NativeArray<float>(3, Allocator.Temp);
            speeds[0] = 0f;
            speeds[1] = maxSpeed * 0.5f;
            speeds[2] = maxSpeed;

            foreach (float speed in speeds)
            {
                for (int step = -4; step <= 4; step++)
                {
                    float angle = step * 0.4f;
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
                        float sideBias = math.dot(sampleVel, leftNormal) * 0.01f;
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
                        else if (t < timeHorizon)
                        {
                            penalty += 2000.0f * (1.0f / (t + 0.1f));
                        }
                    }

                    for (int j = 0; j < obstacles.Length; j++)
                    {
                        float2 obsPos = new float2(obstacles[j].x, obstacles[j].y);
                        float2 relPos = myPos - obsPos;
                        float2 relVel = sampleVel - obstacleVelocities[j];
                        float combinedRad = myRad + obstacles[j].z + 0.2f;

                        float t = PredictCollisionTime(relPos, relVel, combinedRad);
                        float distSq = math.lengthsq(relPos);
                        if (distSq < combinedRad * combinedRad)
                        {
                            float2 pushDir = math.normalizesafe(relPos);
                            float dot = math.dot(sampleVel, pushDir);

                            if (dot < 0)
                            {
                                penalty += 5000.0f * (combinedRad - math.sqrt(distSq));
                            }
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
