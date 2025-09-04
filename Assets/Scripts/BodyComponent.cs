using UnityEngine;

public class BodyComponent : MonoBehaviour
{
    private SensorComponent sensorVelocityForwards = null;
    private SensorComponent sensorVelocitySideways = null;
    private SensorComponent sensorDamage = null;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.VelocityForwards) {
                sensorVelocityForwards = sensor;
            }
            if (sensor.GetSensorType == SensorComponent.SensorType.VelocitySideways) {
                sensorVelocitySideways = sensor;
            }
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;
            }
        }
    }

    private void Update() {
        sensorVelocityForwards.SetSensorValue(1f); // Placeholder for forwards velocity value
        sensorVelocitySideways.SetSensorValue(1f); // Placeholder for sideways velocity value
        sensorDamage.SetSensorValue(0f); // Placeholder for damage value
    }
}
