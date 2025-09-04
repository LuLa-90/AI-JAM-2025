using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArmComponent : MonoBehaviour
{
    private SensorComponent sensorAngle = null;
    private SensorComponent sensorDamage = null;

    private void Start() {
       var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Angle) {
                sensorAngle = sensor;
            }
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;
            }
        }
    }

    private void Update() {
        sensorAngle.SetSensorValue(transform.localEulerAngles.x);
        sensorDamage.SetSensorValue(1f); // Placeholder for damage value
    }
}
