using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Tooltip("控制 target 生成的 Spawner。若沒有指定，遊戲開始時會自動尋找場景中的 Spawner。")]
    public Spawner spawner;

    [Header("Start Menu")]
    [Tooltip("遊戲開始時是否顯示開始介面。")]
    public bool showStartMenu = true;

    [Tooltip("開始畫面背景圖。可放水果重複圖案背景。")]
    public Sprite startBackgroundSprite;

    [Tooltip("標題木牌圖片。")]
    public Sprite titleSignSprite;

    [Tooltip("完整的遊戲說明按鈕圖片，圖片內可直接包含文字。")]
    public Sprite instructionButtonSprite;

    [Tooltip("完整的遊戲說明按鈕第二張圖片（點擊後顯示的圖片）。")]
    public Sprite instructionButtonAltSprite;

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
    // instructionPanel 保留供向後相容，但實際使用 instructionPanel1/2
    private GameObject instructionPanel;
    private GameObject levelIntroPanel; 
    private bool gameStarted = false;

    public TrayFollower trayFollower;
    private bool currentRunFinished = false;

    // help 按鈕與 instruction panels
    private Button helpButton;
    private Image helpButtonImage;
    private int helpButtonClickState = 0; // 0=none,1=help1,2=help2
    private GameObject instructionPanel1;
    private GameObject instructionPanel2;

    // Feedback fields
    public Sprite feedbackCorrectSprite;
    public Sprite feedbackWrongSprite;
    public Sprite feedbackMissSprite;
    public float feedbackDuration = 0.9f;
    public Vector2 feedbackOffset = new Vector2(0f, 30f);

    public int feedbackImageSize = 160;
    public int feedbackTextSize = 36;

    public float topMessageDuration = 1.4f;

    [SerializeField] private GameObject startMenuCanvas;
    public GameObject difficultySelectCanvas;

    private readonly List<GameObject> healthIconObjects = new List<GameObject>();

    private Dictionary<string, TMP_Text> progressTexts = new Dictionary<string, TMP_Text>();
  
    public TMP_Text clickCountText;
    public BoxCollider2D actionZoneCollider;
    private GameObject resultPanel;

    // Feedback canvas for click feedback
    private Canvas feedbackCanvas;
    private RectTransform feedbackRoot;

    public Sprite instructionPanelSprite1;
    public Sprite instructionPanelSprite2;

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

    public Font uiFont;

    [Header("TextMeshPro Font Asset")]
    [Tooltip("優先使用 Inspector 指定的 TMP FontAsset，若未指定會嘗試從 Resources 載入。路徑是相對於 Resources 資料夾，例如: \"Fonts & Materials/ChironGoRoundTC-VariableFont_wght SDF\"")]
    public TMP_FontAsset uiTMPFont;
    public string uiTMPFontResourcePath = "Fonts & Materials/ChironGoRoundTC-VariableFont_wght SDF";

    private TMP_FontAsset GetTMPFontAsset()
    {
        if (uiTMPFont != null) return uiTMPFont;
        if (!string.IsNullOrEmpty(uiTMPFontResourcePath))
        {
            TMP_FontAsset fa = Resources.Load<TMP_FontAsset>(uiTMPFontResourcePath);
            if (fa != null) return fa;
            Debug.LogWarning($"GetTMPFontAsset: Resources.Load 無法找到 TMP_FontAsset at '{uiTMPFontResourcePath}'");
        }
        Debug.LogWarning("GetTMPFontAsset: uiTMPFont 未設定，請在 Inspector 指派或檢查 uiTMPFontResourcePath。");
        return null;
    }

    [Header("HUD Icon Prefabs")]
    public Transform healthIconContainer;
    public GameObject healthIconPrefab;
    public GameObject clickStatusRoot;
    public Image clickSingleIcon;

    [Header("HUD Sizes")]
    [Tooltip("血量（紅心）icon 大小")]
    public Vector2 healthIconSize = new Vector2(64f, 64f);
    [Tooltip("手勢 / 步數 icon 大小")]
    public Vector2 clickIconSize = new Vector2(140f, 140f);
    [Tooltip("步數文字大小")]
    public int clickCountFontSize = 48;

    [Header("Warning Message")]
    public TMP_Text warningText;
    public float warningDuration = 1.2f;
    private Coroutine warningRoutine;

    private bool roundEnded = false;

    private int roundHits = 0;
    private int totalRequiredHits = 0;
    private readonly Dictionary<string, int> requiredHitsByFruit = new Dictionary<string, int>();
    private readonly Dictionary<string, int> currentHitsByFruit = new Dictionary<string, int>();

    //metrics
    private int HitCount = 0;
    private int MissCount = 0;
    private int CorrectReject = 0;
    private int FalseAlarm = 0;
    
    [SerializeField] private float Accuracy = 0f;

    [System.Serializable]
    public class RunRecord
    {
        public string difficulty;
        public float Time;
        public int target_n;
        public int target;      // 0/1 是否有 Go
        public float zone_time;
        public int click;       // 0/1
        public int hit;         // 0 correct reject, 1 error, 2 miss, 3 hit
        public float RT;
    }

    private List<RunRecord> runRecords = new List<RunRecord>();
    private float levelStartTime;
    private RunRecord currentRun;
    private Target currentGoTarget;
    public Transform targetProgressContainer;
    public GameObject targetProgressPrefab;

    [Header("Result Panel")]
    public Sprite replayButtonSprite;
    public Sprite nextLevelButtonSprite;
    public Sprite homeButtonSprite;
    public Sprite resultPanelSprite;



    void Awake()
    {
        Instance = this;
        SetHudVisible(false);
        ResetRoundHealth();

        if (spawner == null)
        {
            spawner = FindObjectOfType<Spawner>();
        }

        // prepare feedback canvas
        EnsureFeedbackCanvasExists();
        ValidateTMPFontAsset();

        if (showStartMenu)
        {
            ShowStartMenu();
        }
        else
        {
            StartGame();
        }

    }

    // 新增於 GameManager 類別內（例如 Awake() 後方）
    private void ValidateTMPFontAsset()
    {
        TMP_FontAsset fa = GetTMPFontAsset();
        if (fa == null)
        {
            Debug.LogError("ValidateTMPFontAsset: 未找到 TMP Font Asset。請在 GameManager.uiTMPFont 指派或更新 uiTMPFontResourcePath。");
            return;
        }

        // 測試字串，包含你 UI 常用文字
        string[] samples = new string[]
        {
        "挑戰成功！",
        "挑戰失敗",
        "剩餘次數",
        "再玩一次",
        "回主畫面",
        "步",
        "往上還原"
        };

        bool anyMissing = false;
        foreach (var s in samples)
        {
            foreach (char c in s)
            {
                if (!fa.HasCharacter(c))
                {
                    anyMissing = true;
                    Debug.LogWarning($"TMP Font asset 缺少字形: '{c}' (U+{((int)c):X4}) -> 字串範例: \"{s}\"");
                }
            }
        }

        if (!anyMissing)
        {
            Debug.Log("ValidateTMPFontAsset: TMP Font asset 看起來有包含測試字元。");
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
        if (remainingClicks <= 0)
        {
            CheckRoundEnd();
            return;
        }
        if (trayFollower != null)
        {
            trayFollower.ShowAtClickPosition(Input.mousePosition);
        }
        ConsumeShootableTarget();
        UpdateHud();

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
        if (!roundEnded)
            CheckRoundEnd();
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
            if (actionZoneCollider == null || target == null)
                return false;

            return actionZoneCollider.bounds.Contains(target.transform.position);

        }

    }

    public float UpdateAcc()
    {
        int total = HitCount + CorrectReject + MissCount + FalseAlarm;
        Accuracy = total > 0 ? (float)(HitCount + CorrectReject) / total : 0f;
        Debug.Log("Acc = " + Accuracy);
        return Accuracy;
    }


    public void ShowDifficultySelect()
    {
        if (startMenuRoot != null)
            startMenuRoot.SetActive(false);

        if (difficultySelectCanvas != null)
            difficultySelectCanvas.SetActive(true);
    }
    private void SetHudVisible(bool visible)
    {
        if (healthIconContainer != null)
            healthIconContainer.gameObject.SetActive(visible);

        if (clickStatusRoot != null)
            clickStatusRoot.SetActive(visible);

        if (targetProgressContainer != null)
            targetProgressContainer.gameObject.SetActive(visible);
    }
    private void BuildTargetProgressUI()
    {
        foreach (Transform child in targetProgressContainer)
            Destroy(child.gameObject);

        progressTexts.Clear();

        foreach (var requirement in requiredHitsByFruit)
        {
            if (requirement.Value <= 0) continue;
            string fruitName = requirement.Key;

            GameObject item =
                Instantiate(
                    targetProgressPrefab,
                    targetProgressContainer
                );

            Image icon =
                item.transform.Find("FruitIcon")
                .GetComponent<Image>();

            TMP_Text text =
                item.transform.Find("CountText")
                .GetComponent<TMP_Text>();

            FruitOption fruit =
                DifficultyManager.Instance
                .GetFruitOption(fruitName);

            SpriteRenderer sr =
                fruit.prefab.GetComponent<SpriteRenderer>();

            icon.sprite = sr.sprite;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(80f, 80f);

            text.text = $"0/{requirement.Value}";

            progressTexts[fruitName] = text;
        }
    }

    
    private void BuildIcons(
        Transform container,
        GameObject prefab,
        int maxCount,
        List<GameObject> iconObjects
    )
    {
        if (container == null || prefab == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        iconObjects.Clear();

        for (int i = 0; i < maxCount; i++)
        {
            GameObject icon = Instantiate(prefab, container);
            // 調整紅心大小
            RectTransform irt = icon.GetComponent<RectTransform>();
            if (irt != null)
            {
                irt.sizeDelta = healthIconSize;
            }
            iconObjects.Add(icon);
        }
    }

    private void SetIconCount(List<GameObject> iconObjects, int currentCount)
    {
        for (int i = 0; i < iconObjects.Count; i++)
        {
            if (iconObjects[i] != null)
                iconObjects[i].SetActive(i < currentCount);
        }
    }
  
    
    private void UpdateHud()
    {
        SetIconCount(healthIconObjects, health);
        if (clickCountText != null)
        {
            clickCountText.text = $"{remainingClicks}/{maxClicks}";
            // 調整步數字體大小
            clickCountText.fontSize = clickCountFontSize;
        }

        // 調整手勢圖示大小（如果存在）
        if (clickSingleIcon != null)
        {
            RectTransform crt = clickSingleIcon.GetComponent<RectTransform>();
            if (crt != null) crt.sizeDelta = clickIconSize;
        }

        UpdateTargetProgressUI();
    }

    private void UpdateTargetProgressUI()
    {
        foreach (var requirement in requiredHitsByFruit)
        {
            string fruitName = requirement.Key;

            if (!progressTexts.ContainsKey(fruitName))
                continue;

            int current =
                currentHitsByFruit.ContainsKey(fruitName)
                ? currentHitsByFruit[fruitName]
                : 0;

            progressTexts[fruitName].text =
                $"{current}/{requirement.Value}";
        }
    }




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
        UpdateAcc();
        UpdateHud();

        Debug.Log($"Success: {target.fruitName} {currentHitsByFruit[target.fruitName]}/{requiredHitsByFruit[target.fruitName]}");
        // 顯示成功反饋在滑鼠位置（優先用 sprite，無 sprite 則文字）
        ShowClickFeedback("正確 +100", Input.mousePosition, Color.green);
        ShowTopMessage("你得到正確的水果了！", Color.green);
        FinishRun(target, 3, true);
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
        ShowTopMessage("拿錯水果了", Color.red);
        target.handled = true;
        AddScore(-100);
        FalseAlarm++;
        LoseHealth();
        UpdateAcc();
        UpdateHud();
        Debug.Log("Punish, Score = " + score);
        // 顯示錯誤反饋在滑鼠位置
        ShowClickFeedback("錯誤 -100", Input.mousePosition, Color.red);
        FinishRun(target, 1, true);
        Destroy(target.gameObject);
        CheckRoundEnd();
        //點錯了
    }

    public void Miss(Target target)
    {
        ShowWarning("不在可拿取範圍！");
        target.handled = true;
        AddScore(-50);
        MissCount++;
        LoseHealth();
        UpdateAcc();
        UpdateHud();
        Debug.Log("Miss, Score = " + score);
        // miss 顯示在水果消失位置（優先 sprite）
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
            ShowClickFeedback("MISS -50", screenPos, Color.yellow);
        }
        FinishRun(target, 2, false);
        Destroy(target.gameObject);
        CheckRoundEnd();
        //這不是可點擊區域！
    }
    public void Miss_Overtime(Target target)
    {
        //ShowWarning("不在可拿取範圍！");
        target.handled = true;
        AddScore(-50);
        MissCount++;
        UpdateAcc();
        UpdateHud();
        Debug.Log("Miss, Score = " + score);
        // 若需要也可顯示 miss（使用物件位置）
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
            ShowClickFeedback("MISS -50", Camera.main.WorldToScreenPoint(target.transform.position), Color.yellow);
            ShowTopMessage("不小心錯過了", Color.yellow);
        }
        FinishRun(target, 2, false);
        Destroy(target.gameObject);
        CheckRoundEnd();
        //
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void StartGame()
    {
        StartGameWithDifficulty(Difficulty.Easy);
    }

    private void BeginGameplay()
    {   
        SetHudVisible(true);
        UpdateHud();
        levelStartTime = Time.time;
        runRecords.Clear();
        gameStarted = true ;
        waitingForLevelIntro = false;
        roundEnded = false;
        Time.timeScale = 1f;

        if (spawner != null)
        {
            spawner.enabled = true;
            spawner.StartSpawnLoop();
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
            trayActionZoneSprite.gameObject.SetActive(true);
            trayActionZoneSprite.color = new Color(1f, 1f, 1f, 0.25f);
        }
    }

    // ShowStartMenu: 若 inspector 指定了 startMenuCanvas 則使用該物件（可在 Editor 編輯並保存）
    private void ShowStartMenu()
    {
        gameStarted = false;
        Time.timeScale = 0f;

        if (spawner != null)
        {
            spawner.enabled = false;
        }

        EnsureEventSystemExists();

        // 若使用者在 Inspector 指定了 Start Menu Canvas（已在場景或為 prefab），優先使用
        if (startMenuCanvas != null)
        {
            // 如果是 prefab（不在 scene），執行時需要實例化
            if (Application.isPlaying && startMenuCanvas.scene.rootCount == 0)
            {
                startMenuRoot = Instantiate(startMenuCanvas);
                startMenuRoot.name = "Fruit Start Menu Canvas";
            }
            else
            {
                startMenuRoot = startMenuCanvas;
            }

            startMenuRoot.SetActive(true);

            // 嘗試自動綁定 Instruction Button / Panels（如果你已在 Editor 放好 UI）
            Transform bg = startMenuRoot.transform.Find("Fruit Pattern Background") ?? startMenuRoot.transform;
            Transform instrBtn = bg.Find("Instruction Button") ?? startMenuRoot.transform.Find("Instruction Button");
            if (instrBtn != null)
            {
                helpButton = instrBtn.GetComponent<Button>();
                helpButtonImage = instrBtn.GetComponent<Image>();
                if (helpButton != null)
                {
                    helpButton.onClick.RemoveAllListeners();
                    helpButton.onClick.AddListener(OnHelpButtonClicked);
                }
            }

            // 如果場景中已存在 panels，就確保其 CanvasGroup 與 Button 綁定正確
            Transform p1 = bg.Find("Instruction Panel 1");
            Transform p2 = bg.Find("Instruction Panel 2");
            if (p1 != null)
            {
                instructionPanel1 = p1.gameObject;
                CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>() ?? instructionPanel1.AddComponent<CanvasGroup>();
                cg1.blocksRaycasts = false;
                Button b1 = instructionPanel1.GetComponent<Button>() ?? instructionPanel1.AddComponent<Button>();
                b1.onClick.RemoveAllListeners();
                b1.transition = Selectable.Transition.None;
                b1.onClick.AddListener(() => OnInstructionPanelClicked(1));
                instructionPanel1.SetActive(false);
            }
            if (p2 != null)
            {
                instructionPanel2 = p2.gameObject;
                CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>() ?? instructionPanel2.AddComponent<CanvasGroup>();
                cg2.blocksRaycasts = false;
                Button b2 = instructionPanel2.GetComponent<Button>() ?? instructionPanel2.AddComponent<Button>();
                b2.onClick.RemoveAllListeners();
                b2.transition = Selectable.Transition.None;
                b2.onClick.AddListener(() => OnInstructionPanelClicked(2));
                instructionPanel2.SetActive(false);
            }

            instructionPanel = instructionPanel1;
            return;
        }

        // 否則使用動態建立（原先行為）
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
        // Replace binding lines in ShowStartMenu() where instructionButton created
        helpButton = instructionButton.GetComponent<Button>();
        helpButtonImage = instructionButton.GetComponent<Image>();
        helpButton.onClick.RemoveAllListeners(); // 確保不會重複綁定
        helpButton.onClick.AddListener(OnHelpButtonClicked);

        GameObject startButton = CreateMenuButton("Start Button", background.transform, startButtonSprite);
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(210f, -160f);
        //startButton.GetComponent<Button>().onClick.AddListener(() => StartGame());
        startButton.GetComponent<Button>().onClick.AddListener(() => ShowDifficultySelect());

        // 建立兩個 instruction panels（help1 / help2）
        CreateInstructionPanel(background.transform);
    }
    public void SelectDifficultyEasy()
    {
        StartGameWithDifficulty(Difficulty.Easy);
    }

    public void SelectDifficultyMedium()
    {
        StartGameWithDifficulty(Difficulty.Medium);
    }

    public void SelectDifficultyHard()
    {
        StartGameWithDifficulty(Difficulty.Hard);
    }

   
    private void StartGameWithDifficulty(Difficulty selectedDifficulty)
    {
        if (difficultySelectCanvas != null)
            difficultySelectCanvas.SetActive(false);

        if (startMenuRoot != null)
            startMenuRoot.SetActive(true); // 加這行

        if (spawner != null)
        {
            spawner.SetCurrentDifficulty(selectedDifficulty);
        }

        GenerateRoundTargetRequirements();

        BuildIcons(
            healthIconContainer,
            healthIconPrefab,
            maxMistakes,
            healthIconObjects
        );

        BuildTargetProgressUI();
        UpdateHud();

        if (showStartMenu && startMenuRoot != null)
        {   
            ShowLevelIntroPanel(selectedDifficulty);
            return;
        }

        BeginGameplay();
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

    // 建立兩個說明面板（help1/help2），面板本身能接收點擊以切換
    private void CreateInstructionPanel(Transform parent)
    {
        if (instructionPanel1 != null) Destroy(instructionPanel1);
        if (instructionPanel2 != null) Destroy(instructionPanel2);

        // Panel 1
        instructionPanel1 = CreateUiObject("Instruction Panel 1", parent);
        Image panelImage1 = instructionPanel1.AddComponent<Image>();
        panelImage1.sprite = instructionPanelSprite;
        panelImage1.type = Image.Type.Sliced;
        panelImage1.preserveAspect = false;
        RectTransform rect1 = instructionPanel1.GetComponent<RectTransform>();
        SetSizeFromSprite(rect1, instructionPanelSprite, new Vector2(760f, 420f), new Vector2(760f, 300f));
        rect1.anchoredPosition = new Vector2(0f, 10f);
        instructionPanel1.SetActive(false);

        CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>();
        if (cg1 == null) cg1 = instructionPanel1.AddComponent<CanvasGroup>();
        cg1.interactable = true;
        cg1.blocksRaycasts = false;

        Button btn1 = instructionPanel1.GetComponent<Button>();
        if (btn1 == null) btn1 = instructionPanel1.AddComponent<Button>();
        btn1.onClick.RemoveAllListeners();
        btn1.transition = Selectable.Transition.None;
        btn1.onClick.AddListener(() => OnInstructionPanelClicked(1));

        // Panel 2
        instructionPanel2 = CreateUiObject("Instruction Panel 2", parent);
        Image panelImage2 = instructionPanel2.AddComponent<Image>();
        // 如果 inspector 指定了第二張 sprite，優先使用，否則回退到 instructionPanelSprite
        panelImage2.sprite = instructionPanelSprite2 != null ? instructionPanelSprite2 : instructionPanelSprite;
        panelImage2.type = Image.Type.Sliced;
        panelImage2.preserveAspect = false;
        RectTransform rect2 = instructionPanel2.GetComponent<RectTransform>();
        SetSizeFromSprite(rect2, panelImage2.sprite, new Vector2(760f, 420f), new Vector2(760f, 300f));
        rect2.anchoredPosition = new Vector2(0f, 10f);
        instructionPanel2.SetActive(false);

        CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>();
        if (cg2 == null) cg2 = instructionPanel2.AddComponent<CanvasGroup>();
        cg2.interactable = true;
        cg2.blocksRaycasts = false;

        Button btn2 = instructionPanel2.GetComponent<Button>();
        if (btn2 == null) btn2 = instructionPanel2.AddComponent<Button>();
        btn2.onClick.RemoveAllListeners();
        btn2.transition = Selectable.Transition.None;
        btn2.onClick.AddListener(() => OnInstructionPanelClicked(2));

        // 保持相容性 reference
        instructionPanel = instructionPanel1;
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
        iconRect.sizeDelta = new Vector2(100f, 100f);

        TMP_Text countText = CreateText(
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
        BuildTargetProgressUI();
        UpdateHud();

        EnsureStartMenuRootExists();

        ShowLevelIntroPanel(nextDifficulty);
    }


    private void ShowLevelIntroPanel(Difficulty difficulty)
    {
        SetHudVisible(false);
        if (trayActionZoneSprite != null)
            trayActionZoneSprite.gameObject.SetActive(false);
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

        List<FruitOption> targetFruits = spawner != null ? spawner.GetCurrentTargetFruits() : null;

        if (targetFruits == null || targetFruits.Count == 0)
        {
            TMP_Text fallback = CreateText("NoTargetsText", levelIntroPanel.transform, "目前沒有目標水果", 36, FontStyle.Bold, Color.white);
            RectTransform fbRect = fallback.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.5f);
            fbRect.anchorMax = new Vector2(0.5f, 0.5f);
            fbRect.anchoredPosition = new Vector2(0f, 0f);
            fbRect.sizeDelta = new Vector2(600f, 120f);
        }
        else
        {
            float startY = 80f;
            float gap = 55f;
            for (int i = 0; i < targetFruits.Count; i++)
            {
                FruitOption fruit = targetFruits[i];
                int count = requiredHitsByFruit.ContainsKey(fruit.fruitName) ? requiredHitsByFruit[fruit.fruitName] : 0;
                CreateTargetRow(levelIntroPanel.transform, fruit, count, startY - i * gap);
            }

            // Replace the existing clickText / Limit Row block inside ShowLevelIntroPanel(...) with this:

            // 在 ShowLevelIntroPanel(...) 中，替換原先建立 Limit Row / clickText 的區塊為下列程式
            // 只顯示步數文字，並把它固定在黑板中間偏右的位置
            RectTransform panelRect = levelIntroPanel.GetComponent<RectTransform>();
            float panelHeight = panelRect != null ? panelRect.sizeDelta.y : 680f;
            float panelWidth = panelRect != null ? panelRect.sizeDelta.x : 1000f;

            // 決定垂直位置（保留先前行為的比例）
            float yPos = Mathf.Clamp(panelHeight * 0.20f, 40f, 140f);
            // 決定水平偏移為黑板寬度的 25%（可調整為更右：改為 0.28~0.35）
            float xPos = Mathf.Clamp(panelWidth * 0.25f, 120f, 420f);

            // 建立步數文字（使用 TMP）
            GameObject numObj = CreateUiObject("Click Limit Text", levelIntroPanel.transform);
            var clickText = numObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset faClick = GetTMPFontAsset();
            if (faClick != null) clickText.font = faClick;
            clickText.text = $"{maxClicks} 步";
            clickText.fontSize = 60;
            clickText.fontStyle = FontStyles.Bold;
            clickText.color = Color.white;
            clickText.alignment = TextAlignmentOptions.Center;

            RectTransform numRect = numObj.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0.5f, 0f);
            numRect.anchorMax = new Vector2(0.5f, 0f);
            numRect.pivot = new Vector2(0.5f, 0f);
            // xPos 為相對於黑板中心向右的像素，yPos 為相對底部的像素
            numRect.anchoredPosition = new Vector2(xPos - 80f, yPos + 40f);
            numRect.sizeDelta = new Vector2(260f, 100f);
        }

        GameObject backButton = CreateMenuButton("LevelIntro Back Button", levelIntroPanel.transform, homeButtonSprite != null ? homeButtonSprite : startButtonSprite);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(-120f, 28f);
        backButton.GetComponent<Button>().onClick.RemoveAllListeners();
        backButton.GetComponent<Button>().onClick.AddListener(CloseLevelIntroPanel);

        GameObject startButton = CreateMenuButton(
            "Level Intro Start Button",
            levelIntroPanel.transform,
            levelIntroStartButtonSprite != null ? levelIntroStartButtonSprite : startButtonSprite
        );
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0f);
        startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.pivot = new Vector2(0.5f, 0f);
        startRect.anchoredPosition = new Vector2(120f, 28f);
        startButton.GetComponent<Button>().onClick.RemoveAllListeners();
        startButton.GetComponent<Button>().onClick.AddListener(BeginGameplay);
    }

    // 新增：關閉 Level Intro 面板的處理（由左上角返回按鈕觸發）
    private void CloseLevelIntroPanel()
    {
        if (levelIntroPanel != null)
        {
            Destroy(levelIntroPanel);
            levelIntroPanel = null;
        }

        // 顯示主選單 root
        if (startMenuRoot != null)
        {
            startMenuRoot.SetActive(true);
        }
    }
    private void ShowResultPanel(bool success)
    {
        Time.timeScale = 0f;
        gameStarted = false;

        SetHudVisible(false);

        if (trayActionZoneSprite != null)
            trayActionZoneSprite.gameObject.SetActive(false);

        if (resultPanel != null)
            Destroy(resultPanel);

        // 確保有 startMenuRoot，並啟用它
        if (startMenuRoot == null)
            EnsureStartMenuRootExists();

        if (startMenuRoot != null)
            startMenuRoot.SetActive(true);

        // 保證 Canvas 存在並在上層顯示
        Canvas rootCanvas = startMenuRoot != null ? startMenuRoot.GetComponent<Canvas>() : null;
        if (rootCanvas == null && startMenuRoot != null)
            rootCanvas = startMenuRoot.AddComponent<Canvas>();
        if (rootCanvas != null)
            rootCanvas.sortingOrder = Mathf.Max(rootCanvas.sortingOrder, 150);

        resultPanel = CreateUiObject("Result Panel", startMenuRoot != null ? startMenuRoot.transform : null);

        Image panelImage = resultPanel.AddComponent<Image>();
        panelImage.sprite = resultPanelSprite != null
            ? resultPanelSprite
            : levelIntroBackgroundSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = true;

        RectTransform rect = resultPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        SetSizeFromSprite(rect, panelImage.sprite, new Vector2(1000f, 680f), new Vector2(900f, 600f));

        // 確保 panel 在最上層
        resultPanel.transform.SetAsLastSibling();

        string title = success ? "挑戰成功！" : "挑戰失敗";

        TMP_Text titleText = CreateText(
            "Result Title",
            resultPanel.transform,
            title,
            56,
            FontStyle.Bold,
            Color.white
        );

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 160f);
        titleRect.sizeDelta = new Vector2(700f, 90f);

        string stats = $"剩餘次數 : {remainingClicks}\n";

        TMP_Text statsText = CreateText(
            "Result Stats",
            resultPanel.transform,
            stats,
            32,
            FontStyle.Bold,
            Color.white
        );

        RectTransform statsRect = statsText.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, 20f);
        statsRect.sizeDelta = new Vector2(700f, 220f);

        GameObject replayButton = CreateMenuButton(
            "Replay Button",
            resultPanel.transform,
            replayButtonSprite != null ? replayButtonSprite : startButtonSprite
        );

        replayButton.GetComponent<RectTransform>().anchoredPosition =
            success ? new Vector2(-240f, -200f) : new Vector2(-120f, -200f);
        replayButton.GetComponent<Button>().onClick.AddListener(ReplayGame);

        GameObject homeButton = CreateMenuButton(
            "Home Button",
            resultPanel.transform,
            homeButtonSprite
        );
        homeButton.GetComponent<RectTransform>().anchoredPosition =
            success ? new Vector2(0f, -200f) : new Vector2(120f, -200f);
        homeButton.GetComponent<Button>().onClick.AddListener(ReturnToMainMenu);

        if (success)
        {
            GameObject nextButton = CreateMenuButton(
                "Next Level Button",
                resultPanel.transform,
                nextLevelButtonSprite != null ? nextLevelButtonSprite : startButtonSprite
            );

            nextButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(160f, -200f);
            nextButton.GetComponent<Button>().onClick.AddListener(NextLevel);
        }
    }
    // 補回遺失的 NextLevel 方法（呼叫 PrepareNextLevelIntro 並關閉 resultPanel）
    private void NextLevel()
    {
        Time.timeScale = 1f;

        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        PrepareNextLevelIntro();
    }
    // ----- instruction panel switching with frame delay to avoid click-through -----
    private void OnInstructionPanelClicked(int panelIndex)
    {
        if (panelIndex == 1)
        {
            if (instructionPanel1 != null)
            {
                CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>();
                if (cg1 != null) cg1.blocksRaycasts = false;
                instructionPanel1.SetActive(false);
            }

            if (instructionPanel2 != null)
            {
                StartCoroutine(ShowPanel2NextFrame());
            }
        }
        else if (panelIndex == 2)
        {
            if (instructionPanel1 != null)
            {
                instructionPanel1.SetActive(false);
                CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>();
                if (cg1 != null) cg1.blocksRaycasts = false;
            }
            if (instructionPanel2 != null)
            {
                instructionPanel2.SetActive(false);
                CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>();
                if (cg2 != null) cg2.blocksRaycasts = false;
            }
            helpButtonClickState = 0;
        }
    }

    private IEnumerator ShowPanel2NextFrame()
    {
        yield return null;
        if (instructionPanel2 != null)
        {
            instructionPanel2.SetActive(true);
            CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>();
            if (cg2 != null) cg2.blocksRaycasts = true;
            instructionPanel2.transform.SetAsLastSibling();
            helpButtonClickState = 2;
        }
    }

    private void OnHelpButtonClicked()
    {
        // 防禦式檢查
        if (instructionPanel1 == null || instructionPanel2 == null)
        {
            if (startMenuRoot != null)
                CreateInstructionPanel(startMenuRoot.transform.Find("Fruit Pattern Background") ?? startMenuRoot.transform);
            // 如果仍為 null，直接返回
            if (instructionPanel1 == null || instructionPanel2 == null) return;
        }

        // 正常循環：0 -> show panel1, 1 -> show panel2, 2 -> hide both
        if (helpButtonClickState == 0)
        {
            instructionPanel1.SetActive(true);
            instructionPanel2.SetActive(false);
            CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>();
            if (cg1 != null) cg1.blocksRaycasts = true;
            instructionPanel1.transform.SetAsLastSibling();
            helpButtonClickState = 1;
        }
        else if (helpButtonClickState == 1)
        {
            instructionPanel1.SetActive(false);
            instructionPanel2.SetActive(true);
            CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>();
            if (cg2 != null) cg2.blocksRaycasts = true;
            instructionPanel2.transform.SetAsLastSibling();
            helpButtonClickState = 2;
        }
        else
        {
            instructionPanel1.SetActive(false);
            instructionPanel2.SetActive(false);
            CanvasGroup cg1 = instructionPanel1.GetComponent<CanvasGroup>();
            if (cg1 != null) cg1.blocksRaycasts = false;
            CanvasGroup cg2 = instructionPanel2.GetComponent<CanvasGroup>();
            if (cg2 != null) cg2.blocksRaycasts = false;
            helpButtonClickState = 0;
        }
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);

        RectTransform rt = uiObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        return uiObject;
    }

    // 替換：使用 TextMeshProUGUI 建立 UI 文字並回傳 TMP_Text
    private TMP_Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        var tmp = textObject.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset fa = GetTMPFontAsset();
        if (fa != null) tmp.font = fa;

        tmp.text = text ?? "";
        tmp.fontSize = fontSize;
        tmp.fontStyle = (fontStyle == FontStyle.Bold) ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        StretchToParent(rect);

        return tmp;
    }

    private void EnsureFeedbackCanvasExists()
    {
        if (feedbackCanvas != null && feedbackRoot != null) return;

        GameObject go = GameObject.Find("FeedbackCanvas");
        if (go == null)
        {
            go = new GameObject("FeedbackCanvas");
            feedbackCanvas = go.AddComponent<Canvas>();
            feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            feedbackCanvas.sortingOrder = 200;
            go.AddComponent<GraphicRaycaster>();
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
        }
        else
        {
            feedbackCanvas = go.GetComponent<Canvas>();
            if (feedbackCanvas == null)
                feedbackCanvas = go.AddComponent<Canvas>();
            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();
            if (go.GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler cs = go.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920f, 1080f);
            }
        }

        RectTransform frc = feedbackCanvas.GetComponent<RectTransform>();
        if (frc == null) frc = feedbackCanvas.gameObject.AddComponent<RectTransform>();
        frc.anchorMin = Vector2.zero;
        frc.anchorMax = Vector2.one;
        frc.offsetMin = Vector2.zero;
        frc.offsetMax = Vector2.zero;

        feedbackRoot = frc;
    }

    // 替換：ShowClickFeedback（使用 TMP 作為文字 fallback）
    private void ShowClickFeedback(string message, Vector3 screenPosition, Color color, float duration = -1f)
    {
        if (duration <= 0f) duration = feedbackDuration;
        if (feedbackCanvas == null || feedbackRoot == null) EnsureFeedbackCanvasExists();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(feedbackRoot, screenPosition, null, out localPoint);

        Sprite useSprite = null;
        if (!string.IsNullOrEmpty(message))
        {
            if (message.StartsWith("正確") || message.StartsWith("CORRECT") || message.Contains("+"))
                useSprite = feedbackCorrectSprite;
            else if (message.StartsWith("錯誤") || message.StartsWith("WRONG") || message.Contains("-100"))
                useSprite = feedbackWrongSprite;
            else if (message.ToUpper().Contains("MISS"))
                useSprite = feedbackMissSprite;
        }

        Vector2 pos = localPoint + feedbackOffset;

        if (useSprite != null)
        {
            GameObject imgObj = new GameObject("ClickFeedbackImage");
            imgObj.transform.SetParent(feedbackRoot, false);
            Image img = imgObj.AddComponent<Image>();
            img.sprite = useSprite;
            img.preserveAspect = true;

            RectTransform rt = imgObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(feedbackImageSize, feedbackImageSize);

            StartCoroutine(FadeAndDestroyImage(img, duration));
        }
        else
        {
            GameObject txtObj = new GameObject("ClickFeedbackText");
            txtObj.transform.SetParent(feedbackRoot, false);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset fa = GetTMPFontAsset();
            if (fa != null) tmp.font = fa;

            tmp.fontSize = feedbackTextSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = message ?? "";
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 80f);

            StartCoroutine(FadeAndDestroyTextTMP(tmp, duration));
        }
    }

    private IEnumerator FadeAndDestroyImage(Image img, float duration)
    {
        if (img == null) yield break;
        float elapsed = 0f;
        Color start = img.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            if (img != null)
                img.color = new Color(start.r, start.g, start.b, 1f - p);
            if (img != null)
                img.transform.localPosition += new Vector3(0f, Time.unscaledDeltaTime * 40f, 0f);
            yield return null;
        }
        if (img != null)
            Destroy(img.gameObject);
    }

    // 新增：TMP 文字淡出 coroutine
    private IEnumerator FadeAndDestroyTextTMP(TextMeshProUGUI t, float duration)
    {
        if (t == null) yield break;
        float elapsed = 0f;
        Color start = t.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            if (t != null)
                t.color = new Color(start.r, start.g, start.b, 1f - p);
            if (t != null)
                t.transform.localPosition += new Vector3(0f, Time.unscaledDeltaTime * 40f, 0f);
            yield return null;
        }
        if (t != null)
            Destroy(t.gameObject);
    }

    private IEnumerator FadeAndDestroyTopTextTMP(TextMeshProUGUI t, float duration)
    {
        if (t == null) yield break;
        float elapsed = 0f;
        Color start = t.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            if (t != null)
                t.color = new Color(start.r, start.g, start.b, 1f - p);
            yield return null;
        }
        if (t != null)
            Destroy(t.gameObject);
    }

    // 替換：ShowTopMessage 使用 TMP
    private void ShowTopMessage(string message, Color color, float duration = -1f)
    {
        if (duration <= 0f) duration = topMessageDuration;
        if (feedbackCanvas == null || feedbackRoot == null) EnsureFeedbackCanvasExists();

        GameObject topObj = new GameObject("TopMessage");
        topObj.transform.SetParent(feedbackRoot, false);
        var tmp = topObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fa = GetTMPFontAsset();
        if (fa != null) tmp.font = fa;

        tmp.fontSize = Mathf.Clamp(feedbackTextSize + 6, 24, 64);
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = message ?? "";
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform rt = topObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(900f, 80f);

        StartCoroutine(FadeAndDestroyTopTextTMP(tmp, duration));
    }

    private void ReplayGame()
    {
        Time.timeScale = 1f;

        Difficulty currentDifficulty =
            spawner != null ? spawner.GetCurrentDifficulty() : Difficulty.Easy;

        HitCount = 0;
        MissCount = 0;
        FalseAlarm = 0;
        CorrectReject = 0;
        Accuracy = 0f;

        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        ResetRoundHealth();

        StartGameWithDifficulty(currentDifficulty);
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

    private void ClearAllTargets()
    {
        if (spawner != null && spawner.spawnedTargetsRoot != null)
        {
            foreach (Transform child in spawner.spawnedTargetsRoot)
            {
                Destroy(child.gameObject);
            }
        }
    }
    private void EndRound(bool success)
    {
        if (roundEnded) return;

        roundEnded = true;
        gameStarted = false;

        ExportCsv();

        if (spawner != null)
        {
            spawner.StopSpawnLoop();
            spawner.ClearSpawnedTargets();
            spawner.enabled = false;
        }

        ClearAllTargets();

        ShowResultPanel(success);
    }
    private void CheckRoundEnd()
    {
        if (health <= 0)
        {
            Debug.Log("Health depleted. Game over.");
            EndRound(false);
            return;
        }

        if (!AreTargetRequirementsMet())
        {
            if (remainingClicks <= 0)
            {
                Debug.Log($"關卡失敗：尚需 {GetRemainingRequiredHitCount()} 個指定水果。");
                EndRound(false);
            }

            return;
        }

        Difficulty currentDifficulty =
            spawner != null ? spawner.GetCurrentDifficulty() : Difficulty.Easy;

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.CompleteDifficulty(currentDifficulty);
        }
        if (spawner != null)
        {
            spawner.EnableAdvancedMode(speedIncreaseMultiplier);
        }

        Debug.Log("Health survived. Stage cleared.");
        EndRound(true);
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

    
    public void BeginRun(int targetCount, int hasTarget)
    {
        currentRun = new RunRecord
        {
            difficulty = spawner.GetCurrentDifficulty().ToString(),
            Time = Time.time - levelStartTime,
            target_n = targetCount,
            target = hasTarget,
            zone_time = 0f,
            click = 0,
            hit = -1,
            RT = -1f
        };

        currentGoTarget = null;
        currentRunFinished = false;
    }

    public bool IsInsideActionZonePublic(Target target)
    {
        
        if (actionZoneCollider == null)
            return false;

        return actionZoneCollider.bounds.Contains(
            target.transform.position
        );
        
    }

    public void RegisterGoEnterZone(Target target)
    {
        currentGoTarget = target;
    }

    private void FinishRun(Target target, int hitCode, bool clicked)
    {
        if (currentRun == null|| currentRunFinished) return;

        currentRun.click = clicked ? 1 : 0;
        currentRun.hit = hitCode;

        if (target != null)
        {
            float zoneTime = target.totalZoneTime;

            if (target.wasInZone && target.zoneEnterTime >= 0f)
                zoneTime += Time.time - target.zoneEnterTime;

            currentRun.zone_time = zoneTime;

            if (clicked && target.zoneEnterTime >= 0f)
                currentRun.RT = Time.time - target.zoneEnterTime;
        }

        runRecords.Add(currentRun);
        currentRunFinished = true;
       
    }
    private void ExportCsv()
    {
        string folderPath = Application.dataPath + "/CSV";

        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        string path = folderPath + "/run_log.csv";

        // 第一次建立檔案時寫 header
        if (!System.IO.File.Exists(path))
        {
            System.IO.File.WriteAllText(
                path,
                "difficulty,Time,target_n,target,zone_time,click,hit,RT\n"
            );
        }

        StringBuilder sb = new StringBuilder();

        foreach (RunRecord r in runRecords)
        {
            sb.AppendLine(
                $"{r.difficulty}," +
                $"{r.Time}," +
                $"{r.target_n}," +
                $"{r.target}," +
                $"{r.zone_time}," +
                $"{r.click}," +
                $"{r.hit}," +
                $"{r.RT}"
            );
        }

        System.IO.File.AppendAllText(path, sb.ToString());

        Debug.Log($"CSV appended: {path}");

        // 清空避免下一關重複寫入
        runRecords.Clear();
    }
    public void FinishCurrentWaveIfNoClick()
    {
        if (currentRun == null || currentRunFinished) return;

        if (currentRun.target == 1)
        {
            // 有 Go 但沒有成功點擊
            currentRun.click = 0;
            currentRun.hit = 2; // miss
        }
        else
        {
            // 沒有 Go 且沒點擊
            currentRun.click = 0;
            currentRun.hit = 0; // correct reject
        }

        runRecords.Add(currentRun);
        currentRunFinished = true;
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(ShowWarningRoutine(message));
    }

    private IEnumerator ShowWarningRoutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(warningDuration);

        warningText.gameObject.SetActive(false);
    }


    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        if (spawner != null)
        {
            spawner.StopSpawnLoop();
            spawner.ClearSpawnedTargets();
            spawner.enabled = false;
        }

        ClearAllTargets();

        SetHudVisible(false);
        score = 0;
        roundHits = 0;
        remainingClicks = maxClicks;
        currentHitsByFruit.Clear();
        requiredHitsByFruit.Clear();
        ResetRoundHealth();
        ShowStartMenu();
    }



}
