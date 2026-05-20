using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    //public GameObject targetPrefab;
    [Tooltip("Go target 使用的 prefab。")]
    public GameObject goPrefab;

    [Tooltip("No-Go target 使用的 prefab。")]
    public GameObject noGoPrefab;

    [Header("Spawn Balance")]
    [Tooltip("單顆模式下生成 No-Go 的機率。多顆模式會固定一波只有一顆 Go，其餘皆為 No-Go。")]
    [Range(0f, 1f)]
    public float noGoChance = 0.3f;

    [Tooltip("每一波 target 生成之間的最短等待時間。")]
    public float minSpawnWait = 0.5f;

    [Tooltip("每一波 target 生成之間的最長等待時間。")]
    public float maxSpawnWait = 2f;

    [Header("Batch Mode")]
    [Tooltip("開啟後，每一波會生成多顆 target；關閉時一次只生成一顆。")]
    public bool spawnMultipleTargets = false;

    [Tooltip("多顆模式下，每一波最少生成幾顆 target。")]
    public int minTargetsPerWave = 2;

    [Tooltip("多顆模式下，每一波最多生成幾顆 target。")]
    public int maxTargetsPerWave = 4;

    [Tooltip("同一波 target 之間盡量保持的 Y 軸距離，用來降低剛生成就重疊或碰撞的機率。")]
    public float sameWaveLaneSpacing = 0.75f;

    [Tooltip("當同一波 target 數量超過 lane 數量時，允許在原 lane 上做的隨機 Y 偏移。")]
    public float laneJitter = 0.6f;

    [Header("Speed")]
    [Tooltip("新生成 target 的基礎移動速度。")]
    public float baseTargetSpeed = 3f;

    [Tooltip("目前速度倍率。實際速度 = Base Target Speed x Speed Multiplier。")]
    public float speedMultiplier = 1f;

    private readonly List<GameObject> currentTargets = new List<GameObject>();

    // 三條 lane
    private float[] lanes = { -2f, 0f, 2f };

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    void OnValidate()
    {
        minSpawnWait = Mathf.Max(0f, minSpawnWait);
        maxSpawnWait = Mathf.Max(minSpawnWait, maxSpawnWait);
        minTargetsPerWave = Mathf.Max(1, minTargetsPerWave);
        maxTargetsPerWave = Mathf.Max(minTargetsPerWave, maxTargetsPerWave);
        sameWaveLaneSpacing = Mathf.Max(0f, sameWaveLaneSpacing);
        laneJitter = Mathf.Max(0f, laneJitter);
        baseTargetSpeed = Mathf.Max(0f, baseTargetSpeed);
        speedMultiplier = Mathf.Max(0f, speedMultiplier);
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            RemoveDestroyedTargets();

            if (currentTargets.Count == 0)
            {
                SpawnWave();

                float waitTime =
                    Random.Range(minSpawnWait, maxSpawnWait);

                yield return new WaitForSeconds(waitTime);
            }

            yield return null;
        }
    }

    void SpawnWave()
    {
        int targetCount = spawnMultipleTargets
            ? Random.Range(minTargetsPerWave, maxTargetsPerWave + 1)
            : 1;
        int goIndex = spawnMultipleTargets ? Random.Range(0, targetCount) : -1;
        List<float> usedLanes = new List<float>();

        for (int i = 0; i < targetCount; i++)
        {
            TargetType targetType = GetTargetTypeForWave(i, goIndex);
            float lane = GetSeparatedLane(usedLanes);
            usedLanes.Add(lane);

            SpawnTarget(targetType, lane);
        }
    }

    TargetType GetTargetTypeForWave(int index, int goIndex)
    {
        if (spawnMultipleTargets)
        {
            return index == goIndex ? TargetType.Go : TargetType.NoGo;
        }

        return Random.value < noGoChance ? TargetType.NoGo : TargetType.Go;
    }

    float GetSeparatedLane(List<float> usedLanes)
    {
        if (usedLanes.Count == 0)
        {
            return lanes[Random.Range(0, lanes.Length)];
        }

        for (int i = 0; i < 30; i++)
        {
            float candidate = lanes[Random.Range(0, lanes.Length)] + Random.Range(-laneJitter, laneJitter);

            if (IsFarEnoughFromWave(candidate, usedLanes))
            {
                return candidate;
            }
        }

        return GetFarthestLane(usedLanes);
    }

    bool IsFarEnoughFromWave(float candidate, List<float> usedLanes)
    {
        foreach (float lane in usedLanes)
        {
            if (Mathf.Abs(candidate - lane) < sameWaveLaneSpacing)
            {
                return false;
            }
        }

        return true;
    }

    float GetFarthestLane(List<float> usedLanes)
    {
        float bestLane = lanes[0];
        float bestDistance = -1f;

        foreach (float lane in lanes)
        {
            CheckFarthestCandidate(lane - laneJitter, usedLanes, ref bestLane, ref bestDistance);
            CheckFarthestCandidate(lane, usedLanes, ref bestLane, ref bestDistance);
            CheckFarthestCandidate(lane + laneJitter, usedLanes, ref bestLane, ref bestDistance);
        }

        return bestLane;
    }

    void CheckFarthestCandidate(float candidate, List<float> usedLanes, ref float bestLane, ref float bestDistance)
    {
        float nearestDistance = float.MaxValue;

        foreach (float usedLane in usedLanes)
        {
            nearestDistance = Mathf.Min(nearestDistance, Mathf.Abs(candidate - usedLane));
        }

        if (nearestDistance > bestDistance)
        {
            bestDistance = nearestDistance;
            bestLane = candidate;
        }
    }

    void SpawnTarget(TargetType targetType, float spawnLane)
    {
        // 隨機左右
        bool spawnLeft =
            Random.value > 0.5f;

        Vector3 spawnPos;
        Vector2 moveDir;

        if (spawnLeft)
        {
            spawnPos =
                new Vector3(-8f, spawnLane, 0f);

            moveDir = Vector2.right;
        }
        else
        {
            spawnPos =
                new Vector3(8f, spawnLane, 0f);

            moveDir = Vector2.left;
        }

        GameObject prefabToSpawn;

        if (targetType == TargetType.NoGo)
        {
            prefabToSpawn = noGoPrefab;
        }
        else
        {
            prefabToSpawn = goPrefab;
        }


        GameObject currentTarget = Instantiate(
            // targetPrefab,
            // spawnPos,
            // Quaternion.identity
            prefabToSpawn,
            spawnPos,
            Quaternion.identity
        );


        // 設定移動方向
        Target target = currentTarget.GetComponent<Target>();

        target.type = targetType;
        target.moveDirection = moveDir;
        target.speed = baseTargetSpeed * speedMultiplier;

        currentTargets.Add(currentTarget);
    }

    public void EnableAdvancedMode(float extraSpeedMultiplier)
    {
        spawnMultipleTargets = true;
        speedMultiplier *= extraSpeedMultiplier;
    }

    private void RemoveDestroyedTargets()
    {
        currentTargets.RemoveAll(target => target == null);
    }
}
