using System.Linq;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static CollisionZoneBehaviour;

public class RobotAgent : Agent {

    [SerializeField] private float cumulativeReward = 0f;

    [Header("Reward Info (separate)")]
    [SerializeField] private float movementRwd;
    [SerializeField] private float robotFacingRwd;
    [SerializeField] private float armReleaseRwd;
    [SerializeField] private float enemyDistanceRwd;
    [SerializeField] private float arenaPositionRwd;
    [SerializeField] private float collisionCombinationRwd;
    //Upgrade
    [SerializeField] private float collisionHitRwd;

    [SerializeField] private SensorComponent[] sensors = new SensorComponent[7];

    private Rigidbody rb;
    [SerializeField] private RewardEditor rewardEditor;
    private BehaviorParameters behavior;
    private RobotMovement robotMovement;
    private CollisionZoneBehaviour[] zoneComponents;
    private IHealth[] healthComponents;
    [HideInInspector] public RobotAgent enemyRobot;
    private bool isHeuristic = false;
    [HideInInspector] public float weightedAverageHealth = 1f;

    public event System.EventHandler OnRobotRespawn;
    public event System.EventHandler OnRobotDie;
    private bool isStarted = false;

    //[SerializeField] private float tempAngleToEnemy;


    // --------------------------- INITIALIZATION METHODS ---------------------------

    public override void Initialize() {
        cumulativeReward = 0f;
        MaxStep = 0; //TODO: 5000; - disable for now
    }

    private void Start() {
        robotMovement = gameObject.GetComponent<RobotMovement>();
        zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();
        healthComponents = GetComponentsInChildren<IHealth>();
        //Debug.Log($"RobotAgent found {zoneComponents.Length} collision zones and {healthComponents.Length} health components.");

        rb = gameObject.GetComponent<Rigidbody>();

        // The enemy is the other robot under the same parent (a robot without a parent has none).
        if (transform.parent != null) {
            var robots = transform.parent.gameObject.GetComponentsInChildren<RobotAgent>();
            enemyRobot = robots.FirstOrDefault(r => r != this);
        }

        rewardEditor = FindAnyObjectByType<RewardEditor>();
        behavior = GetComponent<BehaviorParameters>();

        isStarted = true;



        foreach (var zone in zoneComponents)            // pre kazdy kolider ...
        {   
            zone.OnCollision += HandleZoneCollision;    // prihlad funkciu na event aby sme v okamziku dali velku odmenu za zasah nepriatela
        }
   
    }

    /// <summary>
    /// Reward for one zone hit: the damage this robot DEALT to an enemy robot or its arm, scaled.
    /// Damage the robot took and hits against the arena give nothing.
    /// </summary>
    public static float HitReward(CollisionZoneBehaviour.CollisionEventArgs e) {
        bool hitEnemy = e.colliderHitName == "Robot" || e.colliderHitName == "RobotArm";
        return hitEnemy ? e.DamageDealt * HitRewardPerDamage : 0f;
    }

    // ---------------------------------------------------------------- reward balance
    // Winning must be worth more than everything else in an episode:
    //   win / loss            +1 / -1
    //   time-out              +0.5 / -0.5 by HP (same rule as the tournament), 0 on equal HP
    //   hits                  0.1 per unit of damage dealt (a long fight deals 2 to 4 units)
    //   shaping (Reward Editor) at most about 0.09 per term over a 90 s episode
    public const float WinReward = 1f;
    public const float LoseReward = -1f;
    public const float TimeoutWinReward = 0.5f;
    public const float HitRewardPerDamage = 0.1f;
    /// <summary>Brings the per-step shaping rewards (about 1 per second at full strength) far below a win.</summary>
    public const float ShapingScale = 0.001f;

    /// <summary>One step of a shaping reward: value (-1..1) x gain x step time x Reward Editor factor, not rounded.</summary>
    public static float ShapingReward(float value, float gain, float deltaTime, float factor) =>
        value * gain * deltaTime * factor * ShapingScale;

