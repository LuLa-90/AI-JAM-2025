using UnityEngine;

public class ProximitySensorRay : MonoBehaviour
{
    public enum RaySensorType {
        Long,
        Wide
    }

    [SerializeField] private RaySensorType raySensorType = RaySensorType.Long;

    private SensorComponent sensorProximity = null;

    private float rayAngleLong = 45f / 6f;
    private float rayDistanceLong = 1.7f;
    private float rayAngleWide = 140f / 6f;
    private float rayDistanceWide = 0.7f;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();
        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.ProximityBool) {
                sensorProximity = sensor;
            }
        }
    }

    private void FixedUpdate() {

        var rayDistance = rayDistanceWide;
        var rayAngle = rayAngleWide;
        var rayDirection = transform.TransformDirection(Vector3.forward);

        if (raySensorType == RaySensorType.Long) {
            rayDistance = rayDistanceLong;
            rayAngle = rayAngleLong;
        }

        var sensorValue = 0f;
        for (int i = -3; i <= 3; i++) {
            var raycastDirection = Quaternion.AngleAxis(rayAngle * i, Vector3.up) * rayDirection;
            if (Physics.Raycast(transform.position, raycastDirection, out RaycastHit hit, rayDistance)) {
                
                if (hit.transform.parent == this.transform.parent) {
                    //continue;
                }
                Debug.DrawRay(transform.position, raycastDirection * hit.distance, Color.red);
                sensorValue = 1f;
                //return;
            }
            else {
                Debug.DrawRay(transform.position, raycastDirection * rayDistance, Color.white);
            }
        }
        sensorProximity.SetSensorValue(sensorValue);
    }

    private void OnDrawGizmos() {
        
    }

}
