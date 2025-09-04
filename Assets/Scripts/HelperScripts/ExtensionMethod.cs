using Unity.VisualScripting;
using UnityEngine;

public static class ExtensionMethod {
    public static float SetSensorValue(this SensorComponent sensor, float value) {
        if (sensor != null) {
            sensor.SensorValue = value;
        }
        return value;
    }
}
