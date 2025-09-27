using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BodyComponent : MonoBehaviour
{
    private SensorComponent sensorVelocityForwards = null;
    private SensorComponent sensorVelocitySideways = null;
    private SensorComponent sensorDamage = null;
    private SensorComponent sensorEnemyAzimuth = null;

    private Rigidbody rb;

    private RobotAgent thisRobot;
    private RobotAgent enemyRobot;

    private void Start() {
        rb = GetComponentInParent<Rigidbody>();

        thisRobot = GetComponentInParent<RobotAgent>();
        enemyRobot = FindObjectsByType<RobotAgent>(FindObjectsSortMode.None).FirstOrDefault(r => r != thisRobot);

        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            // TODO: znormalizovat
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
                sensorEnemyAzimuth.SetRangeValues(-180f, 180f);
            }
        }
    }

    private void Update() {
        SetVelocitySensorValues();
        SetAzimuthSensorValues();

        sensorDamage.SetSensorValue(0f); // Placeholder for damage value
        
    }

    private void SetVelocitySensorValues() {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensorVelocityForwards.SetSensorValue(localVelocity.z);
        sensorVelocitySideways.SetSensorValue(localVelocity.x);
    }

    private void SetAzimuthSensorValues() {
        
        if (enemyRobot != null) {
            var vectorToEnemy = enemyRobot.transform.position - thisRobot.transform.position;
            var angleToEnemy = Vector3.SignedAngle(thisRobot.transform.forward, vectorToEnemy, Vector3.up);

            //Debug.Log("Angle to Enemy: " + angleToEnemy);

            sensorEnemyAzimuth.SetSensorValue(angleToEnemy);
        }

        
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.black;

        if (enemyRobot != null && thisRobot != null) {
            Gizmos.DrawLine(thisRobot.transform.position, enemyRobot.transform.position);
        }
    }
}
