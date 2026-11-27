using UnityEngine;

[CreateAssetMenu(menuName = "AIJam/Gamba Event")]
public class GambaEventDefinition : ScriptableObject
{
    public enum EventKind
    {
        Good,
        Bad
    }

    public enum EffectType
    {
        HealthChange,   
        SpeedBuff,     
        DamageBuff,     
        KnockUp,        
        Slow,           
        SpawnBombs      
    }

    [Header("Basic info")]
    public string eventName;
    public EventKind kind;

    [Header("Effect type")]
    public EffectType effectType;

    [Header("Health change (normalized)")]
    [Range(-0.2f, 0.2f)]
    public float healthDelta = 0.2f; 

    [Header("Duration for timed effects")]
    public float duration = 3f;

    [Header("Speed buff settings")]
    public float speedBuffForce = 30f;

    [Header("Damage buff settings")]
    public float damageMultiplier = 2f;

    [Header("KnockUp settings")]
    public float knockUpForce = 8f;

    [Header("Slow settings")]
    public float slowExtraDrag = 2f;

    [Header("Bomb wave settings")]
    public int bombCount = 5;
    public float bombDamage = 0.1f;
    public float bombExplosionForce = 10f;
    public float bombRadius = 2f;
    public float bombDelay = 1.5f;
}
