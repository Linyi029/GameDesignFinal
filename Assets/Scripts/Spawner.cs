using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    //public GameObject targetPrefab;
    public GameObject goPrefab;
    public GameObject noGoPrefab;

    private GameObject currentTarget;

    // 三條 lane
    private float[] lanes = { -2f, 0f, 2f };

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!currentTarget)
            {
                SpawnTarget();

                float waitTime =
                    Random.Range(0.5f, 2f);

                yield return new WaitForSeconds(waitTime);
            }

            yield return null;
        }
    }

    void SpawnTarget()
    {
        // 隨機 lane
        float randomLane =
            lanes[Random.Range(0, lanes.Length)];

        // 隨機左右
        bool spawnLeft =
            Random.value > 0.5f;

        Vector3 spawnPos;
        Vector2 moveDir;

        if (spawnLeft)
        {
            spawnPos =
                new Vector3(-8f, randomLane, 0f);

            moveDir = Vector2.right;
        }
        else
        {
            spawnPos =
                new Vector3(8f, randomLane, 0f);

            moveDir = Vector2.left;
        }

        GameObject prefabToSpawn;

        bool spawnNoGo =
        Random.value < 0.3f;


        if (spawnNoGo)
        {
            prefabToSpawn = noGoPrefab;
        }
        else
        {
            prefabToSpawn = goPrefab;
        }


        currentTarget = Instantiate(
            // targetPrefab,
            // spawnPos,
            // Quaternion.identity
            prefabToSpawn,
            spawnPos,
            Quaternion.identity
        );


        // 設定移動方向
        Target target = currentTarget.GetComponent<Target>();

        target.moveDirection = moveDir;
    }
}