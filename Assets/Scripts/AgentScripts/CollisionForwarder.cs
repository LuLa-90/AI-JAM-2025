using System.Linq;
using UnityEngine;

/// <summary>
/// Handles collision and trigger events by forwarding them to a parent gameObject.
/// Colliders on robots are separate child objects to ensure different damage zones.
/// </summary>
public class CollisionForwarder : MonoBehaviour
{

    private void Start() {
        AssignDamageAndResistance();
    }

    void OnCollisionEnter(Collision collision) {
        // A collision has one contact per contact point and is reported to both robots.
        // ResolveHit applies each collider pair once per physics step.
        float impulse = collision.impulse.magnitude;
        foreach (var contact in collision.contacts) {
            CollisionZoneBehaviour.ResolveHit(contact.thisCollider, contact.otherCollider, impulse, Time.fixedTime);
        }
    }

    private void AssignDamageAndResistance() {
        var zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();

        foreach (var zone in zoneComponents) {
            if (zone.gameObject.name.Contains("body")) { 
                zone.damageOfZone = 1f;
                zone.resistanceOfZone = 3f;
            }
            else if (zone.gameObject.name.Contains("battery")) { 
                zone.damageOfZone = 1f;
                zone.resistanceOfZone = 1f;
            }
            else if (zone.gameObject.name.Contains("wheels")) {
                zone.damageOfZone = 5f;
                zone.resistanceOfZone = 4f;
            }
            else if (zone.gameObject.name.Contains("armbase")) {
                zone.damageOfZone = 2f;
                zone.resistanceOfZone = 3f;
            }
            else if (zone.gameObject.name.Contains("weapon")) {
                zone.damageOfZone = 200f;
                zone.resistanceOfZone = 15f;
            }
            else if (zone.gameObject.name.Contains("bumper")) {
                zone.damageOfZone = 150f;
                zone.resistanceOfZone = 10f;
            }
            else {
                zone.damageOfZone = 1f;
                zone.resistanceOfZone = 5f;
            }
        }
    }

}