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
    public bool inActionZone = false;
    public bool handled = false;

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
}