using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionZoneBehaviour : MonoBehaviour {

    private float totalHealth = 1f;
    [SerializeField] public float CurrentHealth = 1f;

    /// <summary>
    /// Reward of the last hit against an enemy zone. It is readable during exactly one physics step (the step
    /// after the hit), so the agent counts every hit once, whatever the frame rate or time scale.
    /// </summary>
    public float RewardOfCollision => CollisionRewardAt(Time.fixedTime);
    private float collisionReward;
    private float collisionRewardTime = float.NegativeInfinity;

    // Percentual value of how much damage is taken per hit based on collision force
    // [SerializeField, Range(1f, 20f)]
    [HideInInspector] public float resistanceOfZone = 1f;
    // Multiplier for damage to enemy based on collision force
    //[SerializeField, Range(1f, 20f)]
    [HideInInspector] public float damageOfZone = 1f;

    private string zoneType = "default";

    public event EventHandler<CollisionEventArgs> OnCollision;
    public class CollisionEventArgs : EventArgs {
        /// <summary>Damage this zone took.</summary>
        public float Damage { get; private set; }
        /// <summary>Tag of what hit this zone: the other object's Rigidbody (Robot, RobotArm) or its collider (arena).</summary>
        public string colliderHitName { get; private set; }
        /// <summary>Damage this zone dealt to the other zone (0 when the other object has no zone).</summary>
        public float DamageDealt { get; private set; }

        public CollisionEventArgs(float damage, string colliderHitName, float damageDealt = 0f) {
            this.Damage = damage;
            this.colliderHitName = colliderHitName;
            this.DamageDealt = damageDealt;
        }
    }

    private void Start() {
        CurrentHealth = totalHealth;
    }

    // ------------------------------------------------------------------ hits

    private static readonly HashSet<(Collider, Collider)> resolvedThisStep = new HashSet<(Collider, Collider)>();
    private static float resolvedStepTime = float.NaN;

    /// <summary>
    /// Applies one hit between two colliders to their zones. A pair is resolved once per physics step:
    /// Unity reports a hit once per contact point and to both robots, which used to multiply the damage.
    /// </summary>
    /// <param name="stepTime">Time.fixedTime of the physics step the hit happened in.</param>
    public static void ResolveHit(Collider a, Collider b, float impulse, float stepTime) {
        if (a == null || b == null) return;
        if (stepTime != resolvedStepTime) {
            resolvedThisStep.Clear();
            resolvedStepTime = stepTime;
        }
        if (!resolvedThisStep.Add((a, b))) return;
        resolvedThisStep.Add((b, a));

        var zoneA = a.GetComponent<CollisionZoneBehaviour>();
        var zoneB = b.GetComponent<CollisionZoneBehaviour>();
        if (zoneA != null) zoneA.ApplyHit(impulse, zoneB, TagOf(b), stepTime);
        if (zoneB != null) zoneB.ApplyHit(impulse, zoneA, TagOf(a), stepTime);
    }

    /// <summary>Tag of the body a collider belongs to: its Rigidbody's object (robot root or arm), else its own object.</summary>
    private static string TagOf(Collider collider) {
        var body = collider.GetComponentInParent<Rigidbody>();
        return body != null ? body.gameObject.tag : collider.gameObject.tag;
    }

    private void ApplyHit(float impulse, CollisionZoneBehaviour enemyCollisionZone, string hitTag, float stepTime) {
        // normalised collision force
        float collisionForce = impulse / Time.fixedDeltaTime / 100000f;
        if (enemyCollisionZone != null) {
            // a touch between two robots always costs a little
            collisionForce = Math.Max(collisionForce, 0.0001f);
        }
        else if (collisionForce <= 0f) {
            // resting or rolling on the arena: no force, no damage (the old minimum slowly drained HP on the floor)
            return;
        }

        float damageTaken = enemyCollisionZone == null
            ? 1f / resistanceOfZone
            : enemyCollisionZone.damageOfZone / resistanceOfZone;
        var damage = damageTaken * collisionForce;

        CurrentHealth = Math.Max(CurrentHealth - damage, 0f);

        float damageDealt = 0f;
        if (enemyCollisionZone != null) {
            // Calculate damage dealt to the enemy zone, normalize it by logarithm with base 20,
            // because our damage values range from 1 to 20
            float damageRatio = damageOfZone / enemyCollisionZone.resistanceOfZone;
            damageDealt = damageRatio * collisionForce;

            collisionReward = Mathf.Clamp(Mathf.Log(damageRatio, 20), -1, 1);
            collisionRewardTime = stepTime;
        }

        OnCollision?.Invoke(this, new CollisionEventArgs(damage, hitTag, damageDealt));
    }

    /// <summary>The collision reward as seen from the physics step at <paramref name="stepTime"/>: non-zero only in the step right after the hit.</summary>
    public float CollisionRewardAt(float stepTime) {
        float age = stepTime - collisionRewardTime;
        return age > 0f && age <= Time.fixedDeltaTime * 1.5f ? collisionReward : 0f;
    }

    public void ResetHealth() {
        CurrentHealth = totalHealth;
    }

    public void ChangeHealth(float value) {
        CurrentHealth = Math.Clamp(CurrentHealth + value, 0f, totalHealth);
    }

    internal void SetZoneType(string type) {
        zoneType = type;
    }

    public string GetZoneType() {
        return zoneType;
    }
}
