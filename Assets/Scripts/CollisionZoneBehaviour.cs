using System;
using Unity.VisualScripting;
using UnityEngine;

public class CollisionZoneBehaviour : MonoBehaviour {

    private float totalHealth = 1f;
    [SerializeField] public float CurrentHealth;
    public float RewardOfCollision { get; private set; }

    // Percentual value of how much damage is taken per hit based on collision force
    [SerializeField, Range(1f, 20f)] public float resistanceOfZone = 1f;
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
        RewardOfCollision = 0f;
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

        // TODO: opravit reward calculation
        //RewardOfCollision = Mathf.InverseLerp(0.05f, 10f, damageRatio) * 2f - 1f; 
        
        //Debug.Log($"{this.gameObject.name} -> {enemyCollisionZone?.gameObject.name}");
        if (enemyCollisionZone != null)
        {
            float resistanceRatio = damageOfZone / enemyCollisionZone.resistanceOfZone;
            if (resistanceRatio > 1f)
            {
                RewardOfCollision = resistanceRatio - 1f;
            }
            else
            {
                RewardOfCollision = (1f - resistanceRatio) * -10f;
            }

            Debug.Log(gameObject.name + " (" + enemyCollisionZone.damageOfZone + ";" + resistanceOfZone + ") got reward: " + RewardOfCollision);
        } 
        var damage = damageRatio * Math.Max(collisionForce, 0.0001f);

        CurrentHealth = Math.Max(CurrentHealth - damage, 0f);

        OnCollision?.Invoke(this, new CollisionEventArgs(damage, collision.gameObject.tag));
    }

    public void ResetHealth() {
        CurrentHealth = totalHealth;
    }
}
