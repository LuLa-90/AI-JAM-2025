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
            var stats = StatsFor(zone.gameObject.name);
            zone.damageOfZone = stats.damage;
            zone.resistanceOfZone = stats.resistance;
        }
    }

    /// <summary>
    /// Damage and resistance of a zone, chosen by the first keyword its object name contains.
    /// Ramming with the bumper is the main source of damage: it deals five times an arm strike.
    /// The values are set so that well-aimed rams can knock a robot out within a 90 s round.
    /// </summary>
    public static (float damage, float resistance) StatsFor(string zoneName) {
        if (zoneName.Contains("body")) return (1f, 3f);
        if (zoneName.Contains("battery")) return (1f, 1f);
        if (zoneName.Contains("wheels")) return (5f, 4f);
        if (zoneName.Contains("armbase")) return (2f, 3f);
        if (zoneName.Contains("weapon")) return (300f, 15f);
        if (zoneName.Contains("bumper")) return (1500f, 10f);
        return (1f, 5f);
    }

}