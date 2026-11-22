using UnityEngine;

public class BonusShroom : MonoBehaviour
{
    [SerializeField]
    private GameObject shroomPrefab;
    [SerializeField]
    private float arenaRadius = 20f;
    [SerializeField]
    private float spawnInterval = 1.5f;
    [SerializeField]
    private GameObject warningZonePrefab;
    [SerializeField]
    private Color[] shroomColors;
    private float timer;
    [SerializeField]
    private Vector3 arenaCenter = Vector3.zero;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            ShroomSpawn();
        }
    }
    void ShroomSpawn()
    {
        // náhodný uhol
        float angle = Random.Range(0f, Mathf.PI * 2f);

        // náhodný radius
        float radius = Mathf.Sqrt(Random.value) * arenaRadius;

        // pozícia v rovine XZ (ak používaš Y ako výšku)
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        Vector3 spawnPos = arenaCenter + new Vector3(x, 0f, z);
        GameObject shroom = Instantiate(shroomPrefab, spawnPos, Quaternion.Euler(-90f, 0, 0));
        GameObject zone = Instantiate(warningZonePrefab, spawnPos, Quaternion.identity);
        shroom.transform.parent = zone.transform; 
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
    }
}
