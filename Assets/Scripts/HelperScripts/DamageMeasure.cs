using UnityEngine;

public class DamageMeasure : MonoBehaviour
{
    BoxCollider boxCollider;

    private void Start() {
        boxCollider = gameObject.GetComponent<BoxCollider>();
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision) {
        float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;// / 100000f;
        Debug.Log("Collision force: " + collisionForce);
    }
}
