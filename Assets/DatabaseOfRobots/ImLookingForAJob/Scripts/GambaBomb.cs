using UnityEngine;

public class GambaBomb : MonoBehaviour
{
    [SerializeField] private float delay = 1.5f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float damage = 0.1f;

    [Header("Explosion Effect")]
    [SerializeField] private ParticleSystem explosionEffect;

    private void Start()
    {
        Invoke(nameof(Explode), delay);
    }

    private void Explode()
    {
        if (explosionEffect != null)
        {
            ParticleSystem ps = Instantiate(explosionEffect, transform.position, Quaternion.identity);

            float life = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(ps.gameObject, life);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            var zone = hit.GetComponentInParent<CollisionZoneBehaviour>();
            if (zone != null)
            {
                zone.ChangeHealth(-damage);
            }

            var body = hit.GetComponentInParent<Rigidbody>();
            if (body != null)
            {
                body.AddExplosionForce(explosionForce, transform.position, radius);
            }
        }

        Destroy(gameObject);
    }

    public void Configure(float newDamage, float newRadius, float newForce, float newDelay)
    {
        damage = newDamage;
        radius = newRadius;
        explosionForce = newForce;
        delay = newDelay;
    }
}