    /// <summary>Collision-combination reward for one step, scaled by its Reward Editor factor.</summary>
    public static float CollisionStepReward(float combinationReward, float deltaTime, float factor) =>
        ShapingReward(combinationReward, 100f, deltaTime, factor);

    /// <summary>Reward when the episode time runs out: the robot with more HP wins half a point.</summary>
    public static float TimeoutReward(float myHealth, float enemyHealth) {
        if (Mathf.Abs(myHealth - enemyHealth) < 0.01f) return 0f;
        return myHealth > enemyHealth ? TimeoutWinReward : -TimeoutWinReward;
    }

    /// <summary>The episode time limit applies only while a trainer is connected; matches run their own clock.</summary>
    public static bool IsTimedOut(float episodeSeconds, float limitSeconds, bool training) =>
        training && limitSeconds > 0f && episodeSeconds >= limitSeconds;

    public void HandleZoneCollision(object sender, CollisionZoneBehaviour.CollisionEventArgs e)
    {
        // No reward for walls, non-enemy objects or hits that dealt nothing.
        // (No log line per hit: logging every collision slows training down a lot.)
        float zoneReward = HitReward(e);
        if (zoneReward != 0f) AddReward(zoneReward);

        if (e.colliderHitName == "Robot" || e.colliderHitName == "RobotArm") {
            fightStats.AddHit(e.DamageDealt, e.Damage);
        }
    }

    // ---------------------------------------------------------------- fight statistics (TensorBoard, group "Fight")
    private readonly FightStats fightStats = new FightStats();

    /// <summary>
    /// Ends this robot's episode with its final reward. While a trainer is connected, it also writes what the
    /// robot did in the episode to TensorBoard, so progress can be judged by more than the mean reward.
    /// </summary>
    private void FinishEpisode(float reward, bool knockout) {
        if (Academy.Instance.IsCommunicatorOn) {
            var stats = Academy.Instance.StatsRecorder;
            stats.Add("Fight/Damage Dealt", fightStats.DamageDealt);
            stats.Add("Fight/Damage Taken", fightStats.DamageTaken);
            stats.Add("Fight/Falls and Flips", fightStats.Respawns);
            stats.Add("Fight/Ended By Knockout", knockout ? 1f : 0f);
            stats.Add("Fight/End HP", weightedAverageHealth);
        }
        AddReward(reward);
        EndEpisode();
    }



    [Tooltip("Training only: an episode ends after this many seconds (0 = no limit). The robot with more HP gets the time-out reward.")]
    [SerializeField] private float maxEpisodeSeconds = 90f;
    private float episodeStartTime;

    public override void OnEpisodeBegin() {
        movementRwd = 0f;
        robotFacingRwd = 0f;
        armReleaseRwd = 0f;
        enemyDistanceRwd = 0f;
        arenaPositionRwd = 0f;
        collisionCombinationRwd = 0f;

        SetReward(0f);
        cumulativeReward = GetCumulativeReward();
        episodeStartTime = Time.time;
        fightStats.Reset();

        OnRobotRespawn?.Invoke(this, System.EventArgs.Empty);

        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = false;
        //robotMovement.armComponent.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = true;

        if (!isStarted) {
            return;
        }

        foreach (var zone in zoneComponents) {
            zone.ResetHealth();
        }
    }

    //public bool IsTraining() {
    //    // BehaviorParameters controls whether we're using trainer or ONNX
    //    var behavior = GetComponent<BehaviorParameters>();

    //    // This is true ONLY when Python is connected (training)
    //    return behavior;
    //}


    // ---------------------------- HANDLE REWARDS ----------------------------

