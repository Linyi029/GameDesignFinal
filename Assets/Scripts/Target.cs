using UnityEngine;

public enum TargetType
{
    Go,
    NoGo
}

public class Target : MonoBehaviour
{
    [Tooltip("這顆 target 的類型：Go 需要點擊，No-Go 需要避開。")]
    public TargetType type;

    [Tooltip("這顆 target 的水果名稱，用於關卡條件判斷。")]
    public string fruitName;

    [Tooltip("移動速度。Spawner 生成 target 時可能會覆蓋這個數值。")]
    public float speed = 3f;

    public bool handled = false;

    [Tooltip("target 在世界座標中的移動方向。")]
    public Vector2 moveDirection;

    [Tooltip("target 超過這個 X 距離後，會被判定為 miss 或 correct rejection。")]
    public float destroyXLimit = 11f;
    public float zoneEnterTime = -1f;
    public float totalZoneTime = 0f;
    public bool wasInZone = false;

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);
        bool inZone = GameManager.Instance.IsInsideActionZonePublic(this);

        if (inZone && !wasInZone)
        {
            zoneEnterTime = Time.time;
            wasInZone = true;

            if (type == TargetType.Go)
                GameManager.Instance.RegisterGoEnterZone(this);
        }

        if (!inZone && wasInZone)
        {
            totalZoneTime += Time.time - zoneEnterTime;
            wasInZone = false;
        }


        if (!handled && Mathf.Abs(transform.position.x) > destroyXLimit)
        {
            if (type == TargetType.Go)
            {
                GameManager.Instance.Miss_Overtime(this);
            }
            else
            {
                //GameManager.Instance.CorrectRej(this); //正確拒絕
                handled = true;
                Destroy(gameObject);
            }
        }
    }


  
}
