using System.Linq;
using UnityEngine;

public class BumperComponent : MonoBehaviour
{
    private SensorComponent sensorDamage = null;

    private CollisionZoneBehaviour[] zoneComponents = null;


    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;

                zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();
            }
        }
    }

    private void Update() {
        float minCurrentHealth = zoneComponents?.Min(e => e.CurrentHealth) ?? 1f;
        sensorDamage.SetSensorValue(minCurrentHealth); // Placeholder for damage value
    }
}
