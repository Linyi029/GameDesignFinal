using UnityEngine;
using System.Collections;

public class TrayFollower : MonoBehaviour
{
    public Camera mainCamera;
    public Vector3 defaultPosition = new Vector3(0f, -4f, 0f);

    [Tooltip("托盤移到點擊位置後停留多久")]
    public float showDuration = 0.15f;

    private Coroutine moveRoutine;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        transform.position = defaultPosition;
    }

    public void ShowAtClickPosition(Vector3 screenPosition)
    {
        if (mainCamera == null) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPos.z = transform.position.z;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveTrayRoutine(worldPos));
    }

    private IEnumerator MoveTrayRoutine(Vector3 targetPosition)
    {
        transform.position = targetPosition;

        yield return new WaitForSeconds(showDuration);

        transform.position = defaultPosition;
    }
}