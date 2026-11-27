using System.Collections;
//using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class SpeedEffect : Effect
{
    [Header("Effect Settings")]
    public float dragAmount = 8f;
    public float angularDragAmount = 8f;
    public float duration = 3f;

    private float defaultDragAmount = 0f;
    private float defaultAngularDragAmount = 0.05f;

    protected abstract override Reward Reward { get; }

    protected abstract float getDragAmount();
    protected abstract float getAngularDragAmount();

    private RandomSpawnEffect randomSpawn;
    private void Awake()
    {
        randomSpawn = GetComponent<RandomSpawnEffect>();

    }

    protected override void ApplyEffect(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();

        if (rb != null && rb.linearDamping == defaultDragAmount && rb.angularDamping == defaultAngularDragAmount)
        {
            StartCoroutine(ApplySpeed(rb));
        }

        //Destroy(gameObject);

        if (randomSpawn != null)
        {
            Debug.Log("Spawning random effect");
            transform.position = randomSpawn.GetRandomPosition();
        }
    }


    IEnumerator ApplySpeed(Rigidbody rb)
    {

        // Apply slow
        rb.linearDamping = getDragAmount();
        rb.angularDamping = getAngularDragAmount();

        // Wait for duration
        yield return new WaitForSeconds(duration);

        rb.linearDamping = defaultDragAmount;
        rb.angularDamping = defaultAngularDragAmount;
    }


}
