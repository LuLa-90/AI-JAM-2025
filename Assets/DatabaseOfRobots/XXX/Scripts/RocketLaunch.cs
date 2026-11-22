using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class RocketLaunch : MonoBehaviour
{
    [SerializeField]
    private float fallSpeed = 20f;
    [SerializeField]
    private ParticleSystem explosionParticles;
    [SerializeField]
    private AudioClip explosionSound;
    [SerializeField]
    private AudioClip piskanie;
    [SerializeField]
    private GameObject hitPoint;
    

    void Start()
    {
        transform.rotation = Quaternion.Euler(-90, 0, 0);
        MainAudioManager.Instance.SpawnAudio(piskanie, 1f);
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

    }

    private void OnTriggerEnter(Collider collision)
    {
        ArenaEffectZone.hitted = true;
        if (explosionParticles != null)
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
        MainAudioManager.Instance.SpawnAudio(explosionSound, 1f);

        Vector3 pozHit = transform.position;
        pozHit.y = pozHit.y - 0.6f;
        Instantiate(hitPoint, pozHit, Quaternion.identity);
        Destroy(gameObject);
    }
}

