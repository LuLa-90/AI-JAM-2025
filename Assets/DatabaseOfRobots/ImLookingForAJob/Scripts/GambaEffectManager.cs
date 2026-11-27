using System.Collections.Generic;
using UnityEngine;

public class GambaEffectsManager : MonoBehaviour
{
    private class RobotEffectState
    {
        public RobotAgent robot;
        public Rigidbody rb;
        public CollisionZoneBehaviour[] zones;

        public float baseDrag;
        public Dictionary<CollisionZoneBehaviour, float> baseDamageByZone =
            new Dictionary<CollisionZoneBehaviour, float>();

        public float speedBuffTimer;
        public float speedBuffForce;

        public float damageBuffTimer;
        public float damageMultiplier = 1f;

        public float slowTimer;
        public float slowExtraDrag;
    }

    private readonly List<RobotEffectState> robotStates = new List<RobotEffectState>();

    private RobotEffectState GetOrCreateState(RobotAgent robot)
    {
        if (robot == null) return null;

        foreach (var s in robotStates)
        {
            if (s.robot == robot) return s;
        }

        var state = new RobotEffectState();
        state.robot = robot;
        state.rb = robot.GetComponent<Rigidbody>();
        state.zones = robot.GetComponentsInChildren<CollisionZoneBehaviour>();

        if (state.rb != null)
        {
            state.baseDrag = state.rb.linearDamping;
        }

        if (state.zones != null)
        {
            foreach (var zone in state.zones)
            {
                if (zone == null) continue;
                state.baseDamageByZone[zone] = zone.damageOfZone;
            }
        }

        robotStates.Add(state);
        return state;
    }

    private void FixedUpdate()
    {
        for (int i = robotStates.Count - 1; i >= 0; i--)
        {
            var s = robotStates[i];

            if (s.robot == null)
            {
                robotStates.RemoveAt(i);
                continue;
            }

            // SPEED BUFF
            if (s.speedBuffTimer > 0f && s.rb != null)
            {
                s.rb.AddForce(s.rb.transform.forward * s.speedBuffForce, ForceMode.Acceleration);
                s.speedBuffTimer -= Time.fixedDeltaTime;
            }

            // SLOW
            if (s.slowTimer > 0f)
            {
                s.slowTimer -= Time.fixedDeltaTime;
                if (s.slowTimer <= 0f && s.rb != null)
                {
                    s.rb.linearDamping = s.baseDrag;
                }
            }

            // DAMAGE BUFF
            if (s.damageBuffTimer > 0f)
            {
                s.damageBuffTimer -= Time.fixedDeltaTime;
                if (s.damageBuffTimer <= 0f)
                {
                    s.damageMultiplier = 1f;
                }
            }

            // aplikuj damageMultiplier na v�etky z�ny
            if (s.zones != null)
            {
                foreach (var zone in s.zones)
                {
                    if (zone == null) continue;

                    float baseDamage;
                    if (!s.baseDamageByZone.TryGetValue(zone, out baseDamage))
                    {
                        baseDamage = zone.damageOfZone;
                        s.baseDamageByZone[zone] = baseDamage;
                    }

                    zone.damageOfZone = baseDamage * s.damageMultiplier;
                }
            }
        }
    }

    public void ApplyHeal(RobotAgent robot, float normalizedDelta)
    {
        var s = GetOrCreateState(robot);
        if (s == null || s.zones == null) return;

        float clamped = Mathf.Clamp(normalizedDelta, -0.2f, 0.2f);

        foreach (var zone in s.zones)
        {
            if (zone == null) continue;
            zone.ChangeHealth(clamped);
        }
    }

    public void ApplySpeedBuff(RobotAgent robot, float duration, float force)
    {
        var s = GetOrCreateState(robot);
        if (s == null) return;

        s.speedBuffTimer = Mathf.Max(s.speedBuffTimer, duration);
        s.speedBuffForce = force;
    }

    public void ApplyDamageBuff(RobotAgent robot, float duration, float multiplier)
    {
        var s = GetOrCreateState(robot);
        if (s == null) return;

        s.damageBuffTimer = Mathf.Max(s.damageBuffTimer, duration);
        s.damageMultiplier = Mathf.Max(1f, multiplier);
    }

    public void ApplyKnockUp(RobotAgent robot, float force)
    {
        var s = GetOrCreateState(robot);
        if (s == null || s.rb == null) return;

        s.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    public void ApplySlow(RobotAgent robot, float duration, float extraDrag)
    {
        var s = GetOrCreateState(robot);
        if (s == null || s.rb == null) return;

        s.slowTimer = duration;
        s.slowExtraDrag = Mathf.Max(0f, extraDrag);
        s.rb.linearDamping = s.baseDrag + s.slowExtraDrag;
    }
}