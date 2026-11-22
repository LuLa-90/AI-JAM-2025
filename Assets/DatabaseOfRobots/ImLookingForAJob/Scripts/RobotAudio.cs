using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotAudio : MonoBehaviour
{
    [Header("Movement sound")]
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private float minSpeedForSound = 0.1f; 
    [SerializeField] private float maxSpeedForPitch = 3f; 

    [Header("Hammer hit sound")]
    [SerializeField] private AudioSource hammerSource;    
    [SerializeField] private AudioClip hitEnemyClip;
    [SerializeField] private AudioClip hitOtherClip;
    [SerializeField] private string enemyTag = "Robot";

    [Header("Debug (len na testovanie)")]
    [SerializeField] private bool logArmLimitsOnStart = false;

    private Rigidbody rb;
    private ArmComponent armComponent;
    private CollisionZoneBehaviour[] armZones;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementSource != null)
        {
            movementSource.loop = true;
            movementSource.playOnAwake = false;
        }

        armComponent = GetComponentInChildren<ArmComponent>();
        if (armComponent != null)
        {
            armZones = armComponent.GetComponentsInChildren<CollisionZoneBehaviour>();

            foreach (var zone in armZones)
            {
                zone.OnCollision += OnArmZoneCollision;
            }

            if (logArmLimitsOnStart)
            {
                var hinge = armComponent.GetComponent<HingeJoint>();
                if (hinge != null)
                {
                    var limits = hinge.limits;
                    Debug.Log($"[RobotAudio] Arm hinge limits: min = {limits.min}, max = {limits.max}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[RobotAudio] ArmComponent not found on this robot.");
        }
    }

    private void OnDestroy()
    {
        if (armZones != null)
        {
            foreach (var zone in armZones)
            {
                zone.OnCollision -= OnArmZoneCollision;
            }
        }
    }

    private void Update()
    {
        HandleMovementSound();
    }

    private void HandleMovementSound()
    {
        if (movementSource == null || rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;

        if (speed > minSpeedForSound)
        {
            if (!movementSource.isPlaying)
                movementSource.Play();

            float t = Mathf.Clamp01(speed / maxSpeedForPitch);
            movementSource.pitch = Mathf.Lerp(0.8f, 1.3f, t);
            movementSource.volume = Mathf.Lerp(0.2f, 1f, t);
        }
        else
        {
            if (movementSource.isPlaying)
                movementSource.Stop();
        }
    }

    private void OnArmZoneCollision(object sender, CollisionZoneBehaviour.CollisionEventArgs e)
    {
        if (hammerSource == null)
            return;

        AudioClip clipToPlay = null;

        if (e.colliderHitName == enemyTag && hitEnemyClip != null)
        {
            clipToPlay = hitEnemyClip;
        }
        else if (hitOtherClip != null)
        {
            clipToPlay = hitOtherClip;
        }

        if (clipToPlay != null)
        {
            hammerSource.PlayOneShot(clipToPlay);
        }

        // Debug:
        // Debug.Log($"[RobotAudio] Arm hit: tag={e.colliderHitName}, damage={e.Damage}");
    }
}
