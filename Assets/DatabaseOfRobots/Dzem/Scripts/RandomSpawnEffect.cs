using UnityEngine;

public class RandomSpawnEffect : MonoBehaviour
{
    public GameObject platformPrefab;
    public float maxRadius = 5f;

    public Vector3 GetRandomPosition()
    {
        Transform platformTransform = platformPrefab.transform;
 
        Vector3 center = platformTransform.position;

        Vector2 randomCircle = Random.insideUnitCircle * maxRadius;

        Vector3 spawnPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        

        return spawnPos;
    }
}
