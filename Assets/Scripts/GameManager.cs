using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public Transform ActionZone;
    public float radiusX = 1.5f;
    public float radiusY = 5f;

    [SerializeField] private int score = 0;
    public int threshold = 200;

    void Awake()
    {
        Instance = this;
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

            Vector2 p =target.transform.position - ActionZone.position;

            return (p.x * p.x) / (radiusX * radiusX) + (p.y * p.y) / (radiusY * radiusY)
            <= 1f;
        }

    }

    public void Success(Target target)
    {
        target.handled = true;
        AddScore(100);
        Debug.Log("Success, Score = " + score);
        Destroy(target.gameObject);
    }

    public void Punish(Target target)
    {
        target.handled = true;
        AddScore(-100);
        Debug.Log("Punish, Score = " + score);
        Destroy(target.gameObject);
    }

    public void Miss(Target target)
    {
        target.handled = true;
        AddScore(-50);
        Debug.Log("Miss, Score = " + score);
        Destroy(target.gameObject);
    }
    public void AddScore(int amount)
    {
        score += amount;
    }
}