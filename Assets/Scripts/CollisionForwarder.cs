using System.Linq;
using UnityEngine;

/// <summary>
/// Handles collision and trigger events by forwarding them to a parent gameObject.
/// Colliders on robots are separate child objects to ensure different damage zones.
/// </summary>
public class CollisionForwarder : MonoBehaviour
{
    void OnCollisionEnter(Collision collision) {
        foreach (var collider in collision.contacts) {
            var thisCollisionZone = collider.thisCollider.GetComponent<CollisionZoneBehaviour>();
            var otherCollisionZone = collider.otherCollider.GetComponent<CollisionZoneBehaviour>();

            thisCollisionZone?.OnCollisionHit(collision, otherCollisionZone);
            otherCollisionZone?.OnCollisionHit(collision, thisCollisionZone);
        }
    }
}
