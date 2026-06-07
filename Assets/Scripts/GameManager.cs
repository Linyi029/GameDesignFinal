using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Tooltip("控制 target 生成的 Spawner。若沒有指定，遊戲開始時會自動尋找場景中的 Spawner。")]
    public Spawner spawner;

    [Tooltip("Action Zone 的中心位置。")]
    public Transform ActionZone;

    [Tooltip("Action Zone 的水平半徑。")]
    public float radiusX = 1.5f;

    [Tooltip("Action Zone 的垂直半徑。")]
    public float radiusY = 5f;

    [Header("Start Menu")]
    [Tooltip("遊戲開始時是否顯示開始介面。")]
    public bool showStartMenu = true;

    [Tooltip("開始畫面背景圖。可放水果重複圖案背景。")]
    public Sprite startBackgroundSprite;

    [Tooltip("標題木牌圖片。")]
    public Sprite titleSignSprite;


    [Tooltip("完整的遊戲說明按鈕圖片，圖片內可直接包含文字。")]
    public Sprite instructionButtonSprite;

    [Tooltip("完整的遊戲開始按鈕圖片，圖片內可直接包含文字。")]
    public Sprite startButtonSprite;

    [Tooltip("遊戲說明面板圖片。")]
    public Sprite instructionPanelSprite;

    [Header("Level Intro Panel")]
    [Tooltip("關卡目標黑板背景圖。")]
    public Sprite levelIntroBackgroundSprite;

    [Tooltip("關卡開始按鈕圖片。")]
    public Sprite levelIntroStartButtonSprite;
    
    [Header("Action Zone Visual")]
    public SpriteRenderer trayActionZoneSprite;

    private bool waitingForLevelIntro = false;
    private GameObject startMenuRoot;
    private GameObject instructionPanel;
    private GameObject levelIntroPanel;
    private bool gameStarted = false;

    public TrayFollower trayFollower;


    [SerializeField] private int score = 0;

    [Tooltip("每一輪最大可射擊的 Go target 數量。處理完且生命值仍大於 0 時通關。")]
    public int maxClicks = 20;

    [Tooltip("本關需要完成的成功點擊數量。")]
    public int requiredHits = 5;

    [Tooltip("通關後 target 速度會乘上的倍率。")]
    public float speedIncreaseMultiplier = 1.25f;

    private int remainingClicks;

    [SerializeField] private int maxMistakes = 0;

    [SerializeField] private int health = 0;

    private bool roundEnded = false;

    private int roundHits = 0;
    private int totalRequiredHits = 0;
    private readonly Dictionary<string, int> requiredHitsByFruit = new Dictionary<string, int>();
    private readonly Dictionary<string, int> currentHitsByFruit = new Dictionary<string, int>();

    //calculate metrics
    private int HitCount = 0;
    private int MissCount = 0;
    private int CorrectReject = 0;
    private int FalseAlarm = 0;

    [SerializeField] private float Accuracy = 0f;



    void Awake()
    {
        Instance = this;
        ResetRoundHealth();

        if (spawner == null)
        {
            spawner = FindObjectOfType<Spawner>();
        }

        if (showStartMenu)
        {
            ShowStartMenu();
        }
        else
        {
            StartGame();
        }

    }

    void OnValidate()
    {
        maxClicks = Mathf.Max(1, maxClicks);
        speedIncreaseMultiplier = Mathf.Max(0f, speedIncreaseMultiplier);
        maxMistakes = CalculateAllowedMistakes();
        health = maxMistakes;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckMouseClick();
        }
    }

    void CheckMouseClick()
    {
        if (!gameStarted)
        {
            return;
        }
        if (trayFollower != null)
        {
            trayFollower.ShowAtClickPosition(Input.mousePosition);
        }

        if (roundEnded)
        {
            Debug.Log("Round already ended");
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePoint = new Vector2(mouseWorld.x, mouseWorld.y);

        Collider2D clickedCollider = Physics2D.OverlapPoint(mousePoint);

        if (clickedCollider != null &&
            clickedCollider.TryGetComponent(out Target target))
        {
            CheckTarget(target);
        }


    }

    public void CheckTarget(Target target)
    {
        if (target == null) return;
        if (target.handled) return;

        if (target.type == TargetType.Go)
        {
            if (IsInsideActionZone(target))
            {
                Success(target);
            }
            else
            {
                Miss(target);
            }
        }
        else if (target.type == TargetType.NoGo)
        {
            Punish(target);
        }

        bool IsInsideActionZone(Target target)
        {
            if (ActionZone == null) return false;

            Vector2 p = target.transform.position - ActionZone.position;

            return (p.x * p.x) / (radiusX * radiusX) + (p.y * p.y) / (radiusY * radiusY)
            <= 1f;
        }

    }

    public float UpdateAcc()
    {
        int total = HitCount + CorrectReject + MissCount + FalseAlarm;
        Accuracy = total > 0 ? (float)(HitCount + CorrectReject) / total : 0f;
        Debug.Log("Acc = " + Accuracy);
        return Accuracy;
    }



    // public void Success(Target target) // hit
    // {
    //     target.handled = true;
    //     AddScore(100);
    //     HitCount++;
    //     roundHits++;
    //     RecordTargetHit(target.fruitName);
    //     ConsumeShootableTarget();
    //     UpdateAcc();
    //     Debug.Log("Success, Score = " + score);
    //     Destroy(target.gameObject);
    //     CheckRoundEnd();
    // }
    public void Success(Target target)
    {
        if (!requiredHitsByFruit.ContainsKey(target.fruitName))
        {
            Debug.LogWarning($"Clicked Go but not required fruit: {target.fruitName}");
            Punish(target);
            return;
        }

        target.handled = true;
        AddScore(100);
        HitCount++;
        roundHits++;

        RecordTargetHit(target.fruitName);

        ConsumeShootableTarget();
        UpdateAcc();

        Debug.Log($"Success: {target.fruitName} {currentHitsByFruit[target.fruitName]}/{requiredHitsByFruit[target.fruitName]}");

        Destroy(target.gameObject);
        CheckRoundEnd();
    }

    public void CorrectRej(Target target) // correct rejection
    {
        target.handled = true;
        AddScore(100);
        CorrectReject++;
        UpdateAcc();
        Debug.Log("CorrectReject, Score = " + score);
        Destroy(target.gameObject);
    }

    public void Punish(Target target) //false alarm
    {
        target.handled = true;
        AddScore(-100);
        FalseAlarm++;
        LoseHealth();
        UpdateAcc();
        Debug.Log("Punish, Score = " + score);
        Destroy(target.gameObject);
        CheckRoundEnd();
    }

    public void Miss(Target target)
    {
        target.handled = true;
        AddScore(-50);
        MissCount++;
        ConsumeShootableTarget();
        LoseHealth();
        UpdateAcc();
        Debug.Log("Miss, Score = " + score);
        Destroy(target.gameObject);
        CheckRoundEnd();
    }
    public void Miss_Overtime(Target target)
    {
        target.handled = true;
        AddScore(-50);
        MissCount++;
        ConsumeShootableTarget();
        //LoseHealth();
        UpdateAcc();
        Debug.Log("Miss, Score = " + score);
        Destroy(target.gameObject);
        CheckRoundEnd();
    }
    public void AddScore(int amount)
    {
        score += amount;
    }

    public void StartGame()
    {
        // 取得玩家已解鎖的最高難度
        Difficulty currentDifficulty = DifficultyManager.Instance != null 
            ? DifficultyManager.Instance.GetUnlockedDifficulty()
            : Difficulty.Easy;

        // 設定難度
        if (spawner != null)
        {
            spawner.SetCurrentDifficulty(currentDifficulty);
        }

        GenerateRoundTargetRequirements();

        if (showStartMenu && startMenuRoot != null)
        {
            ShowLevelIntroPanel(currentDifficulty);
            return;
        }

        BeginGameplay();
    }

    private void BeginGameplay()
    {
        gameStarted = true ;
        waitingForLevelIntro = false;
        roundEnded = false;
        Time.timeScale = 1f;

        if (spawner != null)
        {
            spawner.enabled = true;
        }

        if (startMenuRoot != null)
        {
            Destroy(startMenuRoot);
            startMenuRoot = null;
        }
        if (levelIntroPanel != null)
        {
            Destroy(levelIntroPanel);
            levelIntroPanel = null;
        }
        if (trayActionZoneSprite != null)
        {
            radiusX = trayActionZoneSprite.bounds.size.x / 2f;
        }
        if (ActionZone != null && trayActionZoneSprite != null)
        {
            ActionZone.position = trayActionZoneSprite.bounds.center;
            radiusX = trayActionZoneSprite.bounds.size.x / 2f;
        }
    }

    private void ShowStartMenu()
    {
        gameStarted = false;
        Time.timeScale = 0f;

        if (spawner != null)
        {
            spawner.enabled = false;
        }

        EnsureEventSystemExists();

        startMenuRoot = new GameObject("Fruit Start Menu Canvas");
        Canvas canvas = startMenuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = startMenuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        startMenuRoot.AddComponent<GraphicRaycaster>();

        GameObject background = CreateUiObject("Fruit Pattern Background", startMenuRoot.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.98f, 0.94f, 0.86f, 1f);
        backgroundImage.sprite = startBackgroundSprite;
        backgroundImage.preserveAspect = false;
        StretchToParent(background.GetComponent<RectTransform>());



        GameObject sign = CreateUiObject("Title Sign", background.transform);
        Image signImage = sign.AddComponent<Image>();
        signImage.sprite = titleSignSprite;
        signImage.type = Image.Type.Simple;
        signImage.preserveAspect = true;

        RectTransform signRect = sign.GetComponent<RectTransform>();
        signRect.anchorMin = new Vector2(0.5f, 0.5f);
        signRect.anchorMax = new Vector2(0.5f, 0.5f);
        signRect.anchoredPosition = new Vector2(0f, 300f);
        SetSizeFromSprite(signRect, titleSignSprite, new Vector2(760f, 520f), new Vector2(650f, 220f));

        GameObject instructionButton = CreateMenuButton("Instruction Button", background.transform, instructionButtonSprite);
        instructionButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210f, -160f);
        instructionButton.GetComponent<Button>().onClick.AddListener(ToggleInstructions);

        GameObject startButton = CreateMenuButton("Start Button", background.transform, startButtonSprite);
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(210f, -160f);
        startButton.GetComponent<Button>().onClick.AddListener(() => StartGame());

        CreateInstructionPanel(background.transform);
    }

    private GameObject CreateMenuButton(string objectName, Transform parent, Sprite buttonSprite)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = buttonSprite;
        buttonImage.type = Image.Type.Simple;
        buttonImage.preserveAspect = true;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        SetSizeFromSprite(buttonRect, buttonSprite, new Vector2(330f, 110f), new Vector2(330f, 110f));

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.94f, 0.75f, 1f);
        colors.pressedColor = new Color(0.76f, 0.52f, 0.31f, 1f);
        button.colors = colors;

        return buttonObject;
    }


    private void CreateInstructionPanel(Transform parent)
    {
        instructionPanel = CreateUiObject("Instruction Panel", parent);
        Image panelImage = instructionPanel.AddComponent<Image>();
        panelImage.sprite = instructionPanelSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = true;
        
        RectTransform rect = instructionPanel.GetComponent<RectTransform>();
        SetSizeFromSprite(rect, instructionPanelSprite, new Vector2(760f, 300f), new Vector2(760f, 300f));
        rect.anchoredPosition = new Vector2(0f, 10f);
        
        instructionPanel.SetActive(false);

        string instructionText =
            "遊戲說明\n\n" +
            "看準水果進入中間的 Action Zone 時點擊。\n" +
            "點到正確的 Go target 會得分，錯過或點到 No-Go target 會扣生命。\n" +
            "每一關開始前會顯示該難度的目標水果數量與通關條件。";

        Text text = CreateText(
            "Instruction Text",
            instructionPanel.transform,
            instructionText,
            32,
            FontStyle.Bold,
            Color.white
        );
        text.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateTargetRow(Transform parent, FruitOption fruit, int count, float y)
    {
        GameObject row = CreateUiObject("Target Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(420f, 80f);

        GameObject iconObj = CreateUiObject("Fruit Icon", row.transform);
        Image iconImage = iconObj.AddComponent<Image>();

        SpriteRenderer sr = fruit.prefab.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            iconImage.sprite = sr.sprite;
        }

        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-70f, 0f);
        iconRect.sizeDelta = new Vector2(70f, 70f);

        Text countText = CreateText(
            "Target Count",
            row.transform,
            $"× {count}",
            48,
            FontStyle.Bold,
            Color.white
        );

        RectTransform countRect = countText.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 0.5f);
        countRect.anchorMax = new Vector2(0.5f, 0.5f);
        countRect.anchoredPosition = new Vector2(80f, 0f);
        countRect.sizeDelta = new Vector2(180f, 80f);
    }

    private void EnsureStartMenuRootExists()
    {
        if (startMenuRoot != null)
        {
            return;
        }

        EnsureEventSystemExists();

        startMenuRoot = new GameObject("Fruit Start Menu Canvas");

        Canvas canvas = startMenuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = startMenuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        startMenuRoot.AddComponent<GraphicRaycaster>();
    }


    private void PrepareNextLevelIntro()
    {
        waitingForLevelIntro = true;
        gameStarted = false;
        roundEnded = true;
        Time.timeScale = 0f;

        if (spawner != null)
        {
            spawner.enabled = false;
        }

        ResetRoundHealth();

        Difficulty nextDifficulty = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetUnlockedDifficulty()
            : Difficulty.Easy;

        if (spawner != null)
        {
            spawner.SetCurrentDifficulty(nextDifficulty);
        }

        GenerateRoundTargetRequirements();

        EnsureStartMenuRootExists();

        ShowLevelIntroPanel(nextDifficulty);
    }


    private void ShowLevelIntroPanel(Difficulty difficulty)
    {
        if (levelIntroPanel != null)
        {
            Destroy(levelIntroPanel);
        }

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        levelIntroPanel = CreateUiObject("Level Intro Panel", startMenuRoot.transform);

        Image panelImage = levelIntroPanel.AddComponent<Image>();
        panelImage.sprite = levelIntroBackgroundSprite != null
            ? levelIntroBackgroundSprite
            : instructionPanelSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = true;

        RectTransform rect = levelIntroPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        SetSizeFromSprite(rect, panelImage.sprite, new Vector2(1000f, 680f), new Vector2(900f, 600f));

        List<FruitOption> targetFruits = spawner.GetCurrentTargetFruits();

        float startY = 70f;
        float gap = 85f;

        for (int i = 0; i < targetFruits.Count; i++)
        {
            FruitOption fruit = targetFruits[i];

            int count = requiredHitsByFruit.ContainsKey(fruit.fruitName)
                ? requiredHitsByFruit[fruit.fruitName]
                : 0;

            CreateTargetRow(
                levelIntroPanel.transform,
                fruit,
                count,
                startY - i * gap
            );
        }

        Text clickText = CreateText(
            "Click Limit Text",
            levelIntroPanel.transform,
            $"{maxClicks} 步",
            42,
            FontStyle.Bold,
            Color.white
        );

        RectTransform clickRect = clickText.GetComponent<RectTransform>();
        clickRect.anchorMin = new Vector2(0.5f, 0.5f);
        clickRect.anchorMax = new Vector2(0.5f, 0.5f);
        clickRect.anchoredPosition = new Vector2(0f, -140f);
        clickRect.sizeDelta = new Vector2(600f, 80f);

        GameObject startButton = CreateMenuButton(
            "Level Intro Start Button",
            levelIntroPanel.transform,
            levelIntroStartButtonSprite != null
                ? levelIntroStartButtonSprite
                : startButtonSprite
        );

        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -235f);
        startButton.GetComponent<Button>().onClick.AddListener(BeginGameplay);
    }


    private void ToggleInstructions()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(!instructionPanel.activeSelf);
        }
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    private Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        StretchToParent(rect);

        return textComponent;
    }


    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetSizeFromSprite(RectTransform rect, Sprite sprite, Vector2 maxSize, Vector2 fallbackSize)
    {
        if (sprite == null)
        {
            rect.sizeDelta = fallbackSize;
            return;
        }

        Vector2 spriteSize = sprite.rect.size;
        float scale = Mathf.Min(maxSize.x / spriteSize.x, maxSize.y / spriteSize.y);
        rect.sizeDelta = spriteSize * scale;
    }



    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void CheckRoundEnd()
    {
        if (health <= 0)
        {
            roundEnded = true;
            Debug.Log("Health depleted. Game over.");
            Time.timeScale = 0f;
            return;
        }

        if (!AreTargetRequirementsMet())
        {
            if (remainingClicks <= 0)
            {
                Debug.Log($"關卡尚未完成：尚需 {GetRemainingRequiredHitCount()} 個指定水果。請繼續嘗試。\n目前生命值 {health}/{maxMistakes}。");
            }
            return;
        }

        // 通知 DifficultyManager 玩家已完成當前難度
        Difficulty currentDifficulty = spawner != null ? spawner.GetCurrentDifficulty() : Difficulty.Easy;
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.CompleteDifficulty(currentDifficulty);
        }

        if (spawner != null)
        {
            spawner.EnableAdvancedMode(speedIncreaseMultiplier);
        }

        Debug.Log("Health survived. Stage cleared. Increasing difficulty.");
        ResetRoundHealth();
        StartGame();
        PrepareNextLevelIntro();
    }

    private void ResetRoundHealth()
    {
        maxMistakes = CalculateAllowedMistakes();
        health = maxMistakes;
        remainingClicks = maxClicks;
        roundHits = 0;
        currentHitsByFruit.Clear();
        roundEnded = false;
    }

    private int CalculateAllowedMistakes()
    {
        return Mathf.Max(1, maxClicks / 4);
    }

    private void ConsumeShootableTarget()
    {
        remainingClicks = Mathf.Max(0, remainingClicks - 1);
        Debug.Log("Remaining Shootable Targets: " + remainingClicks);
    }

    private void LoseHealth()
    {
        health = Mathf.Max(0, health - 1);
        Debug.Log("Health: " + health + "/" + maxMistakes);
    }

    private int GetRequiredHits()
    {
        if (totalRequiredHits > 0)
        {
            return totalRequiredHits;
        }

        if (spawner != null)
        {
            return spawner.GetRequiredHits();
        }

        return Mathf.Max(1, requiredHits);
    }

    private void GenerateRoundTargetRequirements()
    {
        requiredHitsByFruit.Clear();
        currentHitsByFruit.Clear();

        List<FruitOption> targetFruits = spawner != null
            ? spawner.GetCurrentTargetFruits()
            : null;

        if (targetFruits == null || targetFruits.Count == 0)
        {
            totalRequiredHits = Mathf.Clamp(requiredHits, 1, maxClicks);
            requiredHits = totalRequiredHits;
            return;
        }

        int requiredFruitTypes = Mathf.Min(targetFruits.Count, maxClicks);
        totalRequiredHits = Random.Range(requiredFruitTypes, maxClicks + 1);

        for (int i = 0; i < targetFruits.Count; i++)
        {
            string fruitName = targetFruits[i].fruitName;
            int startingCount = i < requiredFruitTypes ? 1 : 0;
            requiredHitsByFruit[fruitName] = startingCount;
            currentHitsByFruit[fruitName] = 0;
        }

        int remainingHitsToAssign = totalRequiredHits - requiredFruitTypes;
        for (int i = 0; i < remainingHitsToAssign; i++)
        {
            FruitOption fruit = targetFruits[Random.Range(0, targetFruits.Count)];
            requiredHitsByFruit[fruit.fruitName]++;
        }

        requiredHits = totalRequiredHits;
    }

    // private void RecordTargetHit(string fruitName)
    // {
    //     if (!requiredHitsByFruit.ContainsKey(fruitName))
    //     {
    //         return;
    //     }

    //     if (currentHitsByFruit[fruitName] >= requiredHitsByFruit[fruitName])
    //     {
    //         return;
    //     }

    //     currentHitsByFruit[fruitName]++;
    // }

    private void RecordTargetHit(string fruitName)
    {
        if (!requiredHitsByFruit.ContainsKey(fruitName))
        {
            Debug.LogWarning($"No requirement for fruit: {fruitName}");
            return;
        }

        if (!currentHitsByFruit.ContainsKey(fruitName))
        {
            currentHitsByFruit[fruitName] = 0;
        }

        currentHitsByFruit[fruitName]++;

        Debug.Log($"Hit {fruitName}: {currentHitsByFruit[fruitName]}/{requiredHitsByFruit[fruitName]}");
    }

    private int GetRemainingRequiredHitCount()
    {
        if (requiredHitsByFruit.Count == 0)
        {
            return Mathf.Max(0, GetRequiredHits() - roundHits);
        }

        int remaining = 0;

        foreach (KeyValuePair<string, int> requirement in requiredHitsByFruit)
        {
            int currentCount = currentHitsByFruit.ContainsKey(requirement.Key)
                ? currentHitsByFruit[requirement.Key]
                : 0;

            remaining += Mathf.Max(0, requirement.Value - currentCount);
        }

        return remaining;
    }

    // private bool AreTargetRequirementsMet()
    // {
    //     return GetRemainingRequiredHitCount() <= 0;
    // }
    private bool AreTargetRequirementsMet()
    {
        foreach (var requirement in requiredHitsByFruit)
        {
            string fruitName = requirement.Key;
            int required = requirement.Value;

            int current = currentHitsByFruit.ContainsKey(fruitName)
                ? currentHitsByFruit[fruitName]
                : 0;

            if (current < required)
            {
                return false;
            }
        }

        return true;
    }

    private string GetTargetRequirementText()
    {
        if (spawner == null)
        {
            return "指定水果：依照畫面上的 Go target。";
        }

        List<FruitOption> targetFruits = spawner.GetCurrentTargetFruits();
        if (targetFruits == null || targetFruits.Count == 0)
        {
            return "指定水果：依照畫面上的 Go target。";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("指定水果：");

        foreach (FruitOption fruit in targetFruits)
        {
            int count = requiredHitsByFruit.ContainsKey(fruit.fruitName)
                ? requiredHitsByFruit[fruit.fruitName]
                : 0;

            if (count <= 0)
            {
                continue;
            }

            builder.AppendLine($"{GetFruitDisplayName(fruit)} x {count}");
        }

        return builder.ToString().TrimEnd();
    }

    private string GetFruitDisplayName(FruitOption fruit)
    {
        if (fruit == null)
        {
            return "未知水果";
        }

        return string.IsNullOrEmpty(fruit.displayName)
            ? fruit.fruitName
            : fruit.displayName;
    }
    //更改通關條件：'生命值條件'，注意GO/NOGO OBJ情況



}
