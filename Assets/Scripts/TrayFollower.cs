using UnityEngine;
using System.Collections;

public class TrayFollower : MonoBehaviour
{
    public Camera mainCamera;

    public Vector3 defaultPosition = new Vector3(0f, -0.5f, 0f);

    [Tooltip("點擊時，托盤相對於點擊位置的偏移。Y 可以設負數讓托盤在點擊點下方。")]
    public Vector3 clickOffset = new Vector3(0f, -0.1f, 0f);

    public float showDuration = 0.2f;

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

        moveRoutine = StartCoroutine(MoveTrayRoutine(worldPos + clickOffset));
    }

    private IEnumerator MoveTrayRoutine(Vector3 targetPosition)
    {
        transform.position = targetPosition;

        yield return new WaitForSeconds(showDuration);

        transform.position = defaultPosition;
    }
}