    private void Update() {
        //AddReward(0.001f);

        if (transform.localPosition.y < -1f) {
            Debug.Log("Robot fell out of arena!");

            foreach (var zone in zoneComponents) {
                zone.ChangeHealth(-0.3f);
            }
            fightStats.AddRespawn();

            OnRobotRespawn?.Invoke(this, System.EventArgs.Empty);
        }

        float rotationUp = Vector3.Dot(transform.up, Vector3.up);
        if (rotationUp < 0.75f) { // origin 0.2f
            Debug.Log("Robot flipped over!");

            foreach (var zone in zoneComponents) {
                zone.ChangeHealth(-0.2f);
            }
            fightStats.AddRespawn();

            OnRobotRespawn?.Invoke(this, System.EventArgs.Empty);
        }

        // Check if robot has died
        if (CheckIfDied()) {
            // Raise event for robot's death
            OnRobotDie?.Invoke(this, System.EventArgs.Empty);

            // End the episode with negative reward (lose)
            FinishEpisode(LoseReward, true);

            // Give positive reward to enemy robot (win)
            if (enemyRobot != null) enemyRobot.FinishEpisode(WinReward, true);
        }
        // Training only: end the episode when its time runs out. The robot with more HP wins half a point,
        // like a round that ends on time in the tournament.
        else if (IsTimedOut(Time.time - episodeStartTime, maxEpisodeSeconds, Academy.Instance.IsCommunicatorOn)) {
            float myHealth = weightedAverageHealth;
            float enemyHealth = enemyRobot != null ? enemyRobot.weightedAverageHealth : myHealth;

            FinishEpisode(TimeoutReward(myHealth, enemyHealth), false);
            if (enemyRobot != null) enemyRobot.FinishEpisode(TimeoutReward(enemyHealth, myHealth), false);
        }

        /// origin position ***AAA***
        

    }

    /// <summary>
    /// Checks if a robot has died based on the weighted average health of its components.
    /// </summary>
    /// <returns>Returns true if the robot is considered dead, otherwise false.</returns>
    private bool CheckIfDied() {

        if (healthComponents.Count() == 0) {
            return false;
        }


        // Probably not needed, because of the new weighted average method of average health

        //foreach (var healthComp in healthComponents) {
        //    switch (healthComp) {
        //        case BodyComponent bodyComp:
        //            //Debug.Log($"Body health: {healthComp.CurrentHealth}");

        //            if (healthComp.CurrentHealth <= 0.1f) {
        //                Debug.Log("Robot body destroyed!");
        //                // TODO: Method for robot dying, losing/winning logic
        //                return true;
        //            }
        //            break;
        //        case WheelComponent wheelComp:
        //            //Debug.Log($"Wheel health: {healthComp.CurrentHealth}");
        //            break;
        //        case BumperComponent bumperComp:
        //            //Debug.Log($"Bumper health: {healthComp.CurrentHealth}");
        //            break;
        //        case ArmComponent armComp:
        //            //Debug.Log($"Arm health: {healthComp.CurrentHealth}");
        //            break;
        //    }
        //}

        var sumHealth = healthComponents.Sum(e => e.CurrentHealth * e.influenceOnTotalHealth);
        var weightsHealth = healthComponents.Sum(e => e.influenceOnTotalHealth);
        weightedAverageHealth = sumHealth / weightsHealth;

        if (weightedAverageHealth <= 0.2f) {
            Debug.Log("Robot beyond repair!");
            return true;
        }

        return false;
    }

    float lastDamageOrHitTime;
    private float GetRewards_collisionCombination() {
        // original
        //var sumReward = zoneComponents.Average(z => z.RewardOfCollision);
        //return Mathf.Clamp(sumReward, -1f, 1f);

        // NEW
        // average of zone rewards for this frame
        // (a robot without damage zones has nothing to average)
        float avgReward = zoneComponents != null && zoneComponents.Length > 0
            ? zoneComponents.Average(z => z.RewardOfCollision)
            : 0f;

        // If we actually got some reward this frame (positive or negative),
        // consider that as "impact happened"
        if (Mathf.Abs(avgReward) > 0.001f)
        {
            lastDamageOrHitTime = Time.time;
        }

        // Start with the normal collision reward
        float reward = avgReward;

        // Now add "hugging" penalty if we are just stuck together
        if (enemyRobot != null)
        {
            float distanceToEnemy = (enemyRobot.transform.position - transform.position).magnitude;

            bool veryClose = distanceToEnemy < 1.0f;           // tune threshold
            bool noRecentHit = (Time.time - lastDamageOrHitTime) > 0.3f; // no impact for 0.3s

            // optional: check relative speed to detect "just sliding"
            var myVel = rb != null ? rb.linearVelocity : Vector3.zero;
            var enemyRb = enemyRobot.GetComponent<Rigidbody>();
            var enemyVel = enemyRb != null ? enemyRb.linearVelocity : Vector3.zero;
            float relativeSpeed = (myVel - enemyVel).magnitude;
            bool slowRelativeMotion = relativeSpeed < 0.5f;    // tuning value

            if (veryClose && noRecentHit && slowRelativeMotion)
            {
                // Small continuous penalty while hugging
                reward -= 0.02f;   // tune this; not too big per step
            }
        }

        //return Mathf.Clamp(reward, -10f, 1f);
        return Mathf.Clamp(reward, -10f, 1f);
    }

