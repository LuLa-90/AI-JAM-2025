using UnityEngine;

public class BumperComponent : MonoBehaviour
{
    private SensorComponent sensorDamage = null;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;
            }
        }
    }

    private void Update() {
        sensorDamage.SetSensorValue(1f); // Placeholder for damage value
    }
}
