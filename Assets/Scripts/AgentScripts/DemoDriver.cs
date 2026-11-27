using Unity.MLAgents.Actuators;
using UnityEngine;

/// <summary>
/// Scripted driver for recording demonstrations (imitation learning) without a human at the keyboard.
/// Put it on a robot together with a Demonstration Recorder and set Behavior Type to Heuristic Only:
/// the robot then turns to its enemy, rams it, backs off and rams again, and lifts the arm when close.
/// It is a training tool only; a robot handed in for the tournament must not carry it.
/// </summary>
public class DemoDriver : MonoBehaviour {
    /// <summary>The robot drives straight while the enemy is within this angle of its nose, otherwise it turns on the spot.</summary>
    public const float StraightAngle = 12f;
    public const float ArmRange = 1.2f;
    public const float StallRange = 1f, StallSpeed = 0.3f;
    /// <summary>Seconds of standing against the enemy before backing off, and how long the robot reverses.</summary>
    public const float StallPatience = 0.6f, BackOffSeconds = 1.2f;

    private Rigidbody body;
    private float stalledSince = -1f, backOffUntil = -1f;

    /// <summary>
    /// Track commands to reach an enemy at the given position in the robot's local space.
    /// They are always full (+1 or -1), like keys on a keyboard: a model action changes the motor torque
    /// step by step, so only a held full command has a clear meaning. Small or zero actions would teach
    /// the imitating robot to do nothing.
    /// </summary>
    public static void Steer(Vector3 enemyLocal, out float left, out float right) {
        float angle = Mathf.Atan2(enemyLocal.x, enemyLocal.z) * Mathf.Rad2Deg; // 0 = ahead, + = right, - = left
        if (Mathf.Abs(angle) <= StraightAngle) { left = 1f; right = 1f; }
        else if (angle > 0f) { left = 1f; right = -1f; }
        else { left = -1f; right = 1f; }
    }

    /// <summary>1 = lift the arm, when the enemy is close in front of the robot.</summary>
    public static int ArmAction(Vector3 enemyLocal) {
        enemyLocal.y = 0f;
        bool inFront = enemyLocal.z > 0f && Mathf.Abs(enemyLocal.x) < enemyLocal.z;
        return inFront && enemyLocal.magnitude < ArmRange ? 1 : 0;
    }

    /// <summary>
    /// True when the robot stands pressed against the enemy. Pushing deals no damage, only impacts do,
    /// so the driver then backs off and charges again.
    /// </summary>
    public static bool Stalled(float enemyDistance, float speed) => enemyDistance < StallRange && speed < StallSpeed;

    /// <summary>Called by RobotAgent.Heuristic in place of the keyboard.</summary>
    public void WriteActions(RobotAgent agent, in ActionBuffers actionsOut) {
        var continuous = actionsOut.ContinuousActions;
        var discrete = actionsOut.DiscreteActions;
        if (agent.enemyRobot == null) {
            continuous[0] = 0f; continuous[1] = 0f; discrete[0] = 0;
            return;
        }
        if (body == null) body = GetComponent<Rigidbody>();
        Vector3 enemyLocal = transform.InverseTransformPoint(agent.enemyRobot.transform.position);
        float speed = body != null ? body.linearVelocity.magnitude : 0f;

        if (Stalled(enemyLocal.magnitude, speed)) {
            if (stalledSince < 0f) stalledSince = Time.time;
            if (Time.time - stalledSince > StallPatience) { backOffUntil = Time.time + BackOffSeconds; stalledSince = -1f; }
        } else {
            stalledSince = -1f;
        }

        bool backingOff = Time.time < backOffUntil;
        float left = -1f, right = -1f;
        if (!backingOff) Steer(enemyLocal, out left, out right);
        continuous[0] = left;
        continuous[1] = right;
        discrete[0] = backingOff ? 0 : ArmAction(enemyLocal);
    }
}
