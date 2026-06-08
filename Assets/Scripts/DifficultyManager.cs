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

    [Tooltip("本難度的遊戲說明文字。")]
    [TextArea(2, 4)]
    public string introText;

    [Tooltip("Easy 目標篩選條件（屬性對）")]
    public PropertyFilter easyTargetFilter;

    [Tooltip("Easy 干擾篩選條件（屬性對）")]
    public PropertyFilter easyDistractionFilter;

    [Tooltip("Medium 目標篩選條件")]
    public PropertyFilter mediumTargetFilter;

    [Tooltip("Medium 干擾篩選條件")]
    public PropertyFilter mediumDistractionFilter;

    [Tooltip("Hard 目標篩選條件")]
    public PropertyFilter hardTargetFilter;

    [Tooltip("Hard 干擾篩選條件")]
    public PropertyFilter hardDistractionFilter;

    [Tooltip("Current mission hits")]
    private int currentMissionHits;
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

    public bool HasTag(string tag)
    {
        foreach (string t in tags)
        {
            if (t == tag) return true;
        }
        return false;
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
            introText = "挑選出目標水果！\n出現的水果中只有 1 種是對的，其他都要避開。"
        };

        difficultyConfigs[1] = new DifficultyConfig
        {
            difficultyName = "Medium",
            targetFruitCount = 2,
            totalFruitTypesCount = 7,
            requiredHits = 10,
            introText = "挑選出 2 種目標水果！\n有相似的水果和壞掉的版本會混淆你，要小心。"
        };

        difficultyConfigs[2] = new DifficultyConfig
        {
            difficultyName = "Hard",
            targetFruitCount = 3,
            totalFruitTypesCount = 10,
            requiredHits = 15,
            introText = "挑選出 3 種目標水果！\n相似水果、壞掉版本、甚至會有蒼蠅出現。集中注意力！"
        };
    }

    public DifficultyConfig GetDifficultyConfig(Difficulty difficulty)
    {
        return difficultyConfigs[(int)difficulty];
    }

    /// <summary>
    /// 根據難度生成該級別的水果列表。
    /// Easy: 1個目標水果 + 2個完全不同的干擾水果（顏色和形狀都不同）
    /// Medium: 2個目標水果（同形狀不同顏色）+ 5個干擾水果（同形狀或不同）
    /// Hard: 3個目標水果（不同形狀，但同色或相似）+ 7個干擾水果
    /// </summary>
    public (List<FruitOption> targetFruits, List<FruitOption> allLevelFruits) GenerateLevelFruits(Difficulty difficulty)
    {
        List<FruitOption> targetFruits = new List<FruitOption>();
        List<FruitOption> allLevelFruits = new List<FruitOption>();

        if (difficulty == Difficulty.Easy)
        {
            // Easy: 隨機挑 1 個目標水果
            FruitOption target = PickRandomFruit();
            if (target != null)
            {
                targetFruits.Add(target);
                allLevelFruits.Add(target);

                // 挑 2 個干擾水果：顏色和形狀都要不同
                PropertyFilter filter = new PropertyFilter();
                filter.excludedProperties = new string[] { $"color={target.color}", $"shape={target.shape}" };

                List<FruitOption> distractors = PickRandomFruits(filter, 2, new List<string> { target.fruitName });
                allLevelFruits.AddRange(distractors);
            }
        }
        else if (difficulty == Difficulty.Medium)
        {
            // Medium: 挑 2 個目標水果（同形狀不同顏色）
            FruitOption firstTarget = PickRandomFruit();
            if (firstTarget != null)
            {
                targetFruits.Add(firstTarget);
                allLevelFruits.Add(firstTarget);

                // 挑第 2 個目標：同形狀，不同顏色
                PropertyFilter sameShapeFilter = new PropertyFilter();
                sameShapeFilter.requiredProperties = new string[] { $"shape={firstTarget.shape}" };
                sameShapeFilter.excludedProperties = new string[] { $"color={firstTarget.color}" };

                FruitOption secondTarget = PickRandomFruit(sameShapeFilter, new List<string> { firstTarget.fruitName });
                if (secondTarget != null)
                {
                    targetFruits.Add(secondTarget);
                    allLevelFruits.Add(secondTarget);

                    // 挑 5 個干擾水果
                    // 可以和目標同形狀但不同顏色，或完全不同
                    List<string> excludeFruits = new List<string> { firstTarget.fruitName, secondTarget.fruitName };
                    List<FruitOption> distractors = PickRandomFruits(null, 2, excludeFruits);
                    allLevelFruits.AddRange(distractors);
                }
            }
        }
        else if (difficulty == Difficulty.Hard)
        {
            // Hard: 挑 3 個目標水果（同顏色同形狀，但不同水果名）
            FruitOption firstTarget = PickRandomFruit();
            if (firstTarget != null)
            {
                targetFruits.Add(firstTarget);
                allLevelFruits.Add(firstTarget);

                // 挑第 2 個目標：同顏色同形狀，不同名稱
                PropertyFilter sameColorShapeFilter = new PropertyFilter();
                sameColorShapeFilter.requiredProperties = new string[] { $"color={firstTarget.color}", $"shape={firstTarget.shape}" };

                FruitOption secondTarget = PickRandomFruit(sameColorShapeFilter, new List<string> { firstTarget.fruitName });
                if (secondTarget != null)
                {
                    targetFruits.Add(secondTarget);
                    allLevelFruits.Add(secondTarget);

                    // 挑第 3 個目標：同顏色同形狀，不同名稱
                    FruitOption thirdTarget = PickRandomFruit(sameColorShapeFilter, new List<string> { firstTarget.fruitName, secondTarget.fruitName });
                    if (thirdTarget != null)
                    {
                        targetFruits.Add(thirdTarget);
                        allLevelFruits.Add(thirdTarget);

                        // 挑 7 個干擾水果
                        List<string> excludeFruits = new List<string> { firstTarget.fruitName, secondTarget.fruitName, thirdTarget.fruitName };
                        List<FruitOption> distractors = PickRandomFruits(null, 7, excludeFruits);
                        allLevelFruits.AddRange(distractors);
                    }
                }
            }
        }

        return (targetFruits, allLevelFruits);
    }

    private FruitOption PickRandomFruit()
    {
        if (allFruits == null || allFruits.Length == 0)
            return null;

        return allFruits[Random.Range(0, allFruits.Length)];
    }

    private FruitOption PickRandomFruit(PropertyFilter filter, List<string> excludeFruitNames)
    {
        List<FruitOption> candidates = new List<FruitOption>();

        foreach (FruitOption fruit in allFruits)
        {
            // 檢查是否在排除清單中
            bool isExcluded = false;
            foreach (string name in excludeFruitNames)
            {
                if (fruit.fruitName == name)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (isExcluded)
                continue;

            // 如果有過濾器，檢查是否符合
            if (filter != null && !filter.Matches(fruit))
                continue;

            candidates.Add(fruit);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<FruitOption> PickRandomFruits(PropertyFilter filter, int count, List<string> excludeFruitNames)
    {
        List<FruitOption> candidates = new List<FruitOption>();

        foreach (FruitOption fruit in allFruits)
        {
            // 檢查是否在排除清單中
            bool isExcluded = false;
            foreach (string name in excludeFruitNames)
            {
                if (fruit.fruitName == name)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (isExcluded)
                continue;

            // 如果有過濾器，檢查是否符合
            if (filter != null && !filter.Matches(fruit))
                continue;

            candidates.Add(fruit);
        }

        List<FruitOption> picked = new List<FruitOption>();
        count = Mathf.Min(count, candidates.Count);

        List<int> indices = new List<int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            indices.Add(i);
        }

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, indices.Count);
            picked.Add(candidates[indices[randomIndex]]);
            indices.RemoveAt(randomIndex);
        }

        return picked;
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

    public string GetLevelIntroText(Difficulty difficulty)
    {
        DifficultyConfig config = GetDifficultyConfig(difficulty);
        if (config != null)
        {
            return config.introText;
        }

        return "開始遊戲！";
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