    private float GetRewards_ArenaPosition() {
        float distanceToCenter = this.transform.localPosition.magnitude;
        float distanceReward = Mathf.Clamp(distanceToCenter / 2f, 0f, 1f) * -2f + 1f;
        return distanceReward;
    }

    private float GetRewards_EnemyDistance() {
        if (enemyRobot == null) return 0f;
        float distanceToEnemy = (enemyRobot.transform.localPosition - this.transform.localPosition).magnitude;
        //Debug.Log("Distance to enemy: " + distanceToEnemy);
        float distanceReward = Mathf.Clamp(distanceToEnemy / 4f, 0f, 1f) * -2f + 1f;
        return distanceReward;
    }

    private float GetRewards_ArmRelease() {
        var arm = robotMovement != null ? robotMovement.armComponent : null;
        if (arm == null) return 0f; // robot without an arm

        var reward = (0.02f * arm.ArmRaisingReward) + arm.ArmDropReward;
        return Mathf.Clamp(reward, 0f, 1f);
    }

    private float GetRewards_Movement() {
        //Debug.Log("Velocity: " + rb.linearVelocity.magnitude);

        float forwardDot = Vector3.Dot(rb.linearVelocity.normalized, transform.forward);
        if (forwardDot > 0.2f) // Threshold to ensure it's *mostly* forward
        {
            return Mathf.Clamp(rb.linearVelocity.magnitude / 1.7f, 0f, 1f) * 2f - 1f;
            // Small bonus for moving forward
        }
        else if (forwardDot < -0.2f) {
            return Mathf.Clamp(rb.linearVelocity.magnitude / 1.7f, 0f, 1f) * 1.5f - 1f;
            // Penalty for moving backward
        }
        return 0f;
    }

    private float GetRewards_FacingRobot() {
        if (enemyRobot != null) {
            var vectorToEnemy = enemyRobot.transform.position - this.transform.position;
            var angleToEnemy = Vector3.SignedAngle(this.transform.forward, vectorToEnemy, Vector3.up);

            //tempAngleToEnemy = angleToEnemy;

            // relativna podla toho ako moc? alebo len ano/nie situacia
            if (angleToEnemy >= -45f && angleToEnemy <= 45f) {
                return 1f;
            }
            else if (angleToEnemy >= 100f || angleToEnemy <= -100f) {
                return -1f;
            }
        }
        return 0f;
    }


    // ---------------------------- ACTIONS AND OBSERVATIONS ---------------------------

