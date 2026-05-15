using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private Target currentTarget;

    private int score = 0;


    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePoint = new Vector2(mouseWorld.x, mouseWorld.y);
            Collider2D clickedCollider = Physics2D.OverlapPoint(mousePoint);

            if (clickedCollider != null && clickedCollider.TryGetComponent(out Target target))
            {
                currentTarget = target;
                CheckInput();
            }
        }
    }

    public void SetCurrentTarget(Target target)
    {
        currentTarget = target;
    }

    void CheckInput()
    {
        if (currentTarget == null) return;
        if (currentTarget.handled) return;

        if (currentTarget.type == TargetType.Go && currentTarget.inActionZone)
        {
            Success(currentTarget);
        }
        else if (currentTarget.type == TargetType.NoGo)
        {
            Punish(currentTarget);
        }
        else
        {
            Miss(currentTarget);
        }
    }

    public void Success(Target target)
    {
        target.handled = true;
        Debug.Log("Success");
        Destroy(target.gameObject);
    }

    public void Punish(Target target)
    {
        target.handled = true;
        Debug.Log("Punish");
        Destroy(target.gameObject);
    }

    public void Miss(Target target)
    {
        target.handled = true;
        Debug.Log("Miss");
        Destroy(target.gameObject);
    }




    // Score or 關卡 管理
    public void AddScore(int amount)
    {
        score += amount;
    }
    public void MinusScore(int amount)
    {
        score -= amount;
    }



}