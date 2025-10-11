using System.Linq;
using UnityEngine;

/// <summary>
/// Handles collision and trigger events by forwarding them to a parent gameObject.
/// Colliders on robots are separate child objects to ensure different damage zones.
/// </summary>
public class CollisionForwarder : MonoBehaviour
{

    private void Start() {
        //Debug.Log("CollisionForwarder initialized for: " + gameObject.name);
    }

    void OnCollisionEnter(Collision collision) {
        foreach (var collider in collision.contacts) {
            var collisionZoneBehaviour = collider.thisCollider.GetComponent<CollisionZoneBehaviour>();
            collisionZoneBehaviour?.OnCollisionHit(collision);
        }

        Debug.Log("Collision happened from: " + collision.contacts.First().thisCollider.name);
        Debug.Log("Collision detected with: " + collision.gameObject.tag);
    }

    //void OnTriggerEnter(Collider other) {
    //    Debug.Log("Trigger detected with: " + other.gameObject.tag);
    //    //parentHandler?.OnChildTriggerEnter(other);
    //}
}
