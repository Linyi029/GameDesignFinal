using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject targetPrefab;

    void Start()
    {
         SpawnTarget();
    }

   void SpawnTarget()
    {
        float randomY = Random.Range(-3f, 3f);

        Vector3 spawnPos = new Vector3(-8f, randomY, 0f);

        Instantiate(targetPrefab, spawnPos, Quaternion.identity);
    }
}
