using UnityEngine;

public class BodyComponent : MonoBehaviour
{
    private SensorComponent sensorVelocityForwards = null;
    private SensorComponent sensorVelocitySideways = null;
    private SensorComponent sensorDamage = null;
    private SensorComponent sensorEnemyAzimuth = null;

    private Rigidbody rb;

    private void Start() {
        rb = GetComponentInParent<Rigidbody>();

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
            if (sensor.GetSensorType == SensorComponent.SensorType.AzimuthToTarget) {
                sensorEnemyAzimuth = sensor;
            }
        }
    }

    private void Update() {

        Debug.Log(rb.linearVelocity.x);
        Debug.Log(rb.linearVelocity.z);

        sensorVelocityForwards.SetSensorValue(rb.linearVelocity.x); // Placeholder for forwards velocity value
        sensorVelocitySideways.SetSensorValue(rb.linearVelocity.z); // Placeholder for sideways velocity value
        sensorDamage.SetSensorValue(0f); // Placeholder for damage value
        sensorEnemyAzimuth.SetSensorValue(0f); // Placeholder for azimuth value
    }
}