    public override void OnActionReceived(ActionBuffers actions) {
        if (!isStarted) {
            return;
        }

        // Process the actions received from the neural network
        float leftWheelsValue = actions.ContinuousActions[0];
        float rightWheelsValue = actions.ContinuousActions[1];
        int armLiftValue = actions.DiscreteActions[0];

        if (isHeuristic) {
            robotMovement.ApplyBothTrackInputsHeuristic(leftWheelsValue, rightWheelsValue);
            robotMovement.ApplyArmInput(armLiftValue);
        }
        else {
            robotMovement.ApplyBothTrackInputs(leftWheelsValue, rightWheelsValue);
            robotMovement.ApplyArmInput(armLiftValue);
        }

        if (rewardEditor != null)
        {

            // Shaping rewards: value x gain x step time x Reward Editor factor (see ShapingReward).
            // They are no longer rounded to 2 decimals, which used to turn the small ones into 0.
            float dt = Time.deltaTime;

            movementRwd = ShapingReward(GetRewards_Movement(), 0.5f, dt, rewardEditor.movementRF);
            AddReward(movementRwd);

            robotFacingRwd = ShapingReward(GetRewards_FacingRobot(), 0.5f, dt, rewardEditor.facingRF);
            AddReward(robotFacingRwd);

            armReleaseRwd = ShapingReward(GetRewards_ArmRelease(), 10f, dt, rewardEditor.armAngleRF);
            AddReward(armReleaseRwd);

            enemyDistanceRwd = ShapingReward(GetRewards_EnemyDistance(), 1f, dt, rewardEditor.enemyDistanceRF);
            AddReward(enemyDistanceRwd);

            arenaPositionRwd = ShapingReward(GetRewards_ArenaPosition(), 1f, dt, rewardEditor.arenaPositionRF);
            AddReward(arenaPositionRwd);

            // This step's reward only (it used to add the running total again on every step).
            collisionCombinationRwd = CollisionStepReward(GetRewards_collisionCombination(), Time.deltaTime, rewardEditor.collisionCombinationRF);
            AddReward(collisionCombinationRwd);

            // TODO: If Butt Sensor not null, then add reward for butt marker visibility (maybe could surpress unwanted front collisions?)

            cumulativeReward = GetCumulativeReward();

        }
    }

    [SerializeField] private float maxEnemyDistance = 4f;   
    [SerializeField] private float maxArenaRadius = 4f;
    public override void CollectObservations(VectorSensor observations) {
        // 1) PASSIVE SENSORS (always present, not user-configurable)

        // a) Distance to enemy
        float enemyDistanceNorm = 0f;
        Vector3 relEnemyLocal = Vector3.zero;

        if (enemyRobot != null)
        {
            Vector3 toEnemy = enemyRobot.transform.position - transform.position;
            float distance = toEnemy.magnitude;
            enemyDistanceNorm = Mathf.Clamp01(distance / maxEnemyDistance);

            // enemy position in *your local space* (x,z)
            relEnemyLocal = transform.InverseTransformPoint(enemyRobot.transform.position);
        }

        // Normalize local enemy position by arena size (roughly)
        float relEnemyX = Mathf.Clamp(relEnemyLocal.x / maxArenaRadius, -1f, 1f);
        float relEnemyZ = Mathf.Clamp(relEnemyLocal.z / maxArenaRadius, -1f, 1f);

        // Add passive observations
        observations.AddObservation(enemyDistanceNorm); // 1
        observations.AddObservation(relEnemyX);         // 2
        observations.AddObservation(relEnemyZ);         // 3

        // 2) USER-CHOSEN SENSORS (up to 7 slots)
        for (int i = 0; i < observations.ObservationSize()-3; i++) {
            var observationValue = sensors?.ElementAtOrDefault(i)?.SensorValue ?? 0f;
            observations.AddObservation(observationValue);
        }
    }

    // ------------------------------- HEURISTIC -------------------------------

    public override void Heuristic(in ActionBuffers actionsOut) {
        isHeuristic = true;
        MaxStep = 0; // Disable max step limit for heuristic mode

        if (!isStarted) {
            return;
        }

        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        float leftWheelsValue = robotMovement.leftWheelsAxis.ReadValue<float>();
        float rightWheelsValue = robotMovement.rightWheelsAxis.ReadValue<float>();
        float armLiftValue = robotMovement.armLiftAction.ReadValue<float>();

        continuousActionsOut[0] = leftWheelsValue;
        continuousActionsOut[1] = rightWheelsValue;
        discreteActionsOut[0] = (int)armLiftValue;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;

        if (enemyRobot != null) {
            Gizmos.DrawLine(transform.position, enemyRobot.transform.position);
        }
    }
}