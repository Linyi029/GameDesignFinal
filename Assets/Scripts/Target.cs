using UnityEngine;

public enum TargetType
{
    Go,
    NoGo
}

public class Target : MonoBehaviour
{
    public TargetType type;
    public float speed = 3f;
    public bool handled = false;
    public Vector2 moveDirection;

    public float destroyXLimit = 11f;

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);


        if (!handled && Mathf.Abs(transform.position.x) > destroyXLimit)
        {
            if (type == TargetType.Go)
            {
                GameManager.Instance.Miss(this);
            }
            else
            {
                Destroy(gameObject);
            }

            if (type == TargetType.NoGo)
            {
                Destroy(gameObject);
            }
        }
    }

    // private void OnMouseDown()
    // {
    //     GameManager.Instance.CheckTarget(this);
    // }
  
}