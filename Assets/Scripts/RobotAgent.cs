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

    private RobotMovement robotMovement;
    private Rigidbody rb;
    private RobotAgent enemyRobot;

    [SerializeField] private float tempAngleToEnemy;


    // --------------------------- INITIALIZATION METHODS ---------------------------

    public override void Initialize() {
        cumulativeReward = 0f;
    }


    private void Start() {
        robotMovement = gameObject.GetComponent<RobotMovement>();
        rb = gameObject.GetComponent<Rigidbody>();
        enemyRobot = FindObjectsByType<RobotAgent>(FindObjectsSortMode.None).FirstOrDefault(r => r != this);
    }

    public override void OnEpisodeBegin() {
        transform.position = new Vector3(Random.Range(-3f, 3f), 0.026f, Random.Range(-3f, 3f));
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        SetReward(0f);
        cumulativeReward = GetCumulativeReward();
    }


    // ---------------------------- HANDLE REWARDS ----------------------------

    private void Update() {
        //AddReward(0.001f);

        if (transform.localPosition.y < -1f) {
            SetReward(-10f);
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

        cumulativeReward = GetCumulativeReward();
    }


    private float GetRewards_Movement() {
        //Debug.Log("Velocity: " + rb.linearVelocity.magnitude);
        if (rb.linearVelocity.magnitude < 0.2f) {
            return -1f; // Penalty for not moving
        }
        else if (rb.linearVelocity.magnitude > 0.8f) {
            //if (rb.linearVelocity.magnitude < 0.2f) {
            return 1f; // Reward for high speeds
            //}
        }
        return 0f;
    }

    private float GetRewards_FacingRobot() {
        if (enemyRobot != null) {
            var vectorToEnemy = enemyRobot.transform.position - this.transform.position;
            var angleToEnemy = Vector3.SignedAngle(this.transform.forward, vectorToEnemy, Vector3.up);

            tempAngleToEnemy = angleToEnemy;

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
}
