using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    public class FruitPrefab
    {
        [Tooltip("水果名稱，用於關卡規則對應。")]
        public string fruitName;

        [Tooltip("對應水果的 prefab。")]
        public GameObject prefab;
    }

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
    public int maxTargetsPerWave = 3;

    [Header("Speed")]
    [Tooltip("新生成 target 的基礎移動速度。")]
    public float baseTargetSpeed = 3f;

    [Tooltip("目前速度倍率。實際速度 = Base Target Speed x Speed Multiplier。")]
    public float speedMultiplier = 1f;

    [Header("Fruit Pool")]
    [Tooltip("可用於關卡的水果 prefab 池。")]
    public FruitPrefab[] fruitPool;

    [Tooltip("啟動時指定的難度。")]
    public Difficulty currentDifficulty = Difficulty.Easy;

    // 當前難度級別的目標水果清單和完整水果清單
    private List<FruitOption> currentTargetFruits = new List<FruitOption>();
    private List<FruitOption> currentLevelFruits = new List<FruitOption>();

    private readonly List<GameObject> currentTargets = new List<GameObject>();

    // 三條固定飛行軌道，由上而下。
    private readonly float[] lanes = { 4f, 2f, -3f };

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
        baseTargetSpeed = Mathf.Max(0f, baseTargetSpeed);
        speedMultiplier = Mathf.Max(0f, speedMultiplier);
    }

    /// <summary>
    /// 設定當前難度並使用 DifficultyManager 生成該難度的水果列表。
    /// </summary>
    public void SetCurrentDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;

        if (DifficultyManager.Instance == null)
        {
            Debug.LogError("DifficultyManager instance not found!");
            return;
        }

        // 使用 DifficultyManager 生成該難度的水果
        (List<FruitOption> targets, List<FruitOption> allFruits) = 
            DifficultyManager.Instance.GenerateLevelFruits(difficulty);

        currentTargetFruits = targets;
        currentLevelFruits = allFruits;

        Debug.Log($"Difficulty set to {difficulty}. Targets: {currentTargetFruits.Count}, Total: {currentLevelFruits.Count}");
        Debug.Log("Current target fruits:");
        foreach (FruitOption fruit in currentTargetFruits)
        {
            Debug.Log($"GO target = {fruit.fruitName}");
        }
        Debug.Log("=== Target Fruits ===");
        foreach (FruitOption fruit in currentTargetFruits)
        {
            Debug.Log("GO: " + fruit.fruitName);
        }

        Debug.Log("=== Level Fruits ===");
        foreach (FruitOption fruit in currentLevelFruits)
        {
            Debug.Log("LEVEL: " + fruit.fruitName);
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            RemoveDestroyedTargets();
            if (currentTargets.Count == 0)
            {
                GameManager.Instance.FinishCurrentWaveIfNoClick();

                SpawnWave();

                float waitTime = Random.Range(minSpawnWait, maxSpawnWait);
                yield return new WaitForSeconds(waitTime);
            }
            yield return null;
        }
    }

    void SpawnWave()
    {
        if (currentLevelFruits.Count == 0)
        {
            Debug.LogWarning("No fruits available for this level!");
            return;
        }

        int targetCount = spawnMultipleTargets
            ? Random.Range(minTargetsPerWave, maxTargetsPerWave + 1)
            : 1;
        
        int goIndex = -1;

        if (spawnMultipleTargets)
        {
            bool hasGo = Random.value > noGoChance;

            if (hasGo)
            {
                goIndex = Random.Range(0, targetCount);
            }
        }

         bool hasTarget = goIndex >= 0;

        if (GameManager.Instance != null)
            {
                GameManager.Instance.BeginRun(
                    targetCount,
                    hasTarget ? 1 : 0
                );
            }
        List<float> usedLanes = new List<float>();

        for (int i = 0; i < targetCount; i++)
        {
            TargetType targetType = GetTargetTypeForWave(i, goIndex);
            FruitOption fruitOption = PickFruitOption(targetType);
            
            if (fruitOption == null)
            {
                Debug.LogWarning("Could not pick a fruit for spawn!");
                continue;
            }

            float lane = GetSeparatedLane(usedLanes);
            usedLanes.Add(lane);

            SpawnTarget(targetType, lane, fruitOption);
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

    /// <summary>
    /// 根據目標類型挑選合適的水果。
    /// Go: 從目標水果清單挑選
    /// NoGo: 從完整水果清單中排除目標水果的其他水果
    /// </summary>
    private FruitOption PickFruitOption(TargetType targetType)
    {
        if (targetType == TargetType.Go)
        {
            // 從目標水果中隨機選擇
            if (currentTargetFruits.Count > 0)
            {
                return currentTargetFruits[Random.Range(0, currentTargetFruits.Count)];
            }
        }
        else // TargetType.NoGo
        {
            List<FruitOption> nonTargetFruits = new List<FruitOption>();

            foreach (FruitOption fruit in currentLevelFruits)
            {
                bool isTarget = false;

                foreach (FruitOption target in currentTargetFruits)
                {
                    if (fruit.fruitName == target.fruitName)
                    {
                        isTarget = true;
                        break;
                    }
                }

                if (!isTarget)
                {
                    nonTargetFruits.Add(fruit);
                }
            }

            if (nonTargetFruits.Count > 0)
            {
                return nonTargetFruits[Random.Range(0, nonTargetFruits.Count)];
            }
        }
        Debug.LogWarning("No No-Go fruits available. Check currentLevelFruits / currentTargetFruits.");
        return null;
    }

    private GameObject GetPrefabForFruit(string fruitName)
    {
        if (string.IsNullOrEmpty(fruitName) || fruitPool == null)
        {
            return null;
        }

        foreach (FruitPrefab fruit in fruitPool)
        {
            if (fruit.fruitName == fruitName && fruit.prefab != null)
            {
                return fruit.prefab;
            }
        }

        return null;
    }

    float GetSeparatedLane(List<float> usedLanes)
    {
        if (usedLanes.Count < lanes.Length)
        {
            for (int i = 0; i < 30; i++)
            {
                float candidate = lanes[Random.Range(0, lanes.Length)];

                if (!usedLanes.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        return lanes[usedLanes.Count % lanes.Length];
    }

    
    private void ApplyLaneSorting(GameObject targetObject, float lane)
    {
        int studentOrder;
        int fruitOrder;

        if (lane > 3f)          // 後排
        {
            studentOrder = 5;
            fruitOrder = 6;
        }
        else if (lane > -2.4f)  // 中排
        {
            studentOrder = 25;
            fruitOrder = 26;
        }
        else                    // 前排
        {
            studentOrder = 45;
            fruitOrder = 46;
        }

        // 改整個 prefab 裡所有 SpriteRenderer
        SpriteRenderer[] renderers =
            targetObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr.gameObject == targetObject)
            {
                sr.sortingOrder = fruitOrder;
            }
            else
            {
                sr.sortingOrder = studentOrder;
            }

            Debug.Log($"{sr.gameObject.name} sortingOrder = {sr.sortingOrder}");
        }
    }
    void SpawnTarget(TargetType targetType, float spawnLane, FruitOption fruitOption)
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

        // 優先使用水果對應的 prefab，否則使用預設的 Go/NoGo prefab
        GameObject prefabToSpawn = GetPrefabForFruit(fruitOption.fruitName);
        // if (prefabToSpawn == null)
        // {
        //     prefabToSpawn = targetType == TargetType.NoGo ? noGoPrefab : goPrefab;
        // }

        GameObject currentTarget = Instantiate(
            prefabToSpawn,
            spawnPos,
            Quaternion.identity
        );

        // 設定移動方向
        currentTarget.transform.localScale = Vector3.one * 0.1f;
        Target target = currentTarget.GetComponent<Target>();

        target.type = targetType;
        target.fruitName = fruitOption.fruitName;
        target.moveDirection = moveDir;
        target.speed = baseTargetSpeed * speedMultiplier;
        ApplyLaneSorting(currentTarget, spawnLane);

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

    public Difficulty GetCurrentDifficulty()
    {
        return currentDifficulty;
    }

    public int GetRequiredHits()
    {
        if (DifficultyManager.Instance == null)
        {
            return 5; // 預設值
        }

        DifficultyConfig config = DifficultyManager.Instance.GetDifficultyConfig(currentDifficulty);
        return config != null ? config.requiredHits : 5;
    }


    public List<FruitOption> GetCurrentTargetFruits()
    {
        return currentTargetFruits;
    }

    
}
