using UnityEngine;

public class RewardEditor : MonoBehaviour
{
    // Shaping: small rewards on every step for how the robot moves and where it is.
    [Header("Reward Factors")]
    [SerializeField, Range(0f, 1f)] public float movementRF = 0.8f;
    [SerializeField, Range(0f, 1f)] public float facingRF = 0.5f;
    [SerializeField, Range(0f, 1f)] public float armAngleRF = 0.5f;
    [SerializeField, Range(0f, 1f)] public float enemyDistanceRF = 0.2f;
    [SerializeField, Range(0f, 1f)] public float arenaPositionRF = 0.2f;
    [SerializeField, Range(0f, 1f)] public float collisionCombinationRF = 0.7f;

    // Fight: what the robot is paid for in the fight itself. These decide its style (attacker or survivor).
    [Header("Fight Factors")]
    [Tooltip("Reward for damage dealt to the enemy. 1 = 0.2 per unit of damage, 0 = hits pay nothing.")]
    [SerializeField, Range(0f, 1f)] public float hitRF = 0.5f;
    [Tooltip("Penalty for damage taken from the enemy. 1 = -0.2 per unit of damage, 0 = no penalty.")]
    [SerializeField, Range(0f, 1f)] public float damageTakenRF = 0f;
    [Tooltip("Value of winning on time (more HP after the time limit) compared with a knockout. 1 = same as a knockout, 0 = only a knockout counts. Losing on time costs the same amount.")]
    [SerializeField, Range(0f, 1f)] public float timeoutRF = 0.5f;
}
