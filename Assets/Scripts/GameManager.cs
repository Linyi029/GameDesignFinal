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

    [Tooltip("每一輪可以點擊的次數。用完後會檢查 accuracy 是否超過 threshold。")]
    public int maxClicks = 20;

    [Tooltip("升級難度需要達到的 accuracy 門檻。0.8 代表 80%。")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("click 用完且 accuracy 高於 threshold 時，target 速度會乘上的倍率。")]
    public float speedIncreaseMultiplier = 1.25f;

    private int remainingClicks;

    //calculate metrics
    private int HitCount = 0;
    private int MissCount = 0;
    private int CorrectReject = 0;
    private int FalseAlarm = 0;

    [SerializeField] private float Accuracy = 0f;



    void Awake()
    {
        Instance = this;
        remainingClicks = maxClicks;

        if (spawner == null)
        {
            spawner = FindObjectOfType<Spawner>();
        }
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
        if (remainingClicks <= 0)
        {
            Debug.Log("No clicks remaining");
            Time.timeScale = 0f;
            return;
        }

        remainingClicks--;

        Debug.Log("Remaining Clicks: " + remainingClicks);

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
            if (ActionZone == null) return false;

            Vector2 p =target.transform.position - ActionZone.position;

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
        UpdateAcc();
        Debug.Log("Success, Score = " + score);
        Destroy(target.gameObject);
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
        UpdateAcc();
        Debug.Log("Punish, Score = " + score);
        Destroy(target.gameObject);
    }

    public void Miss(Target target)
    {
        target.handled = true;
        AddScore(-50);
        MissCount++;
        UpdateAcc();
        Debug.Log("Miss, Score = " + score);
        Destroy(target.gameObject);
    }
    public void AddScore(int amount)
    {
        score += amount;
    }

    private void CheckRoundEnd()
    {
        if (remainingClicks > 0) return;

        if (Accuracy > threshold)
        {
            if (spawner != null)
            {
                spawner.EnableAdvancedMode(speedIncreaseMultiplier);
            }

            remainingClicks = maxClicks;
            Debug.Log("Accuracy passed threshold. Increasing difficulty.");
        }
        else
        {
            Debug.Log("No clicks remaining and accuracy did not pass threshold.");
            Time.timeScale = 0f;
        }
    }
    //更改通關條件：'生命值條件'，注意GO/NOGO OBJ情況



}
