using UnityEngine;

public class Bodik : MonoBehaviour
{
    [SerializeField]
    private float timer = 4f;
    private float time;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time > timer) {
            Destroy(gameObject);
        }
    }
}
