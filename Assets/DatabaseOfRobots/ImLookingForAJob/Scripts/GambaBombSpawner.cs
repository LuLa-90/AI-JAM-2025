using UnityEngine;

public class GambaBombSpawner : MonoBehaviour
{
    [SerializeField] private GambaBomb bombPrefab;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float spawnHeight = 0.1f;

    public void SpawnBombWave(GambaEventDefinition def)
    {
        if (bombPrefab == null)
        {
            Debug.LogWarning("GambaBombSpawner: Bomb prefab not assigned.");
            return;
        }

        for (int i = 0; i < def.bombCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = new Vector3(
                transform.position.x + rnd.x,
                transform.position.y + spawnHeight,
                transform.position.z + rnd.y
            );

            var bomb = Object.Instantiate(bombPrefab, pos, Quaternion.identity);
            bomb.Configure(def.bombDamage, def.bombRadius, def.bombExplosionForce, def.bombDelay);
        }
    }
}
