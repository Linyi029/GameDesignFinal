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

    [Tooltip("第一頁說明黑板底圖 (help1)")]
    public Sprite instructionPanelSpritePage1;

    [Tooltip("第二頁說明黑板底圖 (help2)")]
    public Sprite instructionPanelSpritePage2;

    [Tooltip("說明頁第一頁的下一頁按鈕 (nextpage_on_help1)")]
    public Sprite helpNextButtonSprite;

    [Tooltip("說明頁第二頁的關閉按鈕 (close_on_help2)")]
    public Sprite helpCloseButtonSprite;

    [Tooltip("完整的遊戲開始按鈕圖片，圖片內可直接包含文字。")]
    public Sprite startButtonSprite;

    [Tooltip("遊戲說明面板圖片。")]
    public Sprite instructionPanelSprite;

    [Header("Level Intro Panel")]
    [Tooltip("關卡目標黑板背景圖。")]
    public Sprite levelIntroBackgroundSprite;

    [Tooltip("關卡開始按鈕圖片。")]
    public Sprite levelIntroStartButtonSprite;





    private GameObject startMenuRoot;
    private GameObject instructionPanel;
    private GameObject levelIntroPanel;
    // 🛠️ 宣告變數來控制分頁
    private Image instructionImageComponent;
    private bool gameStarted = false;
    private GameObject introCanvasObj;


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



    public void Success(Target target) // hit
    {
        target.handled = true;
        AddScore(100);
        HitCount++;
        roundHits++;
        RecordTargetHit(target.fruitName);
        ConsumeShootableTarget();
        UpdateAcc();
        Debug.Log("Success, Score = " + score);
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



    // 🛠️ 新增三個難度按鈕的美術變數 (對應你的 btn_diff_e, btn_diff_m, btn_diff_ha)
    [Header("Difficulty Selection UI")]
    public Sprite easyButtonSprite;
    public Sprite mediumButtonSprite;
    public Sprite hardButtonSprite;

    private GameObject difficultyMenuRoot;

    // 🛠️ 改造原本的 StartGame()：點擊主畫面 START 後，先顯示難度選擇！
    public void StartGame()
    {
        if (startMenuRoot != null)
        {
            startMenuRoot.SetActive(false); // 隱藏主畫面
        }

        ShowDifficultyMenu();
    }

    // 🛠️ 新增：顯示難度選擇畫面
    private void ShowDifficultyMenu()
    {
        difficultyMenuRoot = new GameObject("Difficulty Menu Canvas");
        Canvas canvas = difficultyMenuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        CanvasScaler scaler = difficultyMenuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        difficultyMenuRoot.AddComponent<GraphicRaycaster>();

        // 建立跟主畫面一樣的背景
        GameObject background = CreateUiObject("Diff Background", difficultyMenuRoot.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = startBackgroundSprite;
        StretchToParent(background.GetComponent<RectTransform>());

        // 建立三個大圓形按鈕
        GameObject easyBtn = CreateMenuButton("Easy Button", background.transform, easyButtonSprite);
        RectTransform easyRect = easyBtn.GetComponent<RectTransform>();
        easyRect.anchoredPosition = new Vector2(-450f, 0f);
        easyRect.sizeDelta = new Vector2(240f, 240f); // 🛠️ 強制改為正方形比例！
        easyBtn.GetComponent<Button>().onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
        easyBtn.transform.SetAsLastSibling();

        GameObject medBtn = CreateMenuButton("Medium Button", background.transform, mediumButtonSprite);
        RectTransform medRect = medBtn.GetComponent<RectTransform>();
        medRect.anchoredPosition = new Vector2(0f, 0f);
        medRect.sizeDelta = new Vector2(240f, 240f); // 🛠️ 強制改為正方形比例！
        medBtn.GetComponent<Button>().onClick.AddListener(() => OnDifficultySelected(Difficulty.Medium));
        medBtn.transform.SetAsLastSibling();

        GameObject hardBtn = CreateMenuButton("Hard Button", background.transform, hardButtonSprite);
        RectTransform hardRect = hardBtn.GetComponent<RectTransform>();
        hardRect.anchoredPosition = new Vector2(450f, 0f);
        hardRect.sizeDelta = new Vector2(240f, 240f); // 🛠️ 強制改為正方形比例！
        hardBtn.GetComponent<Button>().onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));
        hardBtn.transform.SetAsLastSibling();
    }

    // 🛠️ 新增：玩家選好難度後的實際開局邏輯
    // 🛠️ 修正後的選好難度開局邏輯
    private void OnDifficultySelected(Difficulty selectedDifficulty)
    {
        if (difficultyMenuRoot != null)
        {
            Destroy(difficultyMenuRoot); // 關閉難度選單
        }

        // 設定難度
        if (spawner != null)
        {
            spawner.SetCurrentDifficulty(selectedDifficulty);
        }

        // 1. 先產生這一關的水果隨機數量需求
        GenerateRoundTargetRequirements();

        // 2. 顯示目標黑板
        ShowLevelIntroPanel(selectedDifficulty);


        // ⚠️ 關鍵修正：把原本可能會自動跑進遊戲的邏輯攔截掉！
        // 讓遊戲時間暫停，等待玩家點擊黑板上的 START 按鈕才執行 BeginGameplay()
        gameStarted = false;
        Time.timeScale = 0f;
        if (spawner != null)
        {
            spawner.enabled = false; // 暫時關閉生成器，不讓小朋友偷跑
        }
    }

    private void BeginGameplay()
    {
        gameStarted = true;
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
        if (introCanvasObj != null)
        {
            Destroy(introCanvasObj);
            introCanvasObj = null;
            levelIntroPanel = null;
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

        // 🛠️ 修正「遊戲說明」按鈕尺寸
        GameObject instructionButton = CreateMenuButton("Instruction Button", background.transform, instructionButtonSprite);
        RectTransform instRect = instructionButton.GetComponent<RectTransform>();
        instRect.anchoredPosition = new Vector2(-210f, -160f);
        instRect.sizeDelta = new Vector2(250f, 100f); // 🌟 改為符合美術圖的 2.5 : 1 黃金比例！
        instructionButton.GetComponent<Button>().onClick.AddListener(ToggleInstructions);

        // 🛠️ 修正「START」按鈕尺寸
        GameObject startButton = CreateMenuButton("Start Button", background.transform, startButtonSprite);
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchoredPosition = new Vector2(210f, -160f);
        startRect.sizeDelta = new Vector2(250f, 100f); // 🌟 改為符合美術圖的 2.5 : 1 黃金比例！
        startButton.GetComponent<Button>().onClick.AddListener(() => StartGame());

        CreateInstructionPanel(background.transform);
    }

    private GameObject CreateMenuButton(string objectName, Transform parent, Sprite buttonSprite)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = buttonSprite;
        buttonImage.type = Image.Type.Simple;

        // 🛠️ 核心修正：徹底關閉 UI 面板按鈕的 preserveAspect，消滅模糊馬賽克！
        buttonImage.preserveAspect = false;

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
        // 建立說明的母面板
        instructionPanel = CreateUiObject("Instruction Panel", parent);
        instructionImageComponent = instructionPanel.AddComponent<Image>();

        // 預設顯示第一頁 (help1)
        instructionImageComponent.sprite = instructionPanelSpritePage1 != null ? instructionPanelSpritePage1 : instructionPanelSprite;
        instructionImageComponent.type = Image.Type.Simple;
        instructionImageComponent.preserveAspect = true;

        RectTransform rect = instructionPanel.GetComponent<RectTransform>();
        SetSizeFromSprite(rect, instructionImageComponent.sprite, new Vector2(900f, 650f), new Vector2(760f, 300f));
        rect.anchoredPosition = new Vector2(0f, 10f);

        // 🛠️ 讓黑板本身可以被點擊 (新增 Button 組件)
        Button panelButton = instructionPanel.AddComponent<Button>();
        panelButton.transition = Button.Transition.None; // 關閉點擊閃爍效果
        panelButton.onClick.AddListener(OnInstructionPanelClicked);

        instructionPanel.SetActive(false);
    }

    // 🛠️ 核心邏輯：點擊黑板本身時的切換行為
    private void OnInstructionPanelClicked()
    {
        // 如果目前顯示的是第一頁 (help1)，點擊就換到第二頁 (help2)
        if (instructionImageComponent.sprite == instructionPanelSpritePage1)
        {
            if (instructionPanelSpritePage2 != null)
            {
                instructionImageComponent.sprite = instructionPanelSpritePage2;
            }
        }
        else
        {
            // 如果已經在第二頁了，點擊黑板就直接「關閉說明」
            ToggleInstructions();
        }
    }

    // 🛠️ 還原原本的 Toggle 邏輯
    private void ToggleInstructions()
    {
        if (instructionPanel != null)
        {
            bool isActive = !instructionPanel.activeSelf;
            instructionPanel.SetActive(isActive);

            if (isActive)
            {
                instructionImageComponent.sprite = instructionPanelSpritePage1;
            }
        }
    }

    // 🛠️ 點擊下一頁的切換切換
    private void ShowInstructionPage2()
    {
        if (instructionImageComponent != null && instructionPanelSpritePage2 != null)
        {
            instructionImageComponent.sprite = instructionPanelSpritePage2; // 換成 help2 底圖
        }
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

        // 🛠️ 修正：連同子物件(Fruit_Visual)的 Sprite 紀錄一起翻出來
        SpriteRenderer sr = fruit.prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            iconImage.sprite = sr.sprite;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(0, 0, 0, 0); // 防呆隱藏
        }

        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-70f, 0f);
        iconRect.sizeDelta = new Vector2(70f, 70f);

        // 建立要求數量文字 (× count)
        Text countText = CreateText(
            "Target Count",
            row.transform,
            $"× {count}",
            48,
            FontStyle.Bold,
            Color.white
        );

        // 🛠️ 核心修正：必須從 countText 的 gameObject 身上去拿 RectTransform，才不會崩潰中斷！
        RectTransform countRect = countText.gameObject.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 0.5f);
        countRect.anchorMax = new Vector2(0.5f, 0.5f);
        countRect.anchoredPosition = new Vector2(80f, 0f);
        countRect.sizeDelta = new Vector2(180f, 80f);
    }

    // private void ShowLevelIntroPanel(Difficulty difficulty)
    // {
    //     if (levelIntroPanel != null)
    //     {
    //         Destroy(levelIntroPanel);
    //     }

    //     if (instructionPanel != null)
    //     {
    //         instructionPanel.SetActive(false);
    //     }

    //     levelIntroPanel = CreateUiObject("Level Intro Panel", startMenuRoot.transform);
    //     Image panelImage = levelIntroPanel.AddComponent<Image>();
    //     panelImage.sprite = instructionPanelSprite;
    //     panelImage.type = Image.Type.Simple;
    //     panelImage.preserveAspect = true;

    //     RectTransform rect = levelIntroPanel.GetComponent<RectTransform>();
    //     rect.anchorMin = new Vector2(0.5f, 0.5f);
    //     rect.anchorMax = new Vector2(0.5f, 0.5f);
    //     SetSizeFromSprite(rect, instructionPanelSprite, new Vector2(820f, 360f), new Vector2(820f, 360f));
    //     rect.anchoredPosition = new Vector2(0f, 40f);

    //     // Text text = CreateText(
    //     //     "Level Intro Text",
    //     //     levelIntroPanel.transform,
    //     //     GetLevelStartText(difficulty),
    //     //     32,
    //     //     FontStyle.Bold,
    //     //     Color.black
    //     // );
    //     // text.alignment = TextAnchor.MiddleCenter;
    //     // RectTransform textRect = text.GetComponent<RectTransform>();
    //     // textRect.offsetMin = new Vector2(40f, 90f);
    //     // textRect.offsetMax = new Vector2(-40f, -40f);

    //     GameObject continueButton = CreateUiObject("Continue Button", levelIntroPanel.transform);
    //     Image buttonImage = continueButton.AddComponent<Image>();
    //     buttonImage.color = new Color(0.76f, 0.52f, 0.31f, 1f);

    //     RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
    //     buttonRect.anchorMin = new Vector2(0.5f, 0f);
    //     buttonRect.anchorMax = new Vector2(0.5f, 0f);
    //     buttonRect.sizeDelta = new Vector2(220f, 64f);
    //     buttonRect.anchoredPosition = new Vector2(0f, 34f);

    //     Button button = continueButton.AddComponent<Button>();
    //     button.onClick.AddListener(BeginGameplay);

    //     Text buttonText = CreateText(
    //         "Continue Text",
    //         continueButton.transform,
    //         "開始關卡",
    //         28,
    //         FontStyle.Bold,
    //         Color.white
    //     );
    //     buttonText.alignment = TextAnchor.MiddleCenter;
    // }

    private void ShowLevelIntroPanel(Difficulty difficulty)
    {
        // 🛠️ 徹底修復灰色殘留：如果之前有殘留的畫布，直接整隻消滅！
        if (introCanvasObj != null)
        {
            Destroy(introCanvasObj);
            introCanvasObj = null;
        }

        if (levelIntroPanel != null)
        {
            Destroy(levelIntroPanel);
        }

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        // 🛠️ 建立畫布與鋪上漂亮的水果重複背景底圖
        introCanvasObj = new GameObject("Level Intro Canvas");
        Canvas canvas = introCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 102;

        CanvasScaler scaler = introCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        introCanvasObj.AddComponent<GraphicRaycaster>();

        // 建立背景
        GameObject background = CreateUiObject("Intro Background", introCanvasObj.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = startBackgroundSprite;
        StretchToParent(background.GetComponent<RectTransform>());

        // 把黑板掛在背景底下
        levelIntroPanel = CreateUiObject("Level Intro Panel", background.transform);

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

        // 大標題「目標」
        Text title = CreateText(
            "Mission Title",
            levelIntroPanel.transform,
            "目標",
            64,
            FontStyle.Bold,
            Color.white
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 180f);
        titleRect.sizeDelta = new Vector2(400f, 90f);

        // 🌟 撈出隨機水果目標並計算
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

        // 限制步數文字
        Text clickText = CreateText(
            "Click Limit Text",
            levelIntroPanel.transform,
            $"限制步數： {maxClicks} 步",
            42,
            FontStyle.Bold,
            Color.white
        );

        RectTransform clickRect = clickText.GetComponent<RectTransform>();
        clickRect.anchorMin = new Vector2(0.5f, 0.5f);
        clickRect.anchorMax = new Vector2(0.5f, 0.5f);
        clickRect.anchoredPosition = new Vector2(0f, -140f);
        clickRect.sizeDelta = new Vector2(600f, 80f);

        // 🛠️ 建立美術組的綠色 START 按鈕
        GameObject startButton = CreateMenuButton(
            "Level Intro Start Button",
            levelIntroPanel.transform,
            levelIntroStartButtonSprite != null
                ? levelIntroStartButtonSprite
                : startButtonSprite
        );

        // 🛠️ 完美對位：讓按鈕浮現在黑板內部的下緣
        RectTransform startBtnRect = startButton.GetComponent<RectTransform>();
        startBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        startBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        startBtnRect.anchoredPosition = new Vector2(0f, -220f); // 🌟 往上拉到合適高度，不再被扯到最底層！
        startBtnRect.sizeDelta = new Vector2(250f, 90f);

        startButton.transform.SetAsLastSibling(); // 確保不被蓋住
        startButton.GetComponent<Button>().onClick.AddListener(BeginGameplay);

        // ❌ 刪除原本最後兩行會把按鈕重新扯到底部、重複綁定監聽器的程式碼！
    }



    // private string GetLevelStartText(Difficulty difficulty)
    // {
    //     string levelText =
    //         $"{GetDifficultyName(difficulty)}難度\n\n" +
    //         $"{GetTargetRequirementText()}\n\n" +
    //         $"通關條件：成功點擊 {GetRequiredHits()} 個指定水果\n" +
    //         $"生命值：{maxMistakes}\n" +
    //         $"最大可點擊次數：{maxClicks}";

    //     return levelText;
    // }

    private string GetDifficultyName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "簡單",
            Difficulty.Medium => "普通",
            Difficulty.Hard => "困難",
            _ => "未知"
        };

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

        // 🛠️ 修正：改用安全穩定的系統 Arial 載入方式，解決傳入單一字串報錯的問題
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 防呆：如果 Arial 還找不到，再退回 Legacy 字體
        if (textComponent.font == null)
        {
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = TextAnchor.MiddleCenter;

        // 將溢出模式改為 Overflow！確保數字和 × 絕對不會被文字框裁切
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

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
    }

    private void ResetRoundHealth()
    {
        maxMistakes = CalculateAllowedMistakes();
        health = maxMistakes;
        remainingClicks = maxClicks;
        roundEnded = false;

        // 🛠️ 徹底修復：重置所有通關所需的數據與計算指標
        roundHits = 0;
        totalRequiredHits = 0;
        requiredHitsByFruit.Clear();
        currentHitsByFruit.Clear();

        // 重置認知測驗的專注力數據指標
        HitCount = 0;
        MissCount = 0;
        CorrectReject = 0;
        FalseAlarm = 0;
        Accuracy = 0f;

        Debug.Log("✓ 關卡數據已完全重置，防殘留保護已啟動。");
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

    // private string GetLevelIntroText()
    // {
    //     if (DifficultyManager.Instance != null)
    //     {
    //         Difficulty difficulty = spawner != null ? spawner.GetCurrentDifficulty() : Difficulty.Easy;
    //         return DifficultyManager.Instance.GetLevelIntroText(difficulty);
    //     }

    //     return "點擊目標水果，避開不該點擊的物件。";
    // }
    private string GetLevelIntroText()
    {
        if (DifficultyManager.Instance != null)
        {
            Difficulty difficulty =
                spawner != null ?
                spawner.GetCurrentDifficulty() :
                Difficulty.Easy;

            return DifficultyManager.Instance.GetLevelIntroText(difficulty);
        }

        return "點擊目標水果，避開不該點擊的物件。";
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

    private void RecordTargetHit(string fruitName)
    {
        if (!requiredHitsByFruit.ContainsKey(fruitName))
        {
            return;
        }

        if (currentHitsByFruit[fruitName] >= requiredHitsByFruit[fruitName])
        {
            return;
        }

        currentHitsByFruit[fruitName]++;
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

    private bool AreTargetRequirementsMet()
    {
        return GetRemainingRequiredHitCount() <= 0;
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
