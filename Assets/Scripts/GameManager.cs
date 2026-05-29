using UnityEngine;

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


    [SerializeField] private int score = 0;

    [Tooltip("每一輪最大可射擊的 Go target 數量。處理完且生命值仍大於 0 時通關。")]
    public int maxClicks = 20;

    [Tooltip("通關後 target 速度會乘上的倍率。")]
    public float speedIncreaseMultiplier = 1.25f;

    private int remainingClicks;

    [SerializeField] private int maxMistakes = 0;

    [SerializeField] private int health = 0;

    private bool roundEnded = false;

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
    public void AddScore(int amount)
    {
        score += amount;
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

        if (remainingClicks > 0) return;

        if (spawner != null)
        {
            spawner.EnableAdvancedMode(speedIncreaseMultiplier);
        }

        Debug.Log("Health survived. Stage cleared. Increasing difficulty.");
        ResetRoundHealth();
    }

    private void ResetRoundHealth()
    {
        maxMistakes = CalculateAllowedMistakes();
        health = maxMistakes;
        remainingClicks = maxClicks;
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
    //更改通關條件：'生命值條件'，注意GO/NOGO OBJ情況



}
