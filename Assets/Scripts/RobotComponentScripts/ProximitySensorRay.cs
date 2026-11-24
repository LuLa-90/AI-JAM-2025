using UnityEngine;

public class ProximitySensorRay : MonoBehaviour
{
    public enum RaySensorType {
        Long,
        Wide
    }

    [SerializeField] private RaySensorType raySensorType = RaySensorType.Long;

    private SensorComponent sensorProximity = null;
    private SensorComponent sensorRayinfo = null;

    private float rayAngleLong = 45f / 6f;
    private float rayDistanceLong = 2.5f;
    private float rayAngleWide = 140f / 6f;
    private float rayDistanceWide = 0.7f;

    private void Start() {
        ownRobot = GetComponentInParent<RobotAgent>();
        var sensors = GetComponentsInChildren<SensorComponent>();
        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Proximity) {
                sensorProximity = sensor;
            }
            if (sensor.GetSensorType == SensorComponent.SensorType.Rayinfo) {
                sensorRayinfo = sensor;
            }
        }
    }

    /// <summary>
    /// What a ray hit: 1 = arena, 2 = robot, 3 = bonus, 0 = anything else.
    /// The arm has its own Rigidbody and tag (RobotArm): an enemy's arm counts as a robot, the robot's own arm doesn't.
    /// </summary>
    /// <param name="hitBody">RaycastHit.transform: the Rigidbody's object, or the collider's own object without one.</param>
    public static int HitType(Transform hitBody, RobotAgent ownRobot) {
        switch (hitBody.gameObject.tag) {
            case "Arena": return 1;
            case "Robot": return 2;
            case "Bonus": return 3;
            case "RobotArm":
                var owner = hitBody.GetComponentInParent<RobotAgent>();
                return owner != null && owner != ownRobot ? 2 : 0;
        }
        return 0;
    }

    private RobotAgent ownRobot;

    private void FixedUpdate() {

        var rayDistance = rayDistanceWide;
        var rayAngle = rayAngleWide;
        var rayDirection = transform.TransformDirection(Vector3.forward);

        if (raySensorType == RaySensorType.Long) {
            rayDistance = rayDistanceLong;
            rayAngle = rayAngleLong;
        }

        var sensorValue = rayDistance;
        int objectHitType = 0;
        for (int i = -3; i <= 3; i++) {
            var raycastDirection = Quaternion.AngleAxis(rayAngle * i, Vector3.up) * rayDirection;
            if (Physics.Raycast(transform.position, raycastDirection, out RaycastHit hit, rayDistance)) {

                // for now we wont ignore the arm hit, maybe in the future
                //if (hit.transform.parent == this.transform.parent) {
                //    //continue;
                //}

                //Debug.DrawRay(transform.position, raycastDirection * hit.distance, Color.red);
                if (sensorValue > hit.distance) {
                    sensorValue = hit.distance;
                    // always the type of the nearest hit (an unknown object used to keep the type of a farther ray)
                    objectHitType = HitType(hit.transform, ownRobot);
                }

                //return;
            }
            else {
                //Debug.DrawRay(transform.position, raycastDirection * rayDistance, Color.gray);
            }
        }
        //Debug.Log("Closest hit distance: " + sensorValue / rayDistance);
        //Debug.Log("Closest object hit type: " + objectHitType);
        sensorProximity.SetSensorValue(sensorValue / rayDistance);
        sensorRayinfo.SetSensorValue(objectHitType);
    }

    private void OnDrawGizmos() {
        var rayDistance = rayDistanceWide;
        var rayAngle = rayAngleWide;
        var rayDirection = transform.TransformDirection(Vector3.forward);

        if (raySensorType == RaySensorType.Long) {
            rayDistance = rayDistanceLong;
            rayAngle = rayAngleLong;
        }

        for (int i = -3; i <= 3; i++) {
            var raycastDirection = Quaternion.AngleAxis(rayAngle * i, Vector3.up) * rayDirection;
            if (Physics.Raycast(transform.position, raycastDirection, out RaycastHit hit, rayDistance)) {
                Debug.DrawRay(transform.position, raycastDirection * hit.distance, Color.red);
            }
            else {
                Debug.DrawRay(transform.position, raycastDirection * rayDistance, Color.gray);
            }
        }
    }
}
