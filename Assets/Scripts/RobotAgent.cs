using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RobotAgent : Agent {
    [SerializeField] private SensorComponent[] sensors = new SensorComponent[7];

    private RobotMovement robotMovement;

    private void Start() {
        robotMovement = gameObject.GetComponent<RobotMovement>();
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
}
