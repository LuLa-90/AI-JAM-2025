using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArmComponent : MonoBehaviour {
    private SensorComponent sensorAngle = null;
    private SensorComponent sensorDamage = null;

    private CollisionZoneBehaviour[] zoneComponents = null;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Angle) {
                sensorAngle = sensor;
                sensorAngle.SetRangeValues(-85f, 85f);
            }
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;

                zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();
            }
        }
    }

    private void Update() {
        float minCurrentHealth = zoneComponents?.Min(e=> e.CurrentHealth) ?? 1f;

        sensorAngle.SetSensorValue(transform.localEulerAngles.x);
        sensorDamage.SetSensorValue(minCurrentHealth); // Placeholder for damage value
    }
}
