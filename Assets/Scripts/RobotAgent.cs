using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

public class RobotAgent : Agent {
    [SerializeField] private float cumulativeReward = 0f;

    [SerializeField] private SensorComponent[] sensors = new SensorComponent[7];

    [Header("Reward Factors")]
    [SerializeField, Range(0f, 1f)] private float movementRF = 0.8f;
    [SerializeField, Range(0f, 1f)] private float facingRF = 0.5f;
    [SerializeField, Range(0f, 1f)] private float armAngleRF = 0.5f;
    [SerializeField, Range(0f, 1f)] private float enemyDistanceRF = 0.2f;
    [SerializeField, Range(0f, 1f)] private float arenaPositionRF = 0.2f;

    private RobotMovement robotMovement;
    private Rigidbody rb;
    [HideInInspector] public RobotAgent enemyRobot;

    //[SerializeField] private float tempAngleToEnemy;


    // --------------------------- INITIALIZATION METHODS ---------------------------

    public override void Initialize() {
        cumulativeReward = 0f;
    }


    private void Start() {
        robotMovement = gameObject.GetComponent<RobotMovement>();
        rb = gameObject.GetComponent<Rigidbody>();

        var robots = transform.parent.gameObject.GetComponentsInChildren<RobotAgent>();
        enemyRobot = robots.FirstOrDefault(r => r != this);

        //Debug.Log("agents: " + enemyRobot.Count() + "with name: " + enemyRobot[0].name);
    }


    public override void OnEpisodeBegin() {
        rb.isKinematic = true;

        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = false;
        transform.localPosition = new Vector3(Random.Range(-2.5f, 2.5f), 0.026f, Random.Range(-2.5f, 2.5f));
        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        //robotMovement.armComponent.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        //robotMovement.armComponent.GetComponent<HingeJoint>().enableCollision = true;

        rb.isKinematic = false;

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

        float movementRewards = GetRewards_Movement();
        AddReward(movementRewards * movementRF);

        float robotFacingRewards = GetRewards_FacingRobot();
        AddReward(robotFacingRewards * facingRF);

        float armReleaseReward = GetRewards_ArmRelease();
        AddReward(armReleaseReward * armAngleRF * 10f);

        float enemyDistanceReward = GetRewards_EnemyDistance();
        AddReward(enemyDistanceReward * enemyDistanceRF);

        float arenaPositionReward = GetRewards_ArenaPosition();
        AddReward(arenaPositionReward * arenaPositionRF);

        cumulativeReward = GetCumulativeReward();
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
        float distanceReward = Mathf.Clamp(distanceToEnemy / 4.5f, 0f, 1f) * -2f + 1f;
        return distanceReward;
    }

    private float GetRewards_ArmRelease() {
        return robotMovement.armComponent.ArmDropReward;
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

        robotMovement.ApplyBothTrackInputs(leftWheelsValue, rightWheelsValue);
        robotMovement.ApplyArmInput(armLiftValue);
    }

    public override void CollectObservations(VectorSensor observations) {
        for (int i = 0; i < observations.ObservationSize(); i++) {
            var observationValue = sensors.ElementAtOrDefault(i)?.SensorValue ?? 0f;
            observations.AddObservation(observationValue);
        }
    }

    // ------------------------------- HEURISTIC -------------------------------
    public override void Heuristic(in ActionBuffers actionsOut) {
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
