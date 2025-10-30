using System;
using UnityEngine;

public class CollisionZoneBehaviour : MonoBehaviour {

    private float totalHealth = 1f;
    [SerializeField] public float CurrentHealth;

    // Percentual value of how much damage is taken per hit based on collision force
    [SerializeField, Range(1f, 20f)] private float resistanceOfZone = 1f;
    // Multiplier for damage to enemy based on collision force
    [SerializeField, Range(1f, 20f)] public float damageOfZone = 1f;

    public event EventHandler<CollisionEventArgs> OnCollision;
    public class CollisionEventArgs : EventArgs {
        public float Damage { get; private set; }
        public string colliderHitName { get; private set; }

        public CollisionEventArgs(float damage, string colliderHitName) {
            this.Damage = damage;
            this.colliderHitName = colliderHitName;
        }
    }

    private void Start() {
        CurrentHealth = totalHealth;
    }

    private void Update() {
        
    }

    internal void OnCollisionHit(Collision collision, CollisionZoneBehaviour enemyCollisionZone) {
        // normalised collision force
        float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime / 100000f;
        //Debug.Log($"Collision detected with force: {collisionForce}");

        float damageRatio = 0f;
        if (enemyCollisionZone == null) {
            damageRatio = 1f / (resistanceOfZone);
        }
        else {
            damageRatio = (enemyCollisionZone.damageOfZone) / (resistanceOfZone);
        }
            
        //Debug.Log($"Damage ratio calculated as: {damageRatio}, {this.gameObject.name} -> {enemyCollisionZone?.gameObject.name}");
        var damage = damageRatio * Math.Max(collisionForce, 0.0001f);

        CurrentHealth = Math.Max(CurrentHealth - damage, 0f);

        OnCollision?.Invoke(this, new CollisionEventArgs(damage, collision.gameObject.tag));
    }

    public void ResetHealth() {
        CurrentHealth = totalHealth;
    }
}
