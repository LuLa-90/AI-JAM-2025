using UnityEngine;
using UnityEngine.InputSystem;

public class RobotMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    public InputAction leftWheelsAxis;
    public InputAction rightWheelsAxis;
    public InputAction armLiftAction;

    [SerializeField] private WheelCollider[] rightWheels;
    [SerializeField] private WheelCollider[] leftWheels;
    [SerializeField] private Transform armComponent;

    private Vector3 loweredArmRotation;
    private Vector3 raisedArmRotation;

    private void OnEnable() {
        var actionMap = inputActions.FindActionMap("Robot");
        leftWheelsAxis = actionMap.FindAction("MoveL");
        rightWheelsAxis = actionMap.FindAction("MoveR");
        armLiftAction = actionMap.FindAction("ArmLift");

        actionMap.Enable();
    }

    private void OnDisable() {
        var actionMap = inputActions.FindActionMap("Robot");
        actionMap.Disable();
    }

    private void Start() {
        loweredArmRotation = armComponent.localEulerAngles;
    }

    private void FixedUpdate() {
        ApplyTrackInput(leftWheels, leftWheelsAxis.ReadValue<float>());
        ApplyTrackInput(rightWheels, rightWheelsAxis.ReadValue<float>());

        if (armLiftAction.ReadValue<float>() == 1) {
            Vector3 currentAngles = armComponent.localEulerAngles;
            var armAngleFromLowered = currentAngles.x - loweredArmRotation.x;
            if (armAngleFromLowered <= 90f) {
                currentAngles.x += -20f * Time.fixedDeltaTime;
                armComponent.localEulerAngles = currentAngles;
            }
        }
        //else {
        //    armComponent.localEulerAngles = loweredArmRotation;
        //}
    }

    void ApplyTrackInput(WheelCollider[] wheels, float inputValue) {
        foreach (var wheel in wheels) {
            wheel.motorTorque = inputValue * 10f;
        }
    }
}
