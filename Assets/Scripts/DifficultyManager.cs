using UnityEngine;
using System.Collections.Generic;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}


[System.Serializable]
public class DifficultyConfig
{
    [Tooltip("難度名稱。")]
    public string difficultyName;

    [Tooltip("本難度要求點擊的正確水果種類數。")]
    public int targetFruitCount;

    [Tooltip("本難度會出現的水果總種類數。")]
    public int totalFruitTypesCount;

    [Tooltip("本難度單關需要完成的成功點擊目標數。")]
    public int requiredHits;
}

[System.Serializable]
public class PropertyFilter
{
    [Tooltip("必須包含的屬性條件（屬性名=屬性值）。例：color=Red,shape=Round")]
    public string[] requiredProperties;

    [Tooltip("必須排除的屬性條件（屬性名≠屬性值）。例：color≠Red")]
    public string[] excludedProperties;

    public bool Matches(FruitOption fruit)
    {
        // Check required properties
        if (requiredProperties != null)
        {
            foreach (string prop in requiredProperties)
            {
                if (!string.IsNullOrEmpty(prop) && !MatchesProperty(fruit, prop))
                {
                    return false;
                }
            }
        }

        // Check excluded properties
        if (excludedProperties != null)
        {
            foreach (string prop in excludedProperties)
            {
                if (!string.IsNullOrEmpty(prop) && MatchesProperty(fruit, prop))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool MatchesProperty(FruitOption fruit, string property)
    {
        string[] parts = property.Split('=');
        if (parts.Length != 2) return false;

        string key = parts[0].Trim().ToLower();
        string value = parts[1].Trim().ToLower();

        switch (key)
        {
            case "color":
                return fruit.color.ToLower() == value;
            case "shape":
                return fruit.shape.ToLower() == value;
            case "tag":
                return fruit.HasTag(value);
            default:
                return false;
        }
    }
}

[System.Serializable]
public class FruitOption
{
    [Tooltip("水果名稱，用於識別。")]
    public string fruitName;

    [Tooltip("水果的顯示名稱（用於介紹）。")]
    public string displayName;

    [Tooltip("水果 prefab。")]
    public GameObject prefab;

    [Tooltip("水果顏色（如：Red, Purple, Green, Yellow）。")]
    public string color;

    [Tooltip("水果形狀（如：Round, Cluster, Oval）。")]
    public string shape;

    [Tooltip("其他特徵標籤（如：Rotten, Small）。")]
    public string[] tags = new string[0];

    [Tooltip("水果種類，例如 Apple, Banana, Grape。未熟版本和成熟版本要填一樣。")]
    public string fruitType;

    public bool HasTag(string tag)
    {   
        foreach (string t in tags)
        {
            if (t.ToLower() == tag.ToLower())
                return true;
        }
        return false;
        // foreach (string t in tags)
        // {
        //     if (t == tag) return true;
        // }
        // return false;
    }
}

public class DifficultyManager : MonoBehaviour
{
    [SerializeField] private DifficultyConfig[] difficultyConfigs = new DifficultyConfig[3];

    [SerializeField] private FruitOption[] allFruits;

    // 難度進階追蹤：記錄玩家已解鎖的最高難度
    private Difficulty unlockedDifficulty = Difficulty.Easy;

    public static DifficultyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeDefaultConfigs();
    }

    private void InitializeDefaultConfigs()
    {
        if (difficultyConfigs == null || difficultyConfigs.Length < 3)
        {
            difficultyConfigs = new DifficultyConfig[3];
        }

        difficultyConfigs[0] = new DifficultyConfig
        {
            difficultyName = "Easy",
            targetFruitCount = 1,
            totalFruitTypesCount = 3,
            requiredHits = 5,
            //introText = "挑選出目標水果！\n出現的水果中只有 1 種是對的，其他都要避開。"
        };

        difficultyConfigs[1] = new DifficultyConfig
        {
            difficultyName = "Medium",
            targetFruitCount = 2,
            totalFruitTypesCount = 7,
            requiredHits = 10,
            //introText = "挑選出 2 種目標水果！\n有相似的水果和壞掉的版本會混淆你，要小心。"
        };

        difficultyConfigs[2] = new DifficultyConfig
        {
            difficultyName = "Hard",
            targetFruitCount = 3,
            totalFruitTypesCount = 10,
            requiredHits = 15,
            //introText = "挑選出 3 種目標水果！\n相似水果、壞掉版本、甚至會有蒼蠅出現。集中注意力！"
        };
    }

    public DifficultyConfig GetDifficultyConfig(Difficulty difficulty)
    {
        return difficultyConfigs[(int)difficulty];
    }

    private List<FruitOption> PickRandomFromList(
        List<FruitOption> source,
        int count,
        List<string> excludeFruitNames
    )
    {
        List<FruitOption> candidates = new List<FruitOption>();

        foreach (FruitOption fruit in source)
        {
            if (fruit == null) continue;

            if (excludeFruitNames.Contains(fruit.fruitName))
                continue;

            candidates.Add(fruit);
        }

        List<FruitOption> picked = new List<FruitOption>();

        count = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, candidates.Count);
            picked.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return picked;
    }
    public (List<FruitOption> targetFruits, List<FruitOption> allLevelFruits)
    GenerateLevelFruits(Difficulty difficulty)
    {
        int targetCount = 1;
        int poolMin = 3;
        int poolMax = 3;
        int minUnripeCount = 0;
        bool excludeUnripe = false;

        if (difficulty == Difficulty.Easy)
        {
            targetCount = 1;
            poolMin = 3;
            poolMax = 3;
            minUnripeCount = 0;
            excludeUnripe = true;
        }
        else if (difficulty == Difficulty.Medium)
        {
            targetCount = 2;
            poolMin = 3;
            poolMax = 4;
            minUnripeCount = 1;
            excludeUnripe = false;
        }
        else if (difficulty == Difficulty.Hard)
        {
            targetCount = 3;
            poolMin = 4;
            poolMax = 6;
            minUnripeCount = 2;
            excludeUnripe = false;
        }

        List<FruitOption> candidates = new List<FruitOption>();

        foreach (FruitOption fruit in allFruits)
        {
            if (fruit == null) continue;

            if (excludeUnripe && fruit.HasTag("unripe"))
                continue;

            candidates.Add(fruit);
        }

        List<FruitOption> allLevelFruits = new List<FruitOption>();

        int poolSize = Random.Range(poolMin, poolMax + 1);
        poolSize = Mathf.Min(poolSize, candidates.Count);

        // 先放入指定數量的未熟水果
        List<FruitOption> unripeCandidates = new List<FruitOption>();

        foreach (FruitOption fruit in candidates)
        {
            if (fruit.HasTag("unripe"))
                unripeCandidates.Add(fruit);
        }

        List<FruitOption> pickedUnripe =
            PickRandomFromList(unripeCandidates, minUnripeCount, new List<string>());

        allLevelFruits.AddRange(pickedUnripe);

        List<string> excludeNames = new List<string>();
        foreach (FruitOption fruit in allLevelFruits)
        {
            excludeNames.Add(fruit.fruitName);
        }

        // 補滿 pool
        List<FruitOption> remaining =
            PickRandomFromList(candidates, poolSize - allLevelFruits.Count, excludeNames);

        allLevelFruits.AddRange(remaining);

        // 從 pool 裡挑 target，盡量不要挑未熟
        List<FruitOption> targetCandidates = new List<FruitOption>();

        foreach (FruitOption fruit in allLevelFruits)
        {
            if (!fruit.HasTag("unripe"))
                targetCandidates.Add(fruit);
        }

        if (targetCandidates.Count < targetCount)
            targetCandidates = allLevelFruits;

        List<FruitOption> targetFruits =
            PickRandomFromList(targetCandidates, targetCount, new List<string>());

        return (targetFruits, allLevelFruits);
    }


    public FruitOption GetFruitOption(string fruitName)
    {
        foreach (FruitOption fruit in allFruits)
        {
            if (fruit.fruitName == fruitName)
            {
                return fruit;
            }
        }

        return null;
    }

 

    /// <summary>
    /// 獲取玩家已解鎖的最高難度。遊戲開始時使用。
    /// </summary>
    public Difficulty GetUnlockedDifficulty()
    {
        return unlockedDifficulty;
    }



    /// <summary>
    /// 完成一個難度後，檢查是否有更高難度可解鎖。
    /// </summary>
    public void CompleteDifficulty(Difficulty completedDifficulty)
    {
        // 只有在完成的難度等於或高於當前解鎖難度時才升級
        if ((int)completedDifficulty >= (int)unlockedDifficulty)
        {
            // 升級到下一個難度（如果存在）
            if (completedDifficulty == Difficulty.Easy)
            {
                unlockedDifficulty = Difficulty.Medium;
                Debug.Log("✓ 解鎖 Medium 難度！");
            }
            else if (completedDifficulty == Difficulty.Medium)
            {
                unlockedDifficulty = Difficulty.Hard;
                Debug.Log("✓ 解鎖 Hard 難度！");
            }
            // Hard 是最後難度，沒有更高的了
        }
    }

    /// <summary>
    /// 獲取玩家是否已解鎖特定難度。
    /// </summary>
    public bool IsDifficultyUnlocked(Difficulty difficulty)
    {
        return (int)difficulty <= (int)unlockedDifficulty;
    }

    
}
