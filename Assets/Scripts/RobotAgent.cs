using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

public class RobotAgent : Agent {

    [SerializeField] private float cumulativeReward = 0f;

    [Header("Reward Info (separate)")]
    [SerializeField] private float movementRwd;
    [SerializeField] private float robotFacingRwd;
    [SerializeField] private float armReleaseRwd;
    [SerializeField] private float enemyDistanceRwd;
    [SerializeField] private float arenaPositionRwd;
    [SerializeField] private float collisionCombinationRwd;

    [SerializeField] private SensorComponent[] sensors = new SensorComponent[7]; 

    private Rigidbody rb;
    private RewardEditor rewardEditor;
    private RobotMovement robotMovement;
    private CollisionZoneBehaviour[] zoneComponents;
    [HideInInspector] public RobotAgent enemyRobot;
    [SerializeField] private bool isHeuristic = false;


    //[SerializeField] private float tempAngleToEnemy;


    // --------------------------- INITIALIZATION METHODS ---------------------------

    public override void Initialize() {
        cumulativeReward = 0f;
    }


    private void Start() {
        robotMovement = gameObject.GetComponent<RobotMovement>();
        rb = gameObject.GetComponent<Rigidbody>();
        zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();

        var robots = transform.parent.gameObject.GetComponentsInChildren<RobotAgent>();
        enemyRobot = robots.FirstOrDefault(r => r != this);

        rewardEditor = FindAnyObjectByType<RewardEditor>();

        //Debug.Log("agents: " + enemyRobot.Count() + "with name: " + enemyRobot[0].name);
    }


    public override void OnEpisodeBegin() {
        rb.isKinematic = true;

        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = false;
        transform.localPosition = new Vector3(Random.Range(-2.5f, 2.5f), 0.026f, Random.Range(-2.5f, 2.5f));
        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        //robotMovement.armComponent.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = true;

        foreach (var zone in zoneComponents)
        {
            zone.ResetHealth();
        }

        rb.isKinematic = false;

        movementRwd = 0f;
        robotFacingRwd = 0f;
        armReleaseRwd = 0f;
        enemyDistanceRwd = 0f;
        arenaPositionRwd = 0f;
        collisionCombinationRwd = 0f; 

        SetReward(0f);
        cumulativeReward = GetCumulativeReward();
    }


    // ---------------------------- HANDLE REWARDS ----------------------------

    private void Update() {
        //AddReward(0.001f);

        if (transform.localPosition.y < -1f) {
            Debug.Log("Robot fell out of arena!");
            SetReward(-10f);
            EndEpisode();
        }

        float rotationUp = Vector3.Dot(transform.up, Vector3.up);
        if (rotationUp < 0.2f) {
            Debug.Log("Robot flipped over!");
            EndEpisode();
        }

        //if (GetCumulativeReward() < -50f) {
        //    EndEpisode();
        //}

        // GET REWARDS:

        float movementRewards = GetRewards_Movement() * 0.5f;
        AddReward(movementRewards * rewardEditor.movementRF);
        movementRwd += (float)System.Math.Round(movementRewards, 2);

        float robotFacingRewards = GetRewards_FacingRobot() * 0.5f;
        AddReward(robotFacingRewards * rewardEditor.facingRF);
        robotFacingRwd += (float)System.Math.Round(robotFacingRewards, 2);

        float armReleaseReward = GetRewards_ArmRelease() * 10f;
        AddReward(armReleaseReward * rewardEditor.armAngleRF);
        armReleaseRwd += (float)System.Math.Round(armReleaseReward, 2);

        float enemyDistanceReward = GetRewards_EnemyDistance();
        AddReward(enemyDistanceReward * rewardEditor.enemyDistanceRF);
        enemyDistanceRwd += (float)System.Math.Round(enemyDistanceReward, 2);

        float arenaPositionReward = GetRewards_ArenaPosition();
        AddReward(arenaPositionReward * rewardEditor.arenaPositionRF);
        arenaPositionRwd += (float)System.Math.Round(arenaPositionReward, 2);

        float collisionCombinationReward = GetRewards_collisionCombination() * 100f;
        AddReward(collisionCombinationReward * rewardEditor.collisionCombinationRF);
        collisionCombinationRwd += (float)System.Math.Round(collisionCombinationReward, 2);

        cumulativeReward = GetCumulativeReward();
    }

    private float GetRewards_collisionCombination() {
        var sumReward = zoneComponents.Average(z => z.RewardOfCollision);
        return Mathf.Clamp(sumReward, -1f, 1f); 
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
        float distanceReward = Mathf.Clamp(distanceToEnemy / 6f, 0f, 1f) * -2f + 1f;
        return distanceReward;
    }

    private float GetRewards_ArmRelease() {
        var reward = (0.02f * robotMovement.armComponent.ArmRaisingReward) + robotMovement.armComponent.ArmDropReward;
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
        // Process the actions received from the neural network
        float leftWheelsValue = actions.ContinuousActions[0];
        float rightWheelsValue = actions.ContinuousActions[1];
        int armLiftValue = actions.DiscreteActions[0];

        if (isHeuristic) {
            robotMovement.ApplyBothTrackInputsHeuristic(leftWheelsValue, rightWheelsValue);
            robotMovement.ApplyArmInput(armLiftValue);
        } else {
            robotMovement.ApplyBothTrackInputs(leftWheelsValue, rightWheelsValue);
            robotMovement.ApplyArmInput(armLiftValue);
        }

    }

    public override void CollectObservations(VectorSensor observations) {
        for (int i = 0; i < observations.ObservationSize(); i++) {
            var observationValue = sensors.ElementAtOrDefault(i)?.SensorValue ?? 0f;
            observations.AddObservation(observationValue);
        }
    }

    // ------------------------------- HEURISTIC -------------------------------

    public override void Heuristic(in ActionBuffers actionsOut) {
        isHeuristic = true;

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
