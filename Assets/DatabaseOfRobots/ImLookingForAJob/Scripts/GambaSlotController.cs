using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GambaSlotController : MonoBehaviour
{
    [Header("Reels (valce v poradí 1,2,3)")]
    [SerializeField] private Transform[] reels;

    [Header("Spin settings")]
    [SerializeField] private float commonSpinTime = 1.0f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float stopInterval = 0.3f;

    [Header("Geometry")]
    [SerializeField] private int segmentCount = 6;

    [Header("Events per segment")]
    [Tooltip("GOOD eventy (môžu byť 3 pri 6 segmentoch, segmentIndex sa mapuje modulo length).")]
    [SerializeField] private GambaEventDefinition[] goodSegmentEvents;
    [Tooltip("BAD eventy (môžu byť 3 pri 6 segmentoch, segmentIndex sa mapuje modulo length).")]
    [SerializeField] private GambaEventDefinition[] badSegmentEvents;

    [Header("Bombs (optional)")]
    [SerializeField] private GambaBombSpawner bombSpawner;

    [Header("Stop animation (pop efekt)")]
    [SerializeField] private bool playStopPopAnimation = true;
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private float popDuration = 0.15f;

    [Header("Fireworks (particles pri výhre)")]
    [Tooltip("Sem hoď 8 particle objekty (fireworks), ktoré sa majú zapnúť pri GOOD výsledku.")]
    [SerializeField] private ParticleSystem[] fireworks;
    [SerializeField] private float fireworksDuration = 1.5f;

    [Header("Sounds")]
    [Tooltip("AudioSource pre zvuky spinu a výsledku.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Loopnutý zvuk počas spinu.")]
    [SerializeField] private AudioClip spinningClip;
    [Tooltip("Zvuk po vyhodnotení.")]
    [SerializeField] private AudioClip resultClip;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool isSpinning = false;

    public float SpinTime => commonSpinTime + stopInterval * Mathf.Max(0, (reels != null ? reels.Length - 1 : 0));

    private void Start()
    {
        if (fireworks != null)
        {
            foreach (var ps in fireworks)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }
    }

    public void StartSpinForRobot(RobotAgent robot, bool isGood)
    {
        if (isSpinning || robot == null)
        {
            if (debugLogs)
                Debug.Log($"[GAMBA] Spin blocked: Already spinning = {isSpinning}, Robot = {(robot == null ? "null" : robot.name)}");

            return;
        }

        if (debugLogs)
            Debug.Log($"[GAMBA] Starting spin for robot: {robot.name}, GOOD?: {isGood}");

        StartSpinSound();

        StartCoroutine(SpinAndApplyRoutine(robot, isGood));
    }

    private IEnumerator SpinAndApplyRoutine(RobotAgent robot, bool isGood)
    {
        if (reels == null || reels.Length == 0 || segmentCount <= 0)
        {
            if (debugLogs)
                Debug.LogWarning("[GAMBA] Spin aborted: Bad configuration.");
            yield break;
        }

        isSpinning = true;

        int segmentIndex = Random.Range(0, segmentCount);
        float segmentAngle = 360f / segmentCount;
        float targetZ = segmentIndex * segmentAngle;

        if (debugLogs)
            Debug.Log($"[GAMBA] Selected SEGMENT = {segmentIndex} ({targetZ} degrees)");

        float timer = 0f;
        if (debugLogs)
            Debug.Log("[GAMBA] Phase 1: spinning ALL reels together");

        while (timer < commonSpinTime)
        {
            foreach (var reel in reels)
            {
                if (reel != null)
                    reel.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (debugLogs)
            Debug.Log("[GAMBA] Phase 2: stopping reels one by one (instant snap)");

        bool[] stopped = new bool[reels.Length];

        for (int i = 0; i < reels.Length; i++)
        {
            Transform reel = reels[i];
            if (reel == null) continue;

            if (i == 0)
            {
                SnapReelToTarget(reel, targetZ, i);
                stopped[i] = true;
            }
            else
            {
                float intervalTimer = 0f;
                while (intervalTimer < stopInterval)
                {
                    for (int j = 0; j < reels.Length; j++)
                    {
                        if (stopped[j]) continue;

                        Transform spinningReel = reels[j];
                        if (spinningReel != null)
                        {
                            spinningReel.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
                        }
                    }

                    intervalTimer += Time.deltaTime;
                    yield return null;
                }

                SnapReelToTarget(reel, targetZ, i);
                stopped[i] = true;
            }
        }

        GambaEventDefinition ev = null;

        if (robot != null)
        {
            ev = GetEventForSegment(segmentIndex, isGood);

            if (ev != null)
            {
                if (debugLogs)
                    Debug.Log($"[GAMBA] Applying event '{ev.effectType}' to robot '{robot.name}'");

                ApplyEventToRobot(robot, ev);
            }
            else
            {
                Debug.LogWarning($"[GAMBA] No event assigned for segment {segmentIndex} ({(isGood ? "GOOD" : "BAD")})!");
            }
        }
        else
        {
            if (debugLogs)
                Debug.LogError("[GAMBA] Robot is null when trying to apply event!");
        }

        // zvuk výsledku, vždy, podľa isGood
        PlayResultSound(ev != null);

        if (ev != null)
        {
            TriggerFireworks();
        }

        isSpinning = false;

        if (debugLogs)
            Debug.Log("[GAMBA] Spin completed.");
    }

    private void SnapReelToTarget(Transform reel, float targetZ, int index)
    {
        if (reel == null) return;

        Vector3 e = reel.localEulerAngles;
        reel.localEulerAngles = new Vector3(e.x, e.y, targetZ);

        if (debugLogs)
            Debug.Log($"[GAMBA] Reel {index + 1} snapped instantly to Z={targetZ}");

        if (playStopPopAnimation)
        {
            StartCoroutine(PopReelRoutine(reel));
        }
    }

    private IEnumerator PopReelRoutine(Transform reel)
    {
        if (reel == null) yield break;

        Vector3 originalScale = reel.localScale;
        Vector3 targetScale = originalScale * popScaleMultiplier;

        float halfDuration = popDuration * 0.5f;
        float t = 0f;

        while (t < halfDuration)
        {
            float lerpT = t / halfDuration;
            reel.localScale = Vector3.Lerp(originalScale, targetScale, lerpT);

            t += Time.deltaTime;
            yield return null;
        }

        reel.localScale = targetScale;

        t = 0f;
        while (t < halfDuration)
        {
            float lerpT = t / halfDuration;
            reel.localScale = Vector3.Lerp(targetScale, originalScale, lerpT);

            t += Time.deltaTime;
            yield return null;
        }

        reel.localScale = originalScale;
    }

    private GambaEventDefinition GetEventForSegment(int segmentIndex, bool isGood)
    {
        if (segmentIndex < 0 || segmentIndex >= segmentCount)
            return null;

        GambaEventDefinition[] array = isGood ? goodSegmentEvents : badSegmentEvents;

        if (array == null || array.Length == 0)
        {
            if (debugLogs)
                Debug.LogWarning($"[GAMBA] No events configured for {(isGood ? "GOOD" : "BAD")}!");
            return null;
        }

        int eventIndex = segmentIndex % array.Length;

        GambaEventDefinition ev = array[eventIndex];

        if (debugLogs)
        {
            string type = isGood ? "GOOD" : "BAD";
            string effect = ev != null ? ev.effectType.ToString() : "NULL";
            Debug.Log($"[GAMBA] Event selected for {type} segment {segmentIndex} (mappedIndex={eventIndex}): {effect}");
        }

        return ev;
    }

    private void ApplyEventToRobot(RobotAgent robot, GambaEventDefinition ev)
    {
        if (debugLogs)
            Debug.Log($"[GAMBA] Executing effect '{ev.effectType}' on robot '{robot.name}'");

        if (robot == null || ev == null)
        {
            Debug.LogError("[GAMBA] ERROR: Cannot apply event, robot or definition null!");
            return;
        }

        Rigidbody rb = robot.GetComponent<Rigidbody>();
        CollisionZoneBehaviour[] zones = robot.GetComponentsInChildren<CollisionZoneBehaviour>();

        switch (ev.effectType)
        {
            case GambaEventDefinition.EffectType.HealthChange:
                if (debugLogs) Debug.Log("[GAMBA] → HEAL / DAMAGE applied");
                ApplyHealOnce(zones, ev.healthDelta);
                break;

            case GambaEventDefinition.EffectType.SpeedBuff:
                if (debugLogs) Debug.Log("[GAMBA] → SPEED BUFF applied");
                if (rb != null)
                    StartCoroutine(SpeedBuffCoroutine(rb, ev.duration, ev.speedBuffForce));
                break;

            case GambaEventDefinition.EffectType.DamageBuff:
                if (debugLogs) Debug.Log("[GAMBA] → DAMAGE BUFF applied");
                if (zones != null && zones.Length > 0)
                    StartCoroutine(DamageBuffCoroutine(zones, ev.duration, ev.damageMultiplier));
                break;

            case GambaEventDefinition.EffectType.KnockUp:
                if (debugLogs) Debug.Log("[GAMBA] → KNOCKUP");
                if (rb != null)
                    rb.AddForce(Vector3.up * ev.knockUpForce, ForceMode.Impulse);
                break;

            case GambaEventDefinition.EffectType.Slow:
                if (debugLogs) Debug.Log("[GAMBA] → SLOW applied");
                if (rb != null)
                    StartCoroutine(SlowCoroutine(rb, ev.duration, ev.slowExtraDrag));
                break;

            case GambaEventDefinition.EffectType.SpawnBombs:
                if (debugLogs) Debug.Log("[GAMBA] → SPAWNING BOMBS");
                if (bombSpawner != null)
                    bombSpawner.SpawnBombWave(ev);
                break;
        }
    }

    private void ApplyHealOnce(CollisionZoneBehaviour[] zones, float delta)
    {
        if (zones == null) return;

        float clamped = Mathf.Clamp(delta, -0.2f, 0.2f);

        foreach (var zone in zones)
        {
            if (zone == null) continue;
            zone.ChangeHealth(clamped);
        }
    }

    private IEnumerator SpeedBuffCoroutine(Rigidbody rb, float duration, float force)
    {
        if (rb == null) yield break;

        float timer = 0f;
        while (timer < duration && rb != null)
        {
            rb.AddForce(rb.transform.forward * force, ForceMode.Acceleration);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator DamageBuffCoroutine(CollisionZoneBehaviour[] zones, float duration, float multiplier)
    {
        if (zones == null || zones.Length == 0) yield break;

        multiplier = Mathf.Max(1f, multiplier);

        var baseDamage = new Dictionary<CollisionZoneBehaviour, float>();
        foreach (var zone in zones)
        {
            if (zone == null) continue;
            baseDamage[zone] = zone.damageOfZone;
            zone.damageOfZone = baseDamage[zone] * multiplier;
        }

        yield return new WaitForSeconds(duration);

        foreach (var kvp in baseDamage)
        {
            if (kvp.Key != null)
            {
                kvp.Key.damageOfZone = kvp.Value;
            }
        }
    }

    private IEnumerator SlowCoroutine(Rigidbody rb, float duration, float extraDrag)
    {
        if (rb == null) yield break;

        float baseDrag = rb.linearDamping;
        rb.linearDamping = baseDrag + Mathf.Max(0f, extraDrag);

        yield return new WaitForSeconds(duration);

        if (rb != null)
        {
            rb.linearDamping = baseDrag;
        }
    }

    private void TriggerFireworks()
    {
        if (fireworks == null || fireworks.Length == 0)
            return;

        StartCoroutine(FireworksRoutine());
    }

    private IEnumerator FireworksRoutine()
    {
        foreach (var ps in fireworks)
        {
            if (ps == null) continue;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        yield return new WaitForSeconds(fireworksDuration);

        foreach (var ps in fireworks)
        {
            if (ps == null) continue;
            ps.Stop();
            ps.gameObject.SetActive(false);
        }
    }

    // zvuky

    private void StartSpinSound()
    {
        if (audioSource == null || spinningClip == null)
            return;

        audioSource.loop = true;
        audioSource.clip = spinningClip;
        audioSource.Play();
    }

    private void PlayResultSound(bool hasEvent)
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.loop = false;

        AudioClip clipToPlay = null;

        if (resultClip != null)
        {
            clipToPlay = resultClip;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}
