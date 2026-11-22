using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;

public class RockedTrapSkript : MonoBehaviour
{
public GameObject rocketPrefab;
    [SerializeField]
    private float arenaRadius = 20f;
    [SerializeField]
    private float spawnHeight = 30f;
    [SerializeField]
    private float spawnInterval = 1.5f;
    [SerializeField]
    private Vector3 arenaCenter = Vector3.zero;
    [SerializeField]
    private GameObject warningZonePrefab;
    [SerializeField]
    private int timesToSpawn;

    private float timer = 0;
    public static bool isActive = false;


    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval && timesToSpawn > 0)
        {
            //timesToSpawn--;
            timer = 0f;
            
            SpawnRocket();
        }
        if (timesToSpawn <= 0)
        {
            isActive = false;
        }
    }

    void SpawnRocket()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);

        float radius = Mathf.Sqrt(Random.value) * arenaRadius;

        
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        Vector3 spawnPos = arenaCenter + new Vector3(x, spawnHeight, z);
        Vector3 groundPos = arenaCenter + new Vector3(x, 0f, z);

        Instantiate(warningZonePrefab, groundPos, Quaternion.identity);

        Instantiate(rocketPrefab, spawnPos, Quaternion.identity);
        
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
    }
}
