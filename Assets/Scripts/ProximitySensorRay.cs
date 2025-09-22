using UnityEngine;

public class ProximitySensorRay : MonoBehaviour
{
    private SensorComponent sensorProximity = null;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();
        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.ProximityBool) {
                sensorProximity = sensor;
            }
        }
    }

    private void Update() {
        sensorProximity.SetSensorValue(1f);
    }

}
