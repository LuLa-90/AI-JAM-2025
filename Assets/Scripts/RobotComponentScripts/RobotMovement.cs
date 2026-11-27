using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMovement : MonoBehaviour {
    public bool stationaryRobot = false;

    [SerializeField] private InputActionAsset inputActions;
    public InputAction leftWheelsAxis;
    public InputAction rightWheelsAxis;
    public InputAction armLiftAction;

    [SerializeField] private WheelCollider[] rightWheels;
    [SerializeField] private WheelCollider[] leftWheels;
    [SerializeField] public ArmComponent armComponent;


    private HingeJoint armJointControl;

    float motorTorqueMax = 25f;

    // All robots share one input asset. Its "Robot" action map stays enabled while at least one robot uses it;
    // disabling it with the first robot that goes away used to switch the keyboard off for every robot.
    private static readonly Dictionary<InputActionMap, int> actionMapUsers = new Dictionary<InputActionMap, int>();
    private InputActionMap actionMap;

    private void OnEnable() {
        if (inputActions == null) return;
        actionMap = inputActions.FindActionMap("Robot");
        if (actionMap == null) return;

        leftWheelsAxis = actionMap.FindAction("MoveL");
        rightWheelsAxis = actionMap.FindAction("MoveR");
        armLiftAction = actionMap.FindAction("ArmLift");

        actionMapUsers.TryGetValue(actionMap, out int users);
        actionMapUsers[actionMap] = users + 1;
        actionMap.Enable();
    }

    private void OnDisable() {
        if (actionMap == null) return;
        actionMapUsers.TryGetValue(actionMap, out int users);
        users = Mathf.Max(users - 1, 0);
        actionMapUsers[actionMap] = users;
        if (users == 0) actionMap.Disable();
        actionMap = null;
    }

    private void Start() {
        armComponent = GetComponentInChildren<ArmComponent>();
        if (armComponent == null) return; // robot without an arm

        armJointControl = armComponent.GetComponent<HingeJoint>();
        if (armJointControl == null) return;

        armJointControl.connectedBody = gameObject.GetComponent<Rigidbody>();
        armJointControl.useMotor = false;
        armJointControl.useSpring = false;
    }

    private void FixedUpdate() {
        float slowdown = Time.fixedDeltaTime * motorTorqueMax;
        SlowDown(rightWheels, slowdown);
        SlowDown(leftWheels, slowdown);
    }

    private void SlowDown(WheelCollider[] wheels, float slowdown) {
        if (wheels == null) return;
        foreach (var wheel in wheels) {
            if (wheel == null) continue;
            if (Mathf.Abs(wheel.motorTorque) < slowdown) {
                wheel.motorTorque = 0f;
            }
            else {
                wheel.motorTorque = Mathf.Clamp(wheel.motorTorque - (slowdown * Mathf.Sign(wheel.motorTorque)), -motorTorqueMax, motorTorqueMax);
            }
        }
    }

    public void ApplyArmInput(int inputValue) {
        if (stationaryRobot || armJointControl == null) {
            return;
        }
        if (inputValue == 1) {
            if (!armJointControl.useSpring) {
                armJointControl.useSpring = true;
            }
            armJointControl.useMotor = true;
        }
        else {
            armJointControl.useMotor = false;
        }
    }

    public void ApplyBothTrackInputs(float leftTrackValue, float rightTrackValue) {
        //if (leftTrackValue < 0f || rightTrackValue < 0f) {
        //    leftTrackValue *= 0.8f; // Reduce backward speed for left track
        //    rightTrackValue *= 0.8f; // Reduce backward speed for right track
        //}

        if (stationaryRobot) {
            return;
        }
        ApplyTrackInput(leftWheels, leftTrackValue);
        ApplyTrackInput(rightWheels, rightTrackValue);
    }

    void ApplyTrackInput(WheelCollider[] wheels, float inputValue) {
        if (wheels == null || wheels.Length == 0 || wheels[0] == null) return; // robot without wheel colliders

        float torqueStep = motorTorqueMax * 0.3f * inputValue;
        float changedMotorTorque = Mathf.Clamp(wheels[0].motorTorque + torqueStep, -motorTorqueMax, motorTorqueMax);

        foreach (var wheel in wheels) {
            if (wheel != null) wheel.motorTorque = changedMotorTorque;
        }
    }

    // ------------------------------- HEURISTIC -------------------------------
    public void ApplyBothTrackInputsHeuristic(float leftTrackValue, float rightTrackValue) {
        //if (leftTrackValue < 0f || rightTrackValue < 0f) {
        //    leftTrackValue *= 0.8f; // Reduce backward speed for left track
        //    rightTrackValue *= 0.8f; // Reduce backward speed for right track
        //}

        if (stationaryRobot) {
            return;
        }
        ApplyTrackInputHeuristic(leftWheels, leftTrackValue);
        ApplyTrackInputHeuristic(rightWheels, rightTrackValue);
    }

    void ApplyTrackInputHeuristic(WheelCollider[] wheels, float inputValue) {
        if (wheels == null) return;
        foreach (var wheel in wheels) {
            if (wheel != null) wheel.motorTorque = inputValue * motorTorqueMax;
        }
    }
}
