using UnityEngine;

public class ActionZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Target target))
        {
            target.inActionZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Target target))
        {
            target.inActionZone = false;

            // Go 目標離開 GoArea 還沒被處理 = Miss
            if (target.type == TargetType.Go && !target.handled)
            {
                GameManager.Instance.Miss(target);
            }
        }
    }
}