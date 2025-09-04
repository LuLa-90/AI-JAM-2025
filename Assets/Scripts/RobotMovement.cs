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
    [SerializeField] private ArmComponent armComponent;

    private HingeJoint armJointControl;

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
        armJointControl = armComponent.GetComponent<HingeJoint>();
        armJointControl.useMotor = false;
        armJointControl.useSpring = false;
    }

    private void FixedUpdate() {
        ApplyTrackInput(leftWheels, leftWheelsAxis.ReadValue<float>());
        ApplyTrackInput(rightWheels, rightWheelsAxis.ReadValue<float>());

        if (armLiftAction.ReadValue<float>() == 1) {
            if (!armJointControl.useSpring) {
                armJointControl.useSpring = true;
            }
            armJointControl.useMotor = true;
        }
        else {
            armJointControl.useMotor = false;
        }
    }

    void ApplyTrackInput(WheelCollider[] wheels, float inputValue) {
        foreach (var wheel in wheels) {
            wheel.motorTorque = inputValue * 10f;
        }
    }
}